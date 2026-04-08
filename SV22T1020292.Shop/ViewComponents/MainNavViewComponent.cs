using Microsoft.AspNetCore.Mvc;
using SV22T1020292.BusinessLayers;

namespace SV22T1020292.Shop.ViewComponents;

/// <summary>
/// Thanh điều hướng danh mục loại hàng trên layout Shop.
/// </summary>
public class MainNavViewComponent : ViewComponent
{
    /// <summary>
    /// Tải danh sách loại hàng từ CSDL để hiển thị trong MainNav/Default.cshtml.
    /// </summary>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var categories = await CatalogDataService.ListCategoriesAsync();
        return View(categories);
    }
}
