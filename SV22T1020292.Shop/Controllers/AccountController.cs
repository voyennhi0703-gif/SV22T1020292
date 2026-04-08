using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020292.BusinessLayers;
using SV22T1020292.Models.Partner;

namespace SV22T1020292.Shop.Controllers;

/// <summary>
/// Controller quản lý tài khoản người dùng của trang Shop (Cookie Authentication).
/// Dùng route mặc định {controller}/{action} (/Account/Login, /Account/Register, …).
/// Không dùng [Route("Account")] ở cấp controller — sẽ khiến mọi GET trùng đường dẫn /Account.
/// </summary>
public class AccountController : Controller
{
    /// <summary>
    /// Hiển thị trang đăng nhập.
    /// </summary>
    /// <param name="returnUrl">URL cần chuyển hướng sau khi đăng nhập thành công.</param>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    /// <summary>
    /// Xử lý đăng nhập — lưu toàn bộ thông tin vào Cookie Claims.
    /// </summary>
    /// <param name="email">Email đăng nhập.</param>
    /// <param name="password">Mật khẩu.</param>
    /// <param name="returnUrl">URL cần chuyển hướng sau khi đăng nhập thành công.</param>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Vui lòng nhập email và mật khẩu.");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var user = await CustomerAccountService.AuthenticateAsync(email, password);
        if (user == null)
        {
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var customer = await CustomerAccountService.GetCustomerAsync(int.Parse(user.UserId));
        var displayName = customer?.CustomerName ?? user.DisplayName;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new("DisplayName", displayName),
        };
        if (!string.IsNullOrWhiteSpace(user.RoleNames))
        {
            foreach (var role in user.RoleNames.Split(',', StringSplitOptions.RemoveEmptyEntries))
                claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.Role, "customer"));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Đăng xuất — xóa Cookie Authentication.
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    /// <summary>
    /// Hiển thị trang đăng ký tài khoản.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Register()
    {
        var provinces = await CatalogDataService.ListProvincesAsync();
        ViewBag.Provinces = provinces;
        return View();
    }

    /// <summary>
    /// Xử lý đăng ký tài khoản khách hàng mới.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        string customerName, string contactName, string email,
        string password, string confirmPassword,
        string? phone, string? province, string? address)
    {
        ModelState.Clear();

        if (string.IsNullOrWhiteSpace(customerName))
            ModelState.AddModelError("customerName", "Tên khách hàng không được để trống.");
        if (string.IsNullOrWhiteSpace(contactName))
            ModelState.AddModelError("contactName", "Tên giao dịch không được để trống.");
        if (string.IsNullOrWhiteSpace(email))
            ModelState.AddModelError("email", "Email không được để trống.");
        else if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            ModelState.AddModelError("email", "Email không đúng định dạng (VD: name@domain.com).");

        if (string.IsNullOrWhiteSpace(password))
            ModelState.AddModelError("password", "Mật khẩu không được để trống.");
        else if (password.Length < 6)
            ModelState.AddModelError("password", "Mật khẩu phải có ít nhất 6 ký tự.");

        if (password != confirmPassword)
            ModelState.AddModelError("confirmPassword", "Xác nhận mật khẩu không khớp.");

        if (!ModelState.IsValid)
        {
            var provinces = await CatalogDataService.ListProvincesAsync();
            ViewBag.Provinces = provinces;
            ViewBag.CustomerName = customerName;
            ViewBag.ContactName = contactName;
            ViewBag.Email = email;
            ViewBag.Phone = phone;
            ViewBag.Address = address;
            ViewBag.SelectedProvince = province;
            return View();
        }

        var customerId = await CustomerAccountService.RegisterAsync(
            customerName, contactName, email, password, phone, province, address);

        if (customerId == 0)
        {
            ModelState.AddModelError("email", "Email này đã được sử dụng bởi một tài khoản khác.");
            var provinces = await CatalogDataService.ListProvincesAsync();
            ViewBag.Provinces = provinces;
            ViewBag.CustomerName = customerName;
            ViewBag.ContactName = contactName;
            ViewBag.Email = email;
            ViewBag.Phone = phone;
            ViewBag.Address = address;
            ViewBag.SelectedProvince = province;
            return View();
        }

        TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập lại để truy cập.";
        return RedirectToAction("Login");
    }

    /// <summary>
    /// Hiển thị trang từ chối truy cập.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    /// <summary>
    /// Hiển thị thông tin cá nhân của khách hàng đã đăng nhập.
    /// </summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var customerId))
            return RedirectToAction("Login");

        var customer = await CustomerAccountService.GetCustomerAsync(customerId);
        if (customer == null) return RedirectToAction("Login");

        var provinces = await CatalogDataService.ListProvincesAsync();
        ViewBag.Provinces = provinces;
        return View(customer);
    }

    /// <summary>
    /// Cập nhật thông tin cá nhân của khách hàng đã đăng nhập.
    /// </summary>
    /// <param name="model">Dữ liệu khách hàng cần cập nhật.</param>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(Customer model)
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var customerId))
            return RedirectToAction("Login");

        if (!ModelState.IsValid)
        {
            var provinces = await CatalogDataService.ListProvincesAsync();
            ViewBag.Provinces = provinces;
            return View(model);
        }

        model.CustomerID = customerId;
        var existing = await CustomerAccountService.GetCustomerAsync(customerId);
        if (existing == null)
            return RedirectToAction("Login");
        model.IsLocked = existing.IsLocked;

        var ok = await CustomerAccountService.UpdateCustomerAsync(model);
        if (!ok)
        {
            ModelState.AddModelError("", "Cập nhật thông tin thất bại.");
            var provinces = await CatalogDataService.ListProvincesAsync();
            ViewBag.Provinces = provinces;
            return View(model);
        }

        var claims = User.Claims.ToList();
        var nameIdx = claims.FindIndex(c => c.Type == "DisplayName");
        if (nameIdx >= 0)
            claims[nameIdx] = new Claim("DisplayName", model.CustomerName ?? "");

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            authProperties);

        TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
        return RedirectToAction("Profile");
    }

    /// <summary>
    /// Hiển thị form đổi mật khẩu.
    /// </summary>
    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    /// <summary>
    /// Xử lý đổi mật khẩu của khách hàng đã đăng nhập.
    /// </summary>
    /// <param name="oldPassword">Mật khẩu cũ.</param>
    /// <param name="newPassword">Mật khẩu mới.</param>
    /// <param name="confirmPassword">Xác nhận mật khẩu mới.</param>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            ModelState.AddModelError("newPassword", "Mật khẩu mới không được trống.");
            return View();
        }
        if (newPassword != confirmPassword)
        {
            ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp.");
            return View();
        }
        if (newPassword.Length < 6)
        {
            ModelState.AddModelError("newPassword", "Mật khẩu mới phải có ít nhất 6 ký tự.");
            return View();
        }

        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var customerId))
            return RedirectToAction("Login");

        var ok = await CustomerAccountService.ChangePasswordAsync(customerId, oldPassword, newPassword);
        if (!ok)
        {
            ModelState.AddModelError("oldPassword", "Mật khẩu cũ không đúng.");
            return View();
        }

        TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
        return RedirectToAction("Profile");
    }
}
