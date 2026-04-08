using Microsoft.AspNetCore.Mvc;
using SV22T1020292.BusinessLayers;
using SV22T1020292.Models.Catalog;
using SV22T1020292.Models.Common;
using SV22T1020292.Shop.Models;

namespace SV22T1020292.Shop.Controllers;

/// <summary>
/// Controller quản lý mặt hàng trong cửa hàng: danh sách, lọc, chi tiết và sản phẩm liên quan.
/// </summary>
public class ProductController : Controller
{
    private const int PageSize = 18;
    private const int RelatedPageSize = 8;

    /// <summary>
    /// Danh sách mặt hàng có tìm kiếm, lọc theo loại, nhà cung cấp và khoảng giá.
    /// </summary>
    /// <param name="searchValue">Từ khóa tìm kiếm theo tên hoặc mô tả sản phẩm.</param>
    /// <param name="categoryID">Mã loại hàng (null = tất cả).</param>
    /// <param name="supplierID">Mã nhà cung cấp (null = tất cả).</param>
    /// <param name="minPrice">Giá tối thiểu.</param>
    /// <param name="maxPrice">Giá tối đa.</param>
    /// <param name="page">Số trang cần hiển thị.</param>
    [HttpGet]
    public async Task<IActionResult> Index(
        string searchValue = "",
        int? categoryID = null,
        int? supplierID = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int page = 1)
    {
        var input = new ProductSearchInput
        {
            Page = page,
            PageSize = PageSize,
            SearchValue = searchValue,
            CategoryID = categoryID,
            SupplierID = supplierID,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
        };

        var products = await CatalogDataService.ListProductsAsync(input);
        var categories = await CatalogDataService.ListCategoriesAsync();
        var suppliers = await CatalogDataService.ListSuppliersAsync();

        ViewBag.SearchValue = searchValue;
        ViewBag.CategoryID = categoryID;
        ViewBag.SupplierID = supplierID;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.Categories = categories;
        ViewBag.Suppliers = suppliers;
        ViewBag.CartCount = ShoppingCartService.GetCartItemCount();

        return View(products);
    }

    /// <summary>
    /// Chi tiết một mặt hàng: thông tin cơ bản kèm danh sách sản phẩm liên quan cùng loại.
    /// </summary>
    /// <param name="id">Mã mặt hàng.</param>
    [HttpGet("Product/Detail/{id}")]
    public async Task<IActionResult> Detail(int id)
    {
        var product = await CatalogDataService.GetProductAsync(id);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Mặt hàng không tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        var relatedInput = new ProductSearchInput
        {
            Page = 1,
            PageSize = RelatedPageSize + 1,
            SearchValue = "",
            CategoryID = product.CategoryID,
            SupplierID = null,
            MinPrice = null,
            MaxPrice = null,
        };

        var relatedAll = await CatalogDataService.ListProductsAsync(relatedInput);
        var related = relatedAll.DataItems
            .Where(p => p.ProductID != id && p.IsSelling)
            .Take(RelatedPageSize)
            .ToList();

        var categories = await CatalogDataService.ListCategoriesAsync();
        var categoryName = categories
            .FirstOrDefault(c => c.CategoryID == product.CategoryID)
            ?.CategoryName ?? "";

        ViewBag.RelatedProducts = related;
        ViewBag.CategoryName = categoryName;
        ViewBag.Categories = categories;
        ViewBag.CartCount = ShoppingCartService.GetCartItemCount();

        return View(product);
    }
}
