using Microsoft.AspNetCore.Mvc;
using SV22T1020292.BusinessLayers;
using SV22T1020292.Models.Common;
using SV22T1020292.Shop.Models;

namespace SV22T1020292.Shop.Controllers;

/// <summary>
/// Trang chủ Shop: hero + danh mục. Danh sách / lọc sản phẩm tại <see cref="ProductController.Index"/>.
/// </summary>
public class HomeController : Controller
{
    private const int HeroFeaturedCount = 12;

    /// <summary>
    /// Trang chủ — carousel sản phẩm nổi bật và danh mục. Query lọc sản phẩm chuyển sang /Product.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var filterKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "searchValue", "categoryID", "supplierID", "minPrice", "maxPrice", "page"
        };
        if (Request.Query.Keys.Any(k => filterKeys.Contains(k)))
        {
            var qs = Request.QueryString.Value;
            return Redirect(Url.Action("Index", "Product") + qs);
        }

        var input = new ProductSearchInput
        {
            Page = 1,
            PageSize = HeroFeaturedCount,
            SearchValue = "",
        };

        var products = await CatalogDataService.ListProductsAsync(input);
        var categories = await CatalogDataService.ListCategoriesAsync();

        ViewBag.Categories = categories;
        ViewBag.CartCount = ShoppingCartService.GetCartItemCount();

        return View(products);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
