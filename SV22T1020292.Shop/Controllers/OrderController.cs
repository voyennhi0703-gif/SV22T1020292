using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020292.BusinessLayers;
using SV22T1020292.Models.Common;
using SV22T1020292.Models.Sales;
using SV22T1020292.Shop.Models;

namespace SV22T1020292.Shop.Controllers;

/// <summary>
/// Controller quản lý đơn hàng của khách hàng: đặt hàng, lịch sử, chi tiết và theo dõi đơn hàng.
/// </summary>
public class OrderController : Controller
{
    private const int PageSize = 10;

    /// <summary>
    /// Trang đặt hàng (Checkout) — hiển thị thông tin giỏ hàng và biểu mẫu giao hàng.
    /// Yêu cầu khách hàng đã đăng nhập.
    /// </summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var cart = ShoppingCartService.GetCartItems();
        if (!cart.Any())
        {
            TempData["ErrorMessage"] = "Giỏ hàng trống. Vui lòng chọn mặt hàng.";
            return RedirectToAction("Index", "Home");
        }

        var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out var customerId))
            return RedirectToAction("Login", "Account");

        var customer = await CustomerAccountService.GetCustomerAsync(customerId);
        var provinces = await CatalogDataService.ListProvincesAsync();

        ViewBag.Provinces = provinces;
        ViewBag.Cart = cart;
        ViewBag.CartTotal = ShoppingCartService.GetCartTotal();

        return View(customer);
    }

    /// <summary>
    /// Xử lý tạo đơn hàng từ giỏ hàng.
    /// </summary>
    /// <param name="deliveryProvince">Tỉnh/Thành giao hàng.</param>
    /// <param name="deliveryAddress">Địa chỉ giao hàng chi tiết.</param>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(string deliveryProvince, string deliveryAddress)
    {
        if (string.IsNullOrWhiteSpace(deliveryProvince))
            ModelState.AddModelError("deliveryProvince", "Vui lòng chọn Tỉnh/Thành giao hàng.");
        if (string.IsNullOrWhiteSpace(deliveryAddress))
            ModelState.AddModelError("deliveryAddress", "Vui lòng nhập địa chỉ giao hàng.");

        var cart = ShoppingCartService.GetCartItems();
        if (!cart.Any())
        {
            TempData["ErrorMessage"] = "Giỏ hàng trống.";
            return RedirectToAction("Index", "Home");
        }

        var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out var customerId))
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            var provinces = await CatalogDataService.ListProvincesAsync();
            ViewBag.Provinces = provinces;
            ViewBag.Cart = cart;
            ViewBag.CartTotal = ShoppingCartService.GetCartTotal();
            var customer = await CustomerAccountService.GetCustomerAsync(customerId);
            return View(customer);
        }

        var lines = ShoppingCartService.ToOrderCartLines();
        var orderId = await OrderService.CreateOrderAsync(customerId, deliveryProvince, deliveryAddress, lines);

        if (orderId == 0)
        {
            TempData["ErrorMessage"] = "Đặt hàng thất bại. Vui lòng thử lại.";
            return RedirectToAction("Index", "Home");
        }

        ShoppingCartService.ClearCart();
        return RedirectToAction("Success", new { orderId });
    }

    /// <summary>
    /// Trang thông báo đặt hàng thành công.
    /// </summary>
    /// <param name="orderId">Mã đơn hàng vừa tạo.</param>
    [HttpGet]
    public async Task<IActionResult> Success(int orderId)
    {
        var order = await OrderService.GetOrderAsync(orderId);
        if (order == null)
            return RedirectToAction("Index", "Home");

        return View(order);
    }

    /// <summary>
    /// Đơn hàng đang xử lý — theo dõi trạng thái (chờ xác nhận / đang xử lý / đang giao).
    /// </summary>
    /// <param name="page">Số trang cần hiển thị.</param>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MyOrders(int page = 1)
    {
        var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out var customerId))
            return RedirectToAction("Login", "Account");

        var orders = await OrderService.GetOrdersByCustomerAsync(customerId, page, PageSize, activeOrdersOnly: true);
        return View(orders);
    }

    /// <summary>
    /// Lịch sử mua hàng — tất cả đơn đã đặt (kể cả đã giao / đã hủy).
    /// </summary>
    /// <param name="page">Số trang cần hiển thị.</param>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> PurchaseHistory(int page = 1)
    {
        var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out var customerId))
            return RedirectToAction("Login", "Account");

        var orders = await OrderService.GetOrdersByCustomerAsync(customerId, page, PageSize, activeOrdersOnly: false);
        return View(orders);
    }

    /// <summary>
    /// Chi tiết một đơn hàng của khách hàng đã đăng nhập.
    /// </summary>
    /// <param name="orderId">Mã đơn hàng.</param>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Detail(int orderId)
    {
        var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(customerIdClaim) || !int.TryParse(customerIdClaim, out var customerId))
            return RedirectToAction("Login", "Account");

        var order = await OrderService.GetOrderAsync(orderId);
        if (order == null || order.CustomerID != customerId)
            return RedirectToAction("PurchaseHistory");

        var details = await OrderService.GetOrderDetailsAsync(orderId);
        ViewBag.Details = details;
        return View(order);
    }

    /// <summary>
    /// Theo dõi trạng thái đơn hàng theo mã đơn — không yêu cầu đăng nhập.
    /// </summary>
    /// <param name="orderId">Mã đơn hàng cần tra cứu.</param>
    [HttpGet]
    public async Task<IActionResult> Track(string? orderId)
    {
        if (!string.IsNullOrEmpty(orderId) && int.TryParse(orderId, out var id))
        {
            var order = await OrderService.GetOrderAsync(id);
            if (order != null)
            {
                var details = await OrderService.GetOrderDetailsAsync(id);
                ViewBag.Details = details;
                return View(order);
            }
        }

        return View(new OrderViewInfo());
    }
}
