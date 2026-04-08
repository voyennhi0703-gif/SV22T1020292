namespace SV22T1020292.Models.Common
{
    /// <summary>
    /// Một dòng trong giỏ hàng tạm dùng khi lập đơn hàng.
    /// </summary>
    public class OrderCartLine
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public string Unit { get; set; } = "";
        public int Quantity { get; set; }
        public decimal SalePrice { get; set; }
        public decimal TotalPrice => Quantity * SalePrice;
    }
}
