using SV22T1020292.Models.HR;

namespace SV22T1020292.Admin.Models
{
    /// <summary>
    /// ViewModel cho dialog duyệt đơn hàng (Accept Dialog).
    /// </summary>
    public class OrderAcceptDialogModel
    {
        /// <summary>
        /// Mã đơn hàng cần duyệt.
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Danh sách nhân viên để chọn người phụ trách.
        /// </summary>
        public List<Employee> Employees { get; set; } = new();
    }
}
