using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020292.DataLayers.Interfaces;
using SV22T1020292.Models.DataDictionary;

namespace SV22T1020292.DataLayers.SQLServer
{
    /// <summary>
    /// Truy vấn bảng Provinces (từ điển tỉnh/thành).
    /// </summary>
    public class ProvinceRepository : IDataDictionaryRepository<Province>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo với chuỗi kết nối SQL Server.
        /// </summary>
        public ProvinceRepository(string connectionString) => _connectionString = connectionString;

        /// <summary>
        /// Lấy danh sách tỉnh/thành.
        /// </summary>
        /// <returns>Danh sách tỉnh/thành.</returns>
        public async Task<List<Province>> ListAsync()
        {
            await using var conn = new SqlConnection(_connectionString);
            var rows = await conn.QueryAsync<Province>("SELECT ProvinceName FROM Provinces ORDER BY ProvinceName");
            return rows.ToList();
        }
    }
}
