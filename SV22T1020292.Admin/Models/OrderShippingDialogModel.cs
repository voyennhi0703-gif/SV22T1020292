using SV22T1020292.Models.Partner;

namespace SV22T1020292.Admin.Models
{
    /// <summary>
    /// ViewModel cho dialog chuyển giao hàng (Shipping Dialog).
    /// </summary>
    public class OrderShippingDialogModel
    {
        /// <summary>
        /// Mã đơn hàng cần chuyển giao.
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Danh sách người giao hàng.
        /// </summary>
        public List<Shipper> Shippers { get; set; } = new();
    }
}
