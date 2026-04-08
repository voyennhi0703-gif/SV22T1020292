using SV22T1020292.Models.Security;

namespace SV22T1020292.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các thao tác dữ liệu liên quan đến tài khoản đăng nhập (nhân viên / khách hàng).
    /// </summary>
    public interface IUserAccountRepository
    {
        /// <summary>Xác thực nhân viên theo email và mật khẩu.</summary>
        Task<UserAccount?> AuthenticateEmployeeAsync(string email, string password);

        /// <summary>Xác thực khách hàng theo email và mật khẩu.</summary>
        Task<UserAccount?> AuthenticateCustomerAsync(string email, string password);

        /// <summary>Thay đổi mật khẩu nhân viên khi biết mật khẩu cũ.</summary>
        Task<bool> ChangePasswordEmployeeAsync(int employeeId, string oldPassword, string newPassword);

        /// <summary>Thay đổi mật khẩu khách hàng khi biết mật khẩu cũ.</summary>
        Task<bool> ChangePasswordCustomerAsync(int customerId, string oldPassword, string newPassword);

        /// <summary>Cập nhật mật khẩu nhân viên theo email (sau khi xác thực mật khẩu cũ).</summary>
        Task<bool> SetEmployeePasswordByEmailAsync(string email, string newPassword);

        /// <summary>Thiết lập mật khẩu mới cho nhân viên theo mã (quản trị thay đổi mật khẩu).</summary>
        Task<bool> SetEmployeePasswordByIdAsync(int employeeId, string newPassword);
    }
}
