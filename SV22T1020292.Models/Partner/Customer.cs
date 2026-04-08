using System.ComponentModel.DataAnnotations;

namespace SV22T1020292.Models.Partner
{
    /// <summary>
    /// Khách hàng
    /// </summary>
    public class Customer
    {
        /// <summary>
        /// Mã khách hàng
        /// </summary>
        public int CustomerID { get; set; }
        /// <summary>
        /// Tên khách hàng
        /// </summary>
        [Display(Name = "Tên khách hàng")]
        [Required(ErrorMessage = "Tên khách hàng không được để trống.")]
        public string CustomerName { get; set; } = string.Empty;
        /// <summary>
        /// Tên giao dịch
        /// </summary>
        [Display(Name = "Tên giao dịch")]
        [Required(ErrorMessage = "Tên giao dịch không được để trống.")]
        public string ContactName { get; set; } = string.Empty;
        /// <summary>
        /// Tỉnh/thành
        /// </summary>
        public string? Province { get; set; }
        /// <summary>
        /// Địa chỉ
        /// </summary>
        public string? Address { get; set; }
        /// <summary>
        /// Điện thoại
        /// </summary>
        public string? Phone { get; set; }
        /// <summary>
        /// Email
        /// </summary>
        [Display(Name = "Email")]
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; } = string.Empty;
        /// <summary>
        /// Mật khẩu
        /// </summary>
        public string Password { get; set; } = string.Empty;
        /// <summary>
        /// Khách hàng hiện có bị khóa hay không?
        /// </summary>
        public bool? IsLocked { get; set; }
    }
}
