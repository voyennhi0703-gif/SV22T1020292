using Microsoft.AspNetCore.Mvc;
using SV22T1020292.BusinessLayers;
using SV22T1020292.Models.Sales;
using SV22T1020292.Shop.Models;

namespace SV22T1020292.Shop.Controllers;

/// <summary>
/// Controller quản lý giỏ hàng của khách hàng.
/// </summary>
public class CartController : Controller
{
    /// <summary>
    /// Hiển thị giỏ hàng — yêu cầu khách hàng đã đăng nhập.
    /// Nếu chưa đăng nhập sẽ chuyển hướng tới trang đăng nhập kèm returnUrl.
    /// </summary>
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            TempData["ErrorMessage"] = "Bạn phải đăng nhập mới xem được giỏ hàng của mình.";
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Cart") });
        }

        var cart = ShoppingCartService.GetCartItems();
        return View(cart);
    }

    /// <summary>
    /// Thêm sản phẩm vào giỏ hàng qua AJAX — không yêu cầu đăng nhập.
    /// </summary>
    /// <param name="productID">Mã sản phẩm cần thêm.</param>
    /// <param name="quantity">Số lượng (mặc định 1).</param>
    /// <returns>JSON chứa trạng thái thành công và số lượng badge giỏ hàng.</returns>
    [HttpPost]
    public async Task<IActionResult> AddToCart(int productID, int quantity = 1)
    {
        if (quantity <= 0) quantity = 1;

        var product = await CatalogDataService.GetProductAsync(productID);
        if (product == null)
            return Json(new { success = false, message = "Sản phẩm không tồn tại" });

        var item = new OrderDetailViewInfo
        {
            ProductID = productID,
            ProductName = product.ProductName,
            Unit = product.Unit,
            Photo = product.Photo ?? "",
            Quantity = quantity,
            SalePrice = product.Price
        };

        ShoppingCartService.AddToCart(item);
        var count = ShoppingCartService.GetCartItemCount();
        return Json(new { success = true, count, cartCount = count });
    }

    /// <summary>
    /// Thêm sản phẩm vào giỏ và chuyển hướng tới trang giỏ hàng (GET, dùng cho nút Mua ngay).
    /// </summary>
    /// <param name="productId">Mã sản phẩm cần thêm.</param>
    /// <param name="quantity">Số lượng (mặc định 1).</param>
    /// <returns>Chuyển hướng tới trang giỏ hàng.</returns>
    [HttpGet]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        if (quantity <= 0) quantity = 1;

        var product = await CatalogDataService.GetProductAsync(productId);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Mặt hàng không tồn tại.";
            return RedirectToAction("Index", "Home");
        }

        ShoppingCartService.AddToCart(new OrderDetailViewInfo
        {
            ProductID = product.ProductID,
            ProductName = product.ProductName,
            Unit = product.Unit,
            Photo = product.Photo ?? "",
            Quantity = quantity,
            SalePrice = product.Price
        });

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Xóa sản phẩm khỏi giỏ hàng qua AJAX.
    /// </summary>
    /// <param name="productID">Mã sản phẩm cần xóa.</param>
    /// <returns>JSON xác nhận xóa thành công.</returns>
    [HttpPost]
    public IActionResult RemoveFromCart(int productID)
    {
        ShoppingCartService.RemoveFromCart(productID);
        return Json(new { success = true });
    }

    /// <summary>
    /// Xóa sạch toàn bộ giỏ hàng qua AJAX.
    /// </summary>
    /// <returns>JSON xác nhận xóa thành công.</returns>
    [HttpPost]
    public IActionResult ClearCart()
    {
        ShoppingCartService.ClearCart();
        return Json(new { success = true });
    }

    /// <summary>
    /// Cập nhật số lượng của một dòng trong giỏ hàng (form POST).
    /// Nếu số lượng mới nhỏ hơn hoặc bằng 0, sản phẩm sẽ bị xóa khỏi giỏ.
    /// </summary>
    /// <param name="productId">Mã sản phẩm cần cập nhật.</param>
    /// <param name="quantity">Số lượng mới.</param>
    /// <returns>Chuyển hướng về trang giỏ hàng.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update(int productId, int quantity)
    {
        var line = ShoppingCartService.GetCartItems().Find(m => m.ProductID == productId);
        if (line == null)
            return RedirectToAction(nameof(Index));

        if (quantity <= 0)
            ShoppingCartService.RemoveFromCart(productId);
        else
            ShoppingCartService.UpdateCartItem(productId, quantity, line.SalePrice);

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Xóa một dòng sản phẩm khỏi giỏ hàng (form POST, có chống giả mạo CSRF).
    /// </summary>
    /// <param name="productId">Mã sản phẩm cần xóa.</param>
    /// <returns>Chuyển hướng về trang giỏ hàng.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int productId)
    {
        ShoppingCartService.RemoveFromCart(productId);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Xóa toàn bộ giỏ hàng (form POST, có chống giả mạo CSRF).
    /// </summary>
    /// <returns>Chuyển hướng về trang giỏ hàng.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        ShoppingCartService.ClearCart();
        return RedirectToAction(nameof(Index));
    }
}
