using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020292.DataLayers.Interfaces;
using SV22T1020292.Models.Common;
using SV22T1020292.Models.Partner;

namespace SV22T1020292.DataLayers.SQLServer
{
    /// <summary>
    /// Truy vấn bảng Shippers.
    /// </summary>
    public class ShipperRepository : IGenericRepository<Shipper>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo với chuỗi kết nối SQL Server.
        /// </summary>
        public ShipperRepository(string connectionString) => _connectionString = connectionString;

        /// <summary>
        /// Lấy danh sách shipper với phân trang.
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm và phân trang.</param>
        /// <returns>Kết quả phân trang.</returns>
        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            await using var conn = new SqlConnection(_connectionString);
            var like = SqlLike.Pattern(input.SearchValue);
            const string where = @"(@Search = '' OR ShipperName LIKE @Like OR ISNULL(Phone,'') LIKE @Like)";
            var rowCount = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Shippers WHERE {where}", new { Search = input.SearchValue ?? "", Like = like });
            var result = new PagedResult<Shipper> { Page = input.Page, PageSize = input.PageSize, RowCount = rowCount };
            if (input.PageSize == 0)
            {
                var all = await conn.QueryAsync<Shipper>($"SELECT * FROM Shippers WHERE {where} ORDER BY ShipperName", new { Search = input.SearchValue ?? "", Like = like });
                result.DataItems = all.ToList();
                return result;
            }
            var data = await conn.QueryAsync<Shipper>($@"
                SELECT * FROM Shippers WHERE {where} ORDER BY ShipperName OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY",
                new { Search = input.SearchValue ?? "", Like = like, Offset = input.Offset, Fetch = input.PageSize });
            result.DataItems = data.ToList();
            return result;
        }

        /// <summary>
        /// Lấy thông tin một shipper.
        /// </summary>
        /// <param name="id">Mã shipper.</param>
        /// <returns>Shipper hoặc null nếu không tìm thấy.</returns>
        public async Task<Shipper?> GetAsync(int id)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.QueryFirstOrDefaultAsync<Shipper>("SELECT * FROM Shippers WHERE ShipperID=@id", new { id });
        }

        /// <summary>
        /// Thêm mới shipper.
        /// </summary>
        /// <param name="data">Thông tin shipper.</param>
        /// <returns>Mã shipper được tạo.</returns>
        public async Task<int> AddAsync(Shipper data)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO Shippers (ShipperName, Phone) VALUES (@ShipperName, @Phone);
                SELECT CAST(SCOPE_IDENTITY() AS int);", data);
        }

        /// <summary>
        /// Cập nhật thông tin shipper.
        /// </summary>
        /// <param name="data">Thông tin shipper cần cập nhật.</param>
        /// <returns>True nếu cập nhật thành công, ngược lại False.</returns>
        public async Task<bool> UpdateAsync(Shipper data)
        {
            await using var conn = new SqlConnection(_connectionString);
            var n = await conn.ExecuteAsync("UPDATE Shippers SET ShipperName=@ShipperName, Phone=@Phone WHERE ShipperID=@ShipperID", data);
            return n > 0;
        }

        /// <summary>
        /// Xóa shipper.
        /// </summary>
        /// <param name="id">Mã shipper cần xóa.</param>
        /// <returns>True nếu xóa thành công, ngược lại False.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.ExecuteAsync("DELETE FROM Shippers WHERE ShipperID=@id", new { id }) > 0;
        }

        /// <summary>
        /// Kiểm tra shipper có đang được sử dụng hay không.
        /// </summary>
        /// <param name="id">Mã shipper.</param>
        /// <returns>True nếu đang được sử dụng, ngược lại False.</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.ExecuteScalarAsync<int>(
                "SELECT CASE WHEN EXISTS(SELECT 1 FROM Orders WHERE ShipperID=@id) THEN 1 ELSE 0 END", new { id }) == 1;
        }
    }
}
