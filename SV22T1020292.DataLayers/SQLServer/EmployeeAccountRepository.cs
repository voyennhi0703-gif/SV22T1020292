using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020292.DataLayers.Interfaces;
using SV22T1020292.Models.Security;

namespace SV22T1020292.DataLayers.SQLServer
{
    /// <summary>
    /// Lõi xử lý đăng nhập / mật khẩu Employee và Customer (Dapper). Dùng chung cho EmployeeAccountRepository và CustomerAccountRepository.
    /// </summary>
    internal sealed class UserAccountRepositoryCore
    {
        private readonly string _connectionString;

        public UserAccountRepositoryCore(string connectionString) =>
            _connectionString = connectionString;

        public async Task<UserAccount?> AuthenticateEmployeeAsync(string email, string password)
        {
            await using var conn = new SqlConnection(_connectionString);
            var row = await conn.QueryFirstOrDefaultAsync<EmployeeAuthRow>(@"
                SELECT EmployeeID,
                    CAST(FullName AS NVARCHAR(200)) AS FullName,
                    CAST(Email AS NVARCHAR(256)) AS Email,
                    CAST(Photo AS NVARCHAR(255)) AS Photo,
                    CAST(RoleNames AS NVARCHAR(500)) AS RoleNames
                FROM Employees
                WHERE Email = @email AND Password = @password AND ISNULL(IsWorking, 1) = 1",
                new { email, password });
            if (row == null) return null;
            return new UserAccount
            {
                UserId = row.EmployeeID.ToString(),
                UserName = row.Email,
                Email = row.Email,
                DisplayName = row.FullName,
                Photo = row.Photo ?? "",
                RoleNames = row.RoleNames ?? ""
            };
        }

        /// <summary>
        /// Xác thực khách hàng theo email và mật khẩu.
        /// </summary>
        /// <param name="email">Email khách hàng.</param>
        /// <param name="password">Mật khẩu khách hàng.</param>
        /// <returns>Thông tin tài khoản khách hàng hoặc null nếu không tìm thấy.</returns>
        public async Task<UserAccount?> AuthenticateCustomerAsync(string email, string password)
        {
            await using var conn = new SqlConnection(_connectionString);
            var row = await conn.QueryFirstOrDefaultAsync<CustomerAuthRow>(@"
                SELECT CustomerID,
                    CAST(CustomerName AS NVARCHAR(200)) AS CustomerName,
                    CAST(Email AS NVARCHAR(256)) AS Email
                FROM Customers
                WHERE Email = @email AND Password = @password AND ISNULL(IsLocked, 0) = 0",
                new { email, password });
            if (row == null) return null;
            return new UserAccount
            {
                UserId = row.CustomerID.ToString(),
                UserName = row.Email,
                Email = row.Email,
                DisplayName = row.CustomerName,
                Photo = "",
                RoleNames = ""
            };
        }

        /// <summary>
        /// Thay đổi mật khẩu nhân viên khi biết mật khẩu cũ.
        /// </summary>
        /// <param name="employeeId">Mã nhân viên.</param>
        /// <param name="oldPassword">Mật khẩu cũ nhân viên.</param>
        /// <param name="newPassword">Mật khẩu mới nhân viên.</param>
        /// <returns>True nếu thay đổi thành công, ngược lại False.</returns>
        public async Task<bool> ChangePasswordEmployeeAsync(int employeeId, string oldPassword, string newPassword)
        {
            await using var conn = new SqlConnection(_connectionString);
            var n = await conn.ExecuteAsync(@"
                UPDATE Employees SET Password=@newPassword WHERE EmployeeID=@employeeId AND Password=@oldPassword",
                new { employeeId, oldPassword, newPassword });
            return n > 0;
        }

        /// <summary>
        /// Thay đổi mật khẩu khách hàng khi biết mật khẩu cũ.
        /// </summary>
        /// <param name="customerId">Mã khách hàng.</param>
        /// <param name="oldPassword">Mật khẩu cũ khách hàng.</param>
        /// <param name="newPassword">Mật khẩu mới khách hàng.</param>
        /// <returns>True nếu thay đổi thành công, ngược lại False.</returns>
        public async Task<bool> ChangePasswordCustomerAsync(int customerId, string oldPassword, string newPassword)
        {
            await using var conn = new SqlConnection(_connectionString);
            var n = await conn.ExecuteAsync(@"
                UPDATE Customers SET Password=@newPassword WHERE CustomerID=@customerId AND Password=@oldPassword",
                new { customerId, oldPassword, newPassword });
            return n > 0;
        }

        /// <summary>
        /// Thiết lập mật khẩu mới cho nhân viên theo email (quản trị thay đổi mật khẩu).
        /// </summary>
        /// <param name="email">Email nhân viên.</param>
        /// <param name="newPassword">Mật khẩu mới nhân viên.</param>
        /// <returns>True nếu thiết lập thành công, ngược lại False.</returns>
        public async Task<bool> SetEmployeePasswordByEmailAsync(string email, string newPassword)
        {
            await using var conn = new SqlConnection(_connectionString);
            var n = await conn.ExecuteAsync(
                "UPDATE Employees SET Password=@newPassword WHERE Email=@email",
                new { email, newPassword });
            return n > 0;
        }

        /// <summary>
        /// Thiết lập mật khẩu mới cho nhân viên theo mã (quản trị thay đổi mật khẩu).
        /// </summary>
        /// <param name="employeeId">Mã nhân viên.</param>
        /// <param name="newPassword">Mật khẩu mới nhân viên.</param>
        /// <returns>True nếu thiết lập thành công, ngược lại False.</returns>
        public async Task<bool> SetEmployeePasswordByIdAsync(int employeeId, string newPassword)
        {
            await using var conn = new SqlConnection(_connectionString);
            var n = await conn.ExecuteAsync(
                "UPDATE Employees SET Password=@newPassword WHERE EmployeeID=@employeeId",
                new { employeeId, newPassword });
            return n > 0;
        }
    
        private sealed class EmployeeAuthRow
        {
            public int EmployeeID { get; set; }
            public string FullName { get; set; } = "";
            public string Email { get; set; } = "";
            public string? Photo { get; set; }
            public string? RoleNames { get; set; }
        }

        private sealed class CustomerAuthRow
        {
            public int CustomerID { get; set; }
            public string CustomerName { get; set; } = "";
            public string Email { get; set; } = "";
        }
    }

    /// <summary>
    /// Tài khoản nhân viên (Admin): triển khai IUserAccountRepository.
    /// </summary>
    public sealed class EmployeeAccountRepository : IUserAccountRepository
    {
        private readonly UserAccountRepositoryCore _core;

        public EmployeeAccountRepository(string connectionString) =>
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
