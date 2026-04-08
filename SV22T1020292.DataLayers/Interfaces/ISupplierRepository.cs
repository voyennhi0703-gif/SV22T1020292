using SV22T1020292.Models.Common;
using SV22T1020292.Models.Partner;

namespace SV22T1020292.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu trên Supplier (nhà cung cấp).
    /// Kế thừa IGenericRepository và bổ sung phương thức kiểm tra email trùng.
    /// </summary>
    public interface ISupplierRepository : IGenericRepository<Supplier>
    {
        /// <summary>
        /// Kiểm tra xem email đã được sử dụng bởi nhà cung cấp khác chưa.
        /// </summary>
        /// <param name="email">Email cần kiểm tra.</param>
        /// <param name="id">
        /// Bằng 0: kiểm tra email đối với nhà cung cấp mới.
        /// Khác 0: kiểm tra email đối với nhà cung cấp có mã là id.
        /// </param>
        /// <returns>
        /// true nếu email hợp lệ (không trùng), false nếu email đã tồn tại.
        /// </returns>
        Task<bool> ValidateEmailAsync(string email, int id = 0);
    }
}
