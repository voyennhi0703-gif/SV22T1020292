using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020292.BusinessLayers;
using SV22T1020292.Models.HR;
using SV22T1020292.Models.Security;
using System.Security.Claims;

namespace SV22T1020292.Admin.Controllers
{
    // Controller quản lý tài khoản người dùng quản trị.
    [Authorize]
    public class AccountController : Controller
    {
        // Hiển thị trang đăng nhập.
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Xử lý đăng nhập.
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            ViewBag.Username = username;
            ViewBag.Password = password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập email và mật khẩu!";
                return View();
            }

            var userAccount = await UserAccountService.Authorize(AccountTypes.Employee, username, password);
            if (userAccount == null)
            {
                ViewBag.ErrorMessage = "Email hoặc mật khẩu không đúng! Vui lòng thử lại.";
                return View();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userAccount.UserId),
                new(ClaimTypes.Name, userAccount.UserName),
                new(ClaimTypes.Email, userAccount.Email),
                new("DisplayName", userAccount.DisplayName),
                new("Photo", userAccount.Photo ?? ""),
            };

            if (!string.IsNullOrWhiteSpace(userAccount.RoleNames))
            {
                foreach (var role in userAccount.RoleNames.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);

            return RedirectToAction("Index", "Home");
        }

        // Đăng xuất khỏi hệ thống.
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // Hiển thị trang từ chối truy cập.
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Hiển thị trang thay đổi mật khẩu.
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        /// <summary>
        /// Xử lý đổi mật khẩu.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ thông tin!");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("Error", "Mật khẩu xác nhận không trùng khớp!");
                return View();
            }

            var userData = User.GetUserData();
            if (userData == null || string.IsNullOrWhiteSpace(userData.UserName))
                return RedirectToAction("Login");

            if (!int.TryParse(userData.UserId, out int employeeId))
            {
                ModelState.AddModelError("Error", "Không xác định được tài khoản người dùng!");
                return View();
            }

            var ok = await UserAccountService.ChangePassword(AccountTypes.Employee, employeeId, oldPassword, newPassword);
            if (!ok)
            {
                ModelState.AddModelError("Error", "Mật khẩu hiện tại không đúng!");
                return View();
            }

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Index", "Home");
        }
    }
}
