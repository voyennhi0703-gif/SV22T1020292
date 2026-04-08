using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020292.Models.Sales;

namespace SV22T1020292.BusinessLayers
{
    /// <summary>
    /// Cung cấp các tính năng xử lý dữ liệu cho trang Dashboard (trang chủ admin).
    /// </summary>
    public static class DashboardDataService
    {
        /// <summary>
        /// Lấy doanh thu trong ngày hôm nay.
        /// </summary>
        public static async Task<decimal> GetTodayRevenueAsync()
        {
            await using var conn = new SqlConnection(Configuration.ConnectionString);
            var today = DateTime.Today.Date;
            var tomorrow = today.AddDays(1);
            return await conn.ExecuteScalarAsync<decimal>(@"
                SELECT ISNULL(SUM(od.SalePrice * od.Quantity), 0)
                FROM Orders o
                INNER JOIN OrderDetails od ON o.OrderID = od.OrderID
                WHERE o.Status = 4 AND o.FinishedTime >= @today AND o.FinishedTime < @tomorrow",
                new { today, tomorrow });
        }

        /// <summary>
        /// Lấy tổng số đơn hàng.
        /// </summary>
        public static async Task<int> GetOrderCountAsync()
        {
            await using var conn = new SqlConnection(Configuration.ConnectionString);
            return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Orders");
        }

        /// <summary>
        /// Lấy tổng số khách hàng.
        /// </summary>
        public static async Task<int> GetCustomerCountAsync()
        {
            await using var conn = new SqlConnection(Configuration.ConnectionString);
            return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Customers");
        }

        /// <summary>
        /// Lấy tổng số mặt hàng.
        /// </summary>
        public static async Task<int> GetProductCountAsync()
        {
            await using var conn = new SqlConnection(Configuration.ConnectionString);
            return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Products");
        }

        /// <summary>
        /// Lấy danh sách đơn hàng đang xử lý (trạng thái mới hoặc đã duyệt).
        /// </summary>
        public static async Task<List<OrderViewInfo>> ListProcessingOrdersAsync()
        {
            await using var conn = new SqlConnection(Configuration.ConnectionString);
            var sql = @"
                SELECT TOP 10
                    o.OrderID, o.CustomerID, c.CustomerName, o.OrderTime,
                    o.DeliveryProvince, o.DeliveryAddress, o.EmployeeID,
                    CAST(e.FullName AS NVARCHAR(200)) AS EmployeeName,
                    o.ShipperID, s.ShipperName, o.Status,
                    os.Description AS StatusDescription,
                    ISNULL((SELECT SUM(CAST(od.Quantity AS decimal(18,4)) * CAST(od.SalePrice AS decimal(18,4)))
                        FROM OrderDetails od WHERE od.OrderID = o.OrderID), 0) AS TotalAmount
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                INNER JOIN OrderStatus os ON o.Status = os.Status
                WHERE o.Status IN (1, 2)
                ORDER BY o.OrderTime DESC";
            var rows = await conn.QueryAsync<OrderViewInfo>(sql);
            return rows.ToList();
        }

        /// <summary>
        /// Lấy doanh thu theo ngày trong khoảng thời gian.
        /// </summary>
        /// <param name="fromDate">Ngày bắt đầu.</param>
        /// <param name="toDate">Ngày kết thúc.</param>
        /// <returns>Tổng doanh thu.</returns>
        public static async Task<decimal> GetRevenueAsync(DateTime fromDate, DateTime toDate)
        {
            await using var conn = new SqlConnection(Configuration.ConnectionString);
            return await conn.ExecuteScalarAsync<decimal>(@"
                SELECT ISNULL(SUM(od.SalePrice * od.Quantity), 0)
                FROM Orders o
                INNER JOIN OrderDetails od ON o.OrderID = od.OrderID
                WHERE o.Status = 4
                  AND o.FinishedTime >= @fromDate
                  AND o.FinishedTime <= @toDate",
                new { fromDate, toDate });
        }
    }
}
