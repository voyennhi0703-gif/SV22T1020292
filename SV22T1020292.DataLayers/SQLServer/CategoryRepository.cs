using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020292.DataLayers.Interfaces;
using SV22T1020292.Models.Catalog;
using SV22T1020292.Models.Common;

namespace SV22T1020292.DataLayers.SQLServer
{
    /// <summary>
    /// Truy vấn bảng Categories.
    /// </summary>
    public class CategoryRepository : IGenericRepository<Category>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo với chuỗi kết nối SQL Server.
        /// </summary>
        public CategoryRepository(string connectionString) => _connectionString = connectionString;

        /// <summary>
        /// Lấy danh sách danh mục với phân trang.
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm và phân trang.</param>
        /// <returns>Kết quả phân trang.</returns>
        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            await using var conn = new SqlConnection(_connectionString);
            var like = SqlLike.Pattern(input.SearchValue);
            const string where = @"(@Search = '' OR CategoryName LIKE @Like OR ISNULL(Description,'') LIKE @Like)";
            var rowCount = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Categories WHERE {where}", new { Search = input.SearchValue ?? "", Like = like });
            var result = new PagedResult<Category> { Page = input.Page, PageSize = input.PageSize, RowCount = rowCount };
            if (input.PageSize == 0)
            {
                var all = await conn.QueryAsync<Category>($"SELECT * FROM Categories WHERE {where} ORDER BY CategoryName", new { Search = input.SearchValue ?? "", Like = like });
                result.DataItems = all.ToList();
                return result;
            }
            var data = await conn.QueryAsync<Category>($@"
                SELECT * FROM Categories WHERE {where} ORDER BY CategoryName OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY",
                new { Search = input.SearchValue ?? "", Like = like, Offset = input.Offset, Fetch = input.PageSize });
            result.DataItems = data.ToList();
            return result;
        }

        /// <summary>
        /// Lấy thông tin danh mục theo mã.
        /// </summary>
        /// <param name="id">Mã danh mục.</param>
        /// <returns>Danh mục hoặc null nếu không tìm thấy.</returns>
        public async Task<Category?> GetAsync(int id)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.QueryFirstOrDefaultAsync<Category>("SELECT * FROM Categories WHERE CategoryID=@id", new { id });
        }

        /// <summary>
        /// Thêm danh mục mới.
        /// </summary>
        /// <param name="data">Thông tin danh mục.</param>
        /// <returns>Mã danh mục được tạo.</returns>
        public async Task<int> AddAsync(Category data)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO Categories (CategoryName, Description) VALUES (@CategoryName, @Description);
                SELECT CAST(SCOPE_IDENTITY() AS int);", data);
        }

        /// <summary>
        /// Cập nhật thông tin danh mục.
        /// </summary>
        /// <param name="data">Thông tin danh mục cần cập nhật.</param>
        /// <returns>True nếu cập nhật thành công, ngược lại False.</returns>
        public async Task<bool> UpdateAsync(Category data)
        {
            await using var conn = new SqlConnection(_connectionString);
            var n = await conn.ExecuteAsync("UPDATE Categories SET CategoryName=@CategoryName, Description=@Description WHERE CategoryID=@CategoryID", data);
            return n > 0;
        }

        /// <summary>
        /// Xóa danh mục.
        /// </summary>
        /// <param name="id">Mã danh mục cần xóa.</param>
        /// <returns>True nếu xóa thành công, ngược lại False.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.ExecuteAsync("DELETE FROM Categories WHERE CategoryID=@id", new { id }) > 0;
        }

            /// <summary>
        /// Kiểm tra danh mục có đang được sử dụng hay không.
        /// </summary>
        /// <param name="id">Mã danh mục.</param>
        /// <returns>True nếu đang được sử dụng, ngược lại False.</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.ExecuteScalarAsync<int>(
                "SELECT CASE WHEN EXISTS(SELECT 1 FROM Products WHERE CategoryID=@id) THEN 1 ELSE 0 END", new { id }) == 1;
        }
    }
}
