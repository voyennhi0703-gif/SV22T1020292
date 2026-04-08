using SV22T1020292.DataLayers.Interfaces;
using SV22T1020292.DataLayers.SQLServer;
using SV22T1020292.Models.Security;

namespace SV22T1020292.BusinessLayers
{
    /// <summary>Loại tài khoản dùng cho xác thực / đổi mật khẩu.</summary>
    public enum AccountTypes
    {
        /// <summary>Nhân viên quản trị / nội bộ.</summary>
        Employee,
        /// <summary>Khách hàng.</summary>
        Customer
    }

    /// <summary>Dịch vụ tài khoản: đăng nhập, đổi mật khẩu (wrap repository + cấu hình kết nối).</summary>
    public static class UserAccountService
    {
        private static IUserAccountRepository Repo => new EmployeeAccountRepository(Configuration.ConnectionString);

        /// <summary>Xác thực theo loại tài khoản (email + mật khẩu).</summary>
        public static Task<UserAccount?> Authorize(AccountTypes type, string username, string password)
        {
            return type switch
            {
                AccountTypes.Employee => Repo.AuthenticateEmployeeAsync(username, password),
                AccountTypes.Customer => Repo.AuthenticateCustomerAsync(username, password),
                _ => Task.FromResult<UserAccount?>(null)
            };
        }

        /// <summary>
        /// Đặt mật khẩu mới sau khi đã kiểm tra mật khẩu cũ ở controller.
        /// Dùng khi quản trị muốn đặt lại mật khẩu mà không cần mật khẩu cũ.
        /// </summary>
        public static async Task ChangePassword(AccountTypes type, string userName, string newPassword)
        {
            if (type == AccountTypes.Employee)
            {
                var ok = await Repo.SetEmployeePasswordByEmailAsync(userName, newPassword);
                if (!ok)
                    throw new InvalidOperationException("Không thể cập nhật mật khẩu nhân viên.");
            }
        }

        /// <summary>
        /// Đổi mật khẩu nhân viên: kiểm tra mật khẩu cũ và đổi sang mật khẩu mới trong một thao tác.
        /// </summary>
        /// <param name="type">Loại tài khoản.</param>
        /// <param name="id">Mã nhân viên.</param>
        /// <param name="oldPassword">Mật khẩu cũ (dùng để xác minh).</param>
        /// <param name="newPassword">Mật khẩu mới.</param>
        /// <returns>True nếu đổi thành công, False nếu mật khẩu cũ không đúng.</returns>
        public static async Task<bool> ChangePassword(AccountTypes type, int id, string oldPassword, string newPassword)
        {
            if (type == AccountTypes.Employee)
            {
                return await Repo.ChangePasswordEmployeeAsync(id, oldPassword, newPassword);
            }
            return false;
        }

        /// <summary>
        /// Quản trị đặt lại mật khẩu nhân viên theo mã.
        /// </summary>
        public static Task<bool> SetEmployeePassword(int employeeId, string newPassword) =>
            Repo.SetEmployeePasswordByIdAsync(employeeId, newPassword);
    }
}
