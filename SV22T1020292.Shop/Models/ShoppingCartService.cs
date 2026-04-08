using SV22T1020292.Models.Sales;
using SV22T1020292.Shop.AppCodes;

namespace SV22T1020292.Shop.Models;

/// <summary>
/// Quản lý giỏ hàng lưu trong session (theo mẫu dự án tham chiếu Shop).
/// </summary>
public static class ShoppingCartService
{
    private const string CartSessionKey = "ShoppingCart";

    /// <summary>
    /// Lấy toàn bộ dòng hàng trong giỏ (luôn khác null).
    /// </summary>
    /// <returns>Danh sách chi tiết dạng <see cref="OrderDetailViewInfo"/>.</returns>
    public static List<OrderDetailViewInfo> GetCartItems()
    {
        var cart = ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(CartSessionKey);
        if (cart == null)
        {
            cart = new List<OrderDetailViewInfo>();
            ApplicationContext.SetSessionData(CartSessionKey, cart);
        }

        return cart;
    }

    /// <summary>
    /// Thêm hoặc gộp số lượng một sản phẩm vào giỏ.
    /// </summary>
    /// <param name="item">Thông tin dòng hàng (mã SP, SL, giá, ảnh...).</param>
    public static void AddToCart(OrderDetailViewInfo item)
    {
        var cart = GetCartItems();
        var existing = cart.Find(m => m.ProductID == item.ProductID);
        if (existing == null)
        {
            cart.Add(item);
        }
        else
        {
            existing.Quantity += item.Quantity;
            existing.SalePrice = item.SalePrice;
            if (!string.IsNullOrEmpty(item.Photo))
                existing.Photo = item.Photo;
            if (!string.IsNullOrEmpty(item.ProductName))
                existing.ProductName = item.ProductName;
            if (!string.IsNullOrEmpty(item.Unit))
                existing.Unit = item.Unit;
        }

        ApplicationContext.SetSessionData(CartSessionKey, cart);
    }

    /// <summary>
    /// Cập nhật số lượng và giá bán của một dòng trong giỏ.
    /// </summary>
    /// <param name="productID">Mã mặt hàng.</param>
    /// <param name="quantity">Số lượng mới.</param>
    /// <param name="salePrice">Đơn giá bán.</param>
    public static void UpdateCartItem(int productID, int quantity, decimal salePrice)
    {
        var cart = GetCartItems();
        var line = cart.Find(m => m.ProductID == productID);
        if (line == null) return;

        line.Quantity = quantity;
        line.SalePrice = salePrice;
        ApplicationContext.SetSessionData(CartSessionKey, cart);
    }

    /// <summary>
    /// Xóa một sản phẩm khỏi giỏ.
    /// </summary>
    /// <param name="productID">Mã mặt hàng.</param>
    public static void RemoveFromCart(int productID)
    {
        var cart = GetCartItems();
        var index = cart.FindIndex(m => m.ProductID == productID);
        if (index < 0) return;

        cart.RemoveAt(index);
        ApplicationContext.SetSessionData(CartSessionKey, cart);
    }

    /// <summary>
    /// Xóa sạch giỏ hàng.
    /// </summary>
    public static void ClearCart()
    {
        ApplicationContext.SetSessionData(CartSessionKey, new List<OrderDetailViewInfo>());
    }

    /// <summary>
    /// Tổng số lượng mặt hàng (cộng dồn từng dòng).
    /// </summary>
    public static int GetCartItemCount() => GetCartItems().Sum(i => i.Quantity);

    /// <summary>
    /// Tổng tiền hàng (chưa gồm phí vận chuyển).
    /// </summary>
    public static decimal GetCartTotal() => GetCartItems().Sum(i => i.TotalPrice);

    /// <summary>
    /// Chuyển giỏ sang dạng <see cref="SV22T1020292.Models.Common.OrderCartLine"/> để tạo đơn hàng.
    /// </summary>
    public static IReadOnlyList<SV22T1020292.Models.Common.OrderCartLine> ToOrderCartLines()
    {
        return GetCartItems()
            .Select(x => new SV22T1020292.Models.Common.OrderCartLine
            {
                ProductID = x.ProductID,
                ProductName = x.ProductName,
                Unit = x.Unit,
                Quantity = x.Quantity,
                SalePrice = x.SalePrice
            })
            .ToList();
    }
}
