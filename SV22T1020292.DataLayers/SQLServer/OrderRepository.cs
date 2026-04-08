using System.Globalization;
using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020292.DataLayers.Interfaces;
using SV22T1020292.Models.Common;
using SV22T1020292.Models.Sales;

namespace SV22T1020292.DataLayers.SQLServer
{
    /// <summary>
    /// Truy vấn Orders và OrderDetails.
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo với chuỗi kết nối SQL Server.
        /// </summary>
        public OrderRepository(string connectionString) => _connectionString = connectionString;

        private const string OrderSelect = @"
            SELECT o.OrderID, o.CustomerID, c.CustomerName, c.ContactName AS CustomerContactName,
                c.Phone AS CustomerPhone,
                o.OrderTime, o.DeliveryProvince, o.DeliveryAddress,
                o.EmployeeID, CAST(e.FullName AS NVARCHAR(200)) AS EmployeeName, o.AcceptTime,
                o.ShipperID, s.ShipperName, o.ShippedTime, o.FinishedTime,
                o.Status, os.Description AS StatusDescription,
                ISNULL((
                    SELECT SUM(CAST(od.Quantity AS decimal(18,4)) * CAST(od.SalePrice AS decimal(18,4)))
                    FROM OrderDetails od WHERE od.OrderID = o.OrderID
                ), 0) AS TotalAmount
            FROM Orders o
            LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
            LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
            LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
            INNER JOIN OrderStatus os ON o.Status = os.Status";

        private static DateTime? TryParseOrderDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            var s = value.Trim();
            var cultureVi = new CultureInfo("vi-VN");
            if (DateTime.TryParse(s, cultureVi, DateTimeStyles.None, out var d))
                return d.Date;
            var formats = new[] { "d/M/yyyy", "dd/MM/yyyy", "d/M/yy", "dd/MM/yy" };
            if (DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return d.Date;
            return null;
        }

