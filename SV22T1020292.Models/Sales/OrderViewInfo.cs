namespace SV22T1020292.Models.Sales
{
    /// <summary>
    /// Thông tin đầy đủ của một đơn hàng (DTO)
    /// </summary>
    public class OrderViewInfo : Order
    {
        /// <summary>
        /// Tên nhân viên phụ trách đơn hàng
        /// </summary>
        public string EmployeeName { get; set; } = "";

        /// <summary>
        /// Tên khách hàng
        /// </summary>
        public string CustomerName { get; set; } = "";
        /// <summary>
        /// Tên giao dịch của khách hàng
        /// </summary>
        public string CustomerContactName { get; set; } = "";
        /// <summary>
        /// Email của khách hàng
        /// </summary>
        public string CustomerEmail { get; set; } = "";
        /// <summary>
        /// Điện thoại khách hàng
        /// </summary>
        public string CustomerPhone { get; set; } = "";
        /// <summary>
        /// Địa chỉ của khách hàng
        /// </summary>
        public string CustomerAddress { get; set; } = "";

        /// <summary>
        /// Tên người giao hàng
        /// </summary>
        public string ShipperName { get; set; } = "";
        /// <summary>
        /// Điện thoại người giao hàng
        /// </summary>
        public string ShipperPhone { get; set; } = "";

        /// <summary>
        /// Mô tả trạng thái (map từ SQL, bảng OrderStatus).
        /// </summary>
        public string StatusDescription { get; set; } = "";

        /// <summary>
        /// Tổng tiền đơn (SUM chi tiết). Có setter để Dapper map từ cột SQL <c>TotalAmount</c>.
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>Tổng giá trị đơn hàng (alias của TotalAmount).</summary>
        public decimal DetailsTotalValue
        {
            get => TotalAmount;
            set => TotalAmount = value;
        }

        /// <summary>Tổng giá trị đơn hàng (alias).</summary>
        public decimal TotalValue => TotalAmount;
    }
}
