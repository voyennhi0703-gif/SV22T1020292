using SV22T1020292.Models.Catalog;
using SV22T1020292.Models.Common;

namespace SV22T1020292.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu mặt hàng, thuộc tính và ảnh.
    /// </summary>
    public interface IProductRepository
    {
        Task<PagedResult<Product>> ListAsync(ProductSearchInput input);
        Task<Product?> GetAsync(int productId);
        Task<int> AddAsync(Product data);
        Task<bool> UpdateAsync(Product data);
        Task<bool> DeleteAsync(int productId);
        Task<bool> IsUsedAsync(int productId);

        Task<IReadOnlyList<ProductAttribute>> ListAttributesAsync(int productId);
        Task<ProductAttribute?> GetAttributeAsync(int productId, long attributeId);
        Task<long> AddAttributeAsync(ProductAttribute data);
        Task<bool> UpdateAttributeAsync(ProductAttribute data);
        Task<bool> DeleteAttributeAsync(long attributeId);

        Task<IReadOnlyList<ProductPhoto>> ListPhotosAsync(int productId);
        Task<ProductPhoto?> GetPhotoAsync(int productId, long photoId);
        Task<long> AddPhotoAsync(ProductPhoto data);
        Task<bool> UpdatePhotoAsync(ProductPhoto data);
        Task<bool> DeletePhotoAsync(long photoId);

        /// <summary>Lấy danh sách sản phẩm nổi bật (tối đa N sản phẩm giá cao nhất mỗi danh mục).</summary>
        Task<IReadOnlyList<Product>> GetFeaturedProductsAsync(int topPerCategory = 1);

        /// <summary>Lấy sản phẩm cùng danh mục (trừ sản phẩm hiện tại).</summary>
        Task<IReadOnlyList<Product>> GetRelatedProductsAsync(int categoryID, int currentProductID, int take = 4);
    }
}