        /// <summary>
        /// Lấy danh sách đơn hàng với phân trang.
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm và phân trang.</param>
        /// <returns>Kết quả phân trang.</returns>
        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            await using var conn = new SqlConnection(_connectionString);
            var like = SqlLike.Pattern(input.SearchValue);
            var orderDateFrom = TryParseOrderDate(input.DateFrom);
            var orderDateTo = TryParseOrderDate(input.DateTo);
            var where = @"WHERE (@Search = '' OR CAST(o.OrderID AS nvarchar(20)) LIKE @Like OR c.CustomerName LIKE @Like OR ISNULL(o.DeliveryAddress,'') LIKE @Like)
                AND (@Status IS NULL OR o.Status = @Status)
                AND (@OrderDateFrom IS NULL OR CAST(o.OrderTime AS DATE) >= @OrderDateFrom)
                AND (@OrderDateTo IS NULL OR CAST(o.OrderTime AS DATE) <= @OrderDateTo)
                AND (@CustomerID IS NULL OR o.CustomerID = @CustomerID)
                AND (@ActiveOrdersOnly = 0 OR o.Status IN (1, 2, 3))";
            var countSql = $@"
                SELECT COUNT(*) FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                {where}";
            var activeOnly = input.ActiveOrdersOnly ? 1 : 0;
            var rowCount = await conn.ExecuteScalarAsync<int>(countSql,
                new { Search = input.SearchValue ?? "", Like = like, input.Status, OrderDateFrom = orderDateFrom, OrderDateTo = orderDateTo, input.CustomerID, ActiveOrdersOnly = activeOnly });
            var result = new PagedResult<OrderViewInfo> { Page = input.Page, PageSize = input.PageSize, RowCount = rowCount };
            if (input.PageSize == 0)
            {
                var all = await conn.QueryAsync<OrderViewInfo>($@"
                    {OrderSelect} {where} ORDER BY o.OrderTime DESC",
                    new { Search = input.SearchValue ?? "", Like = like, input.Status, OrderDateFrom = orderDateFrom, OrderDateTo = orderDateTo, input.CustomerID, ActiveOrdersOnly = activeOnly });
                result.DataItems = all.ToList();
                return result;
            }
            var data = await conn.QueryAsync<OrderViewInfo>($@"
                SELECT OrderID, CustomerID, CustomerName, CustomerContactName, CustomerPhone, OrderTime, DeliveryProvince, DeliveryAddress,
                    EmployeeID, EmployeeName, AcceptTime, ShipperID, ShipperName, ShippedTime, FinishedTime, Status, StatusDescription, TotalAmount
                FROM (
                    SELECT ROW_NUMBER() OVER (ORDER BY o.OrderTime DESC) AS rn,
                        o.OrderID, o.CustomerID, c.CustomerName, c.ContactName AS CustomerContactName,
                        c.Phone AS CustomerPhone,
                        o.OrderTime, o.DeliveryProvince, o.DeliveryAddress,
                        o.EmployeeID, CAST(e.FullName AS NVARCHAR(200)) AS EmployeeName, o.AcceptTime,
                        o.ShipperID, s.ShipperName, o.ShippedTime, o.FinishedTime,
                        o.Status, os.Description AS StatusDescription,
                        ISNULL((
                            SELECT SUM(CAST(od.Quantity AS decimal(18,4)) * CAST(od.SalePrice AS decimal(18,4)))
                            FROM OrderDetails od WHERE od.OrderID = o.OrderID
                        ), 0) AS TotalAmount
                    FROM Orders o
                    LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                    LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                    LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                    INNER JOIN OrderStatus os ON o.Status = os.Status
                    {where}
                ) x WHERE rn > @Offset AND rn <= @Offset + @Fetch",
                new { Search = input.SearchValue ?? "", Like = like, input.Status, OrderDateFrom = orderDateFrom, OrderDateTo = orderDateTo, input.CustomerID, ActiveOrdersOnly = activeOnly, Offset = input.Offset, Fetch = input.PageSize });
            result.DataItems = data.ToList();
            return result;
        }

        /// <summary>
        /// Lấy thông tin đơn hàng theo mã.
        /// </summary>
        /// <param name="orderId">Mã đơn hàng.</param>
        /// <returns>Thông tin đơn hàng hoặc null nếu không tìm thấy.</returns>
        public async Task<OrderViewInfo?> GetAsync(int orderId)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.QueryFirstOrDefaultAsync<OrderViewInfo>($@"
                {OrderSelect} WHERE o.OrderID=@orderId", new { orderId });
        }

        /// <summary>
        /// Lấy danh sách chi tiết đơn hàng.
        /// </summary>
        /// <param name="orderId">Mã đơn hàng.</param>
        /// <returns>Danh sách chi tiết đơn hàng.</returns>
        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderId)
        {
            await using var conn = new SqlConnection(_connectionString);
            var rows = await conn.QueryAsync<OrderDetailViewInfo>(@"
                SELECT od.OrderID, od.ProductID, p.ProductName, p.Unit, od.Quantity, od.SalePrice
                FROM OrderDetails od
                INNER JOIN Products p ON od.ProductID = p.ProductID
                WHERE od.OrderID=@orderId
                ORDER BY p.ProductName", new { orderId });
            return rows.ToList();
        }

        /// <summary>
        /// Lấy thông tin chi tiết đơn hàng theo mã.
        /// </summary>
        /// <param name="orderId">Mã đơn hàng.</param>
        /// <param name="productId">Mã mặt hàng.</param>
        /// <returns>Thông tin chi tiết đơn hàng hoặc null nếu không tìm thấy.</returns>
        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderId, int productId)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(@"
                SELECT od.OrderID, od.ProductID, p.ProductName, p.Unit, od.Quantity, od.SalePrice
                FROM OrderDetails od
                INNER JOIN Products p ON od.ProductID = p.ProductID
                WHERE od.OrderID=@orderId AND od.ProductID=@productId", new { orderId, productId });
        }

        /// <summary>
        /// Thêm đơn hàng mới.
        /// </summary>
        /// <param name="order">Thông tin đơn hàng.</param>
        /// <returns>Mã đơn hàng được tạo.</returns>
        public async Task<int> AddAsync(Order order)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO Orders (CustomerID, OrderTime, DeliveryProvince, DeliveryAddress, EmployeeID, AcceptTime, ShipperID, ShippedTime, FinishedTime, Status)
                VALUES (@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress, @EmployeeID, @AcceptTime, @ShipperID, @ShippedTime, @FinishedTime, @Status);
                SELECT CAST(SCOPE_IDENTITY() AS int);", order);
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng.
        /// </summary>
        /// <param name="order">Thông tin đơn hàng cần cập nhật.</param>
        /// <returns>True nếu cập nhật thành công, ngược lại False.</returns>
        public async Task<bool> UpdateAsync(Order order)
        {
            await using var conn = new SqlConnection(_connectionString);
            var n = await conn.ExecuteAsync(@"
                UPDATE Orders SET CustomerID=@CustomerID, OrderTime=@OrderTime, DeliveryProvince=@DeliveryProvince,
                    DeliveryAddress=@DeliveryAddress, EmployeeID=@EmployeeID, AcceptTime=@AcceptTime,
                    ShipperID=@ShipperID, ShippedTime=@ShippedTime, FinishedTime=@FinishedTime, Status=@Status
                WHERE OrderID=@OrderID", order);
            return n > 0;
        }

        /// <summary>
        /// Xóa đơn hàng.
        /// </summary>
        /// <param name="orderId">Mã đơn hàng.</param>
        /// <returns>True nếu xóa thành công, ngược lại False.</returns>
        public async Task<bool> DeleteAsync(int orderId)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.ExecuteAsync("DELETE FROM OrderDetails WHERE OrderID=@orderId", new { orderId });
            return await conn.ExecuteAsync("DELETE FROM Orders WHERE OrderID=@orderId", new { orderId }) > 0;
        }

        /// <summary>
        /// Thêm chi tiết đơn hàng mới.
        /// </summary>
        /// <param name="detail">Thông tin chi tiết đơn hàng.</param>
        /// <returns>True nếu thêm thành công, ngược lại False.</returns>
        public async Task<bool> AddDetailAsync(OrderDetail detail)
        {
            await using var conn = new SqlConnection(_connectionString);
            var exists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM OrderDetails WHERE OrderID=@OrderID AND ProductID=@ProductID", detail);
            if (exists > 0) return false;
            var n = await conn.ExecuteAsync(@"
                INSERT INTO OrderDetails (OrderID, ProductID, Quantity, SalePrice)
                VALUES (@OrderID, @ProductID, @Quantity, @SalePrice)", detail);
            return n > 0;
        }

                /// <summary>
        /// Cập nhật thông tin chi tiết đơn hàng.
        /// </summary>
        /// <param name="data">Thông tin chi tiết đơn hàng cần cập nhật.</param>
        /// <returns>True nếu cập nhật thành công, ngược lại False.</returns>
        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            await using var conn = new SqlConnection(_connectionString);
            var n = await conn.ExecuteAsync(@"
                UPDATE OrderDetails SET Quantity=@Quantity, SalePrice=@SalePrice
                WHERE OrderID=@OrderID AND ProductID=@ProductID", data);
            return n > 0;
        }

        /// <summary>
        /// Cập nhật trạng thái đơn hàng.
        /// </summary>
        /// <param name="orderID">Mã đơn hàng.</param>
        /// <param name="status">Trạng thái đơn hàng.</param>
        /// <param name="acceptTime">Thời gian nhận đơn hàng.</param>
        /// <param name="finishedTime">Thời gian hoàn thành đơn hàng.</param>
        /// <param name="employeeID">Mã nhân viên.</param>
        /// <returns>True nếu cập nhật thành công, ngược lại False.</returns>
        public async Task<bool> UpdateStatusAsync(int orderID, OrderStatusEnum status, DateTime? acceptTime, DateTime? finishedTime, int? employeeID)
        {
            await using var conn = new SqlConnection(_connectionString);
            var n = await conn.ExecuteAsync(@"
                UPDATE Orders SET
                    Status = @status,
                    AcceptTime = ISNULL(@acceptTime, AcceptTime),
                    FinishedTime = ISNULL(@finishedTime, FinishedTime),
                    EmployeeID = ISNULL(@employeeID, EmployeeID)
                WHERE OrderID = @orderID",
                new
                {
                    orderID,
                    status = (int)status,
                    acceptTime,
                    finishedTime,
                    employeeID
                });
            return n > 0;
        }

        /// <summary>
        /// Xóa chi tiết đơn hàng.
        /// </summary>
        /// <param name="orderId">Mã đơn hàng.</param>
        /// <param name="productId">Mã mặt hàng.</param>
        /// <returns>True nếu xóa thành công, ngược lại False.</returns>
        public async Task<bool> DeleteDetailAsync(int orderId, int productId)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.ExecuteAsync(
                "DELETE FROM OrderDetails WHERE OrderID=@orderId AND ProductID=@productId", new { orderId, productId }) > 0;
        }
    }
}
