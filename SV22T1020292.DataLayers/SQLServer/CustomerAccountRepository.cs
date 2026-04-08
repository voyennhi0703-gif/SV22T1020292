using SV22T1020292.DataLayers.Interfaces;
using SV22T1020292.Models.Security;

namespace SV22T1020292.DataLayers.SQLServer
{
    /// <summary>
    /// Tài khoản khách hàng (Shop): triển khai IUserAccountRepository; dùng chung UserAccountRepositoryCore với EmployeeAccountRepository.
    /// </summary>
    public sealed class CustomerAccountRepository : IUserAccountRepository
    {
        private readonly UserAccountRepositoryCore _core;

        public CustomerAccountRepository(string connectionString) =>
            _core = new UserAccountRepositoryCore(connectionString);

        /// <summary>
        /// Xác thực nhân viên theo email và mật khẩu.
        /// </summary>
        /// <param name="email">Email nhân viên.</param>
        /// <param name="password">Mật khẩu nhân viên.</param>
        /// <returns>Thông tin tài khoản nhân viên hoặc null nếu không tìm thấy.</returns>
        public Task<UserAccount?> AuthenticateEmployeeAsync(string email, string password) =>
            _core.AuthenticateEmployeeAsync(email, password);

        /// <summary>
        /// Xác thực khách hàng theo email và mật khẩu.
        /// </summary>
        /// <param name="email">Email khách hàng.</param>
        /// <param name="password">Mật khẩu khách hàng.</param>
        /// <returns>Thông tin tài khoản khách hàng hoặc null nếu không tìm thấy.</returns>
        public Task<UserAccount?> AuthenticateCustomerAsync(string email, string password) =>
            _core.AuthenticateCustomerAsync(email, password);

        /// <summary>
        /// Thay đổi mật khẩu nhân viên khi biết mật khẩu cũ.
        /// </summary>
        /// <param name="employeeId">Mã nhân viên.</param>
        /// <param name="oldPassword">Mật khẩu cũ nhân viên.</param>
        /// <param name="newPassword">Mật khẩu mới nhân viên.</param>
        /// <returns>True nếu thay đổi thành công, ngược lại False.</returns>
        public Task<bool> ChangePasswordEmployeeAsync(int employeeId, string oldPassword, string newPassword) =>
            _core.ChangePasswordEmployeeAsync(employeeId, oldPassword, newPassword);

        /// <summary>
        /// Thay đổi mật khẩu khách hàng khi biết mật khẩu cũ.
        /// </summary>
        /// <param name="customerId">Mã khách hàng.</param>
        /// <param name="oldPassword">Mật khẩu cũ khách hàng.</param>
        /// <param name="newPassword">Mật khẩu mới khách hàng.</param>
        /// <returns>True nếu thay đổi thành công, ngược lại False.</returns>
        public Task<bool> ChangePasswordCustomerAsync(int customerId, string oldPassword, string newPassword) =>
            _core.ChangePasswordCustomerAsync(customerId, oldPassword, newPassword);

        /// <summary>
        /// Thiết lập mật khẩu mới cho nhân viên theo email (quản trị thay đổi mật khẩu).
        /// </summary>
        /// <param name="email">Email nhân viên.</param>
        /// <param name="newPassword">Mật khẩu mới nhân viên.</param>
        /// <returns>True nếu thiết lập thành công, ngược lại False.</returns>
        public Task<bool> SetEmployeePasswordByEmailAsync(string email, string newPassword) =>
            _core.SetEmployeePasswordByEmailAsync(email, newPassword);

        /// <summary>
        /// Thiết lập mật khẩu mới cho nhân viên theo mã (quản trị thay đổi mật khẩu).
        /// </summary>
        /// <param name="employeeId">Mã nhân viên.</param>
        /// <param name="newPassword">Mật khẩu mới nhân viên.</param>
        /// <returns>True nếu thiết lập thành công, ngược lại False.</returns>
        public Task<bool> SetEmployeePasswordByIdAsync(int employeeId, string newPassword) =>
            _core.SetEmployeePasswordByIdAsync(employeeId, newPassword);
    }
}
