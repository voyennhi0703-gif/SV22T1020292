using SV22T1020292.Models.Partner;
using SV22T1020292.Models.Sales;

namespace SV22T1020292.Admin.Models
{
    /// <summary>
    /// ViewModel cho trang chi tiết đơn hàng.
    /// </summary>
    public class OrderDetailPageModel
    {
        /// <summary>
        /// Thông tin đơn hàng.
        /// </summary>
        public OrderViewInfo Order { get; set; } = new();
        /// <summary>
        /// Thông tin khách hàng của đơn hàng.
        /// </summary>
        public Customer? Customer { get; set; }
        /// <summary>
        /// Danh sách chi tiết mặt hàng trong đơn hàng.
        /// </summary>
        public List<OrderDetailViewInfo> Details { get; set; } = new();
    }
}
