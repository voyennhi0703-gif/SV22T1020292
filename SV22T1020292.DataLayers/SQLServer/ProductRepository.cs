using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020292.DataLayers.Interfaces;
using SV22T1020292.Models.Catalog;
using SV22T1020292.Models.Common;

namespace SV22T1020292.DataLayers.SQLServer
{
    /// <summary>
    /// Truy vấn Products, ProductAttributes, ProductPhotos.
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo với chuỗi kết nối SQL Server.
        /// </summary>
        public ProductRepository(string connectionString) => _connectionString = connectionString;

        private const string ProductColumns = @"
            ProductID, ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo,
            CAST(ISNULL(IsSelling, 0) AS bit) AS IsSelling";

        /// <summary>
        /// Lấy danh sách mặt hàng với phân trang.
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm và phân trang.</param>
        /// <returns>Kết quả phân trang.</returns>
        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            await using var conn = new SqlConnection(_connectionString);
            var like = SqlLike.Pattern(input.SearchValue);
            // 0 = "chưa chọn" trong form Admin; coi như không lọc (tránh WHERE CategoryID = 0 → 0 dòng)
            int? categoryId = input.CategoryID is null or 0 ? null : input.CategoryID;
            int? supplierId = input.SupplierID is null or 0 ? null : input.SupplierID;
            decimal? minPrice = input.MinPrice is null || input.MinPrice == 0 ? null : input.MinPrice;
            decimal? maxPrice = input.MaxPrice is null || input.MaxPrice == 0 ? null : input.MaxPrice;

            var where = @"(@Search = '' OR ProductName LIKE @Like OR ISNULL(ProductDescription,'') LIKE @Like)
                AND (@CategoryID IS NULL OR CategoryID = @CategoryID)
                AND (@SupplierID IS NULL OR SupplierID = @SupplierID)
                AND (@MinPrice IS NULL OR Price >= @MinPrice)
                AND (@MaxPrice IS NULL OR Price <= @MaxPrice)";
            var rowCount = await conn.ExecuteScalarAsync<int>($@"
                SELECT COUNT(*) FROM Products WHERE {where}",
                new { Search = input.SearchValue ?? "", Like = like, CategoryID = categoryId, SupplierID = supplierId, MinPrice = minPrice, MaxPrice = maxPrice });
            var result = new PagedResult<Product> { Page = input.Page, PageSize = input.PageSize, RowCount = rowCount };
            if (input.PageSize == 0)
            {
                var all = await conn.QueryAsync<Product>($@"
                    SELECT {ProductColumns} FROM Products WHERE {where} ORDER BY ProductName",
                    new { Search = input.SearchValue ?? "", Like = like, CategoryID = categoryId, SupplierID = supplierId, MinPrice = minPrice, MaxPrice = maxPrice });
                result.DataItems = all.ToList();
                return result;
            }
            var data = await conn.QueryAsync<Product>($@"
                SELECT {ProductColumns} FROM Products WHERE {where} ORDER BY ProductName
                OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY",
                new { Search = input.SearchValue ?? "", Like = like, CategoryID = categoryId, SupplierID = supplierId, MinPrice = minPrice, MaxPrice = maxPrice, Offset = input.Offset, Fetch = input.PageSize });
            result.DataItems = data.ToList();
            return result;
        }

        /// <summary>
        /// Lấy thông tin một mặt hàng.
        /// </summary>
        /// <param name="productId">Mã mặt hàng.</param>
        /// <returns>Mặt hàng hoặc null nếu không tìm thấy.</returns>
        public async Task<Product?> GetAsync(int productId)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.QueryFirstOrDefaultAsync<Product>($@"
                SELECT {ProductColumns} FROM Products WHERE ProductID=@productId", new { productId });
        }

        /// <summary>
        /// Thêm mới mặt hàng.
        /// </summary>
        /// <param name="data">Thông tin mặt hàng.</param>
        /// <returns>Mã mặt hàng được tạo.</returns>
        public async Task<int> AddAsync(Product data)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO Products (ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling)
                VALUES (@ProductName, @ProductDescription, @SupplierID, @CategoryID, @Unit, @Price, @Photo, @IsSelling);
                SELECT CAST(SCOPE_IDENTITY() AS int);", data);
        }

        /// <summary>
        /// Cập nhật thông tin mặt hàng.
        /// </summary>
        /// <param name="data">Thông tin mặt hàng cần cập nhật.</param>
        /// <returns>True nếu cập nhật thành công, ngược lại False.</returns>
        public async Task<bool> UpdateAsync(Product data)
        {
            await using var conn = new SqlConnection(_connectionString);
            var n = await conn.ExecuteAsync(@"
                UPDATE Products SET ProductName=@ProductName, ProductDescription=@ProductDescription,
                    SupplierID=@SupplierID, CategoryID=@CategoryID, Unit=@Unit, Price=@Price, Photo=@Photo, IsSelling=@IsSelling
                WHERE ProductID=@ProductID", data);
            return n > 0;
        }

        /// <summary>
        /// Xóa mặt hàng.
        /// </summary>
        /// <param name="productId">Mã mặt hàng cần xóa.</param>
        /// <returns>True nếu xóa thành công, ngược lại False.</returns>
        public async Task<bool> DeleteAsync(int productId)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.ExecuteAsync("DELETE FROM ProductAttributes WHERE ProductID=@productId", new { productId });
            await conn.ExecuteAsync("DELETE FROM ProductPhotos WHERE ProductID=@productId", new { productId });
            return await conn.ExecuteAsync("DELETE FROM Products WHERE ProductID=@productId", new { productId }) > 0;
        }

        /// <summary>
        /// Kiểm tra mặt hàng có đang được sử dụng hay không.
        /// </summary>
        /// <param name="productId">Mã mặt hàng.</param>
        /// <returns>True nếu đang được sử dụng, ngược lại False.</returns>
        public async Task<bool> IsUsedAsync(int productId)
        {
            await using var conn = new SqlConnection(_connectionString);
            var sql = @"
                SELECT CASE WHEN EXISTS(SELECT 1 FROM OrderDetails WHERE ProductID=@id)
                    OR EXISTS(SELECT 1 FROM ProductAttributes WHERE ProductID=@id)
                    OR EXISTS(SELECT 1 FROM ProductPhotos WHERE ProductID=@id)
                    THEN 1 ELSE 0 END";
            return await conn.ExecuteScalarAsync<int>(sql, new { id = productId }) == 1;
        }

        /// <summary>
        /// Lấy danh sách thuộc tính của một mặt hàng.
        /// </summary>
        /// <param name="productId">Mã mặt hàng.</param>
        /// <returns>Danh sách thuộc tính.</returns>
        public async Task<IReadOnlyList<ProductAttribute>> ListAttributesAsync(int productId)
        {
            await using var conn = new SqlConnection(_connectionString);
            var rows = await conn.QueryAsync<ProductAttribute>(@"
                SELECT AttributeID, ProductID, AttributeName, AttributeValue, DisplayOrder
                FROM ProductAttributes WHERE ProductID=@productId ORDER BY DisplayOrder, AttributeID",
                new { productId });
            return rows.ToList();
        }

        /// <summary>
        /// Lấy thông tin một thuộc tính của một mặt hàng.
        /// </summary>
        /// <param name="productId">Mã mặt hàng.</param>
        /// <param name="attributeId">Mã thuộc tính.</param>
        /// <returns>Thông tin thuộc tính hoặc null nếu không tìm thấy.</returns>
        public async Task<ProductAttribute?> GetAttributeAsync(int productId, long attributeId)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.QueryFirstOrDefaultAsync<ProductAttribute>(@"
                SELECT AttributeID, ProductID, AttributeName, AttributeValue, DisplayOrder
                FROM ProductAttributes WHERE ProductID=@productId AND AttributeID=@attributeId",
                new { productId, attributeId });
        }

                /// <summary>
        /// Thêm mới thuộc tính của một mặt hàng.
        /// </summary>
        /// <param name="data">Thông tin thuộc tính.</param>
        /// <returns>Mã thuộc tính được tạo.</returns>
        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.ExecuteScalarAsync<long>(@"
                INSERT INTO ProductAttributes (ProductID, AttributeName, AttributeValue, DisplayOrder)
                VALUES (@ProductID, @AttributeName, @AttributeValue, @DisplayOrder);
                SELECT CAST(SCOPE_IDENTITY() AS bigint);", data);
        }

        /// <summary>
        /// Cập nhật thông tin thuộc tính của một mặt hàng.
        /// </summary>
        /// <param name="data">Thông tin thuộc tính cần cập nhật.</param>
        /// <returns>True nếu cập nhật thành công, ngược lại False.</returns>
        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            await using var conn = new SqlConnection(_connectionString);
            var n = await conn.ExecuteAsync(@"
                UPDATE ProductAttributes SET AttributeName=@AttributeName, AttributeValue=@AttributeValue, DisplayOrder=@DisplayOrder
                WHERE AttributeID=@AttributeID AND ProductID=@ProductID", data);
            return n > 0;
        }

        /// <summary>
        /// Xóa thuộc tính của một mặt hàng.
        /// </summary>
        /// <param name="attributeId">Mã thuộc tính cần xóa.</param>
        /// <returns>True nếu xóa thành công, ngược lại False.</returns>
        public async Task<bool> DeleteAttributeAsync(long attributeId)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.ExecuteAsync("DELETE FROM ProductAttributes WHERE AttributeID=@attributeId", new { attributeId }) > 0;
        }

        /// <summary>
        /// Lấy danh sách ảnh của một mặt hàng.
        /// </summary>
        /// <param name="productId">Mã mặt hàng.</param>
        /// <returns>Danh sách ảnh.</returns>
        public async Task<IReadOnlyList<ProductPhoto>> ListPhotosAsync(int productId)
        {
            await using var conn = new SqlConnection(_connectionString);
            var rows = await conn.QueryAsync<ProductPhoto>(@"
                SELECT PhotoID, ProductID, Photo, Description, DisplayOrder, IsHidden
                FROM ProductPhotos WHERE ProductID=@productId ORDER BY DisplayOrder, PhotoID", new { productId });
            return rows.ToList();
        }

        /// <summary>
        /// Lấy thông tin một ảnh của một mặt hàng.
        /// </summary>
        /// <param name="productId">Mã mặt hàng.</param>
        /// <param name="photoId">Mã ảnh.</param>
        /// <returns>Thông tin ảnh hoặc null nếu không tìm thấy.</returns>
        public async Task<ProductPhoto?> GetPhotoAsync(int productId, long photoId)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.QueryFirstOrDefaultAsync<ProductPhoto>(@"
                SELECT PhotoID, ProductID, Photo, Description, DisplayOrder, IsHidden
                FROM ProductPhotos WHERE ProductID=@productId AND PhotoID=@photoId", new { productId, photoId });
        }

        /// <summary>
        /// Thêm mới ảnh của một mặt hàng.
        /// </summary>
        /// <param name="data">Thông tin ảnh.</param>
        /// <returns>Mã ảnh được tạo.</returns>
        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.ExecuteScalarAsync<long>(@"
                INSERT INTO ProductPhotos (ProductID, Photo, Description, DisplayOrder, IsHidden)
                VALUES (@ProductID, @Photo, @Description, @DisplayOrder, @IsHidden);
                SELECT CAST(SCOPE_IDENTITY() AS bigint);", data);
        }

        /// <summary>
        /// Cập nhật thông tin ảnh của một mặt hàng.
        /// </summary>
        /// <param name="data">Thông tin ảnh cần cập nhật.</param>
        /// <returns>True nếu cập nhật thành công, ngược lại False.</returns>
        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            await using var conn = new SqlConnection(_connectionString);
            var n = await conn.ExecuteAsync(@"
                UPDATE ProductPhotos SET Photo=@Photo, Description=@Description, DisplayOrder=@DisplayOrder, IsHidden=@IsHidden
                WHERE PhotoID=@PhotoID AND ProductID=@ProductID", data);
            return n > 0;
        }

        /// <summary>
        /// Xóa ảnh của một mặt hàng.
        /// </summary>
        /// <param name="photoId">Mã ảnh cần xóa.</param>
        /// <returns>True nếu xóa thành công, ngược lại False.</returns>
        public async Task<bool> DeletePhotoAsync(long photoId)
        {
            await using var conn = new SqlConnection(_connectionString);
            return await conn.ExecuteAsync("DELETE FROM ProductPhotos WHERE PhotoID=@photoId", new { photoId }) > 0;
        }

        /// <summary>
        /// Lấy danh sách mặt hàng nổi bật.
        /// </summary>
        /// <param name="topPerCategory">Số lượng mặt hàng nổi bật trên mỗi loại.</param>
        /// <returns>Danh sách mặt hàng nổi bật.</returns>
        public async Task<IReadOnlyList<Product>> GetFeaturedProductsAsync(int topPerCategory = 1)
        {
            await using var conn = new SqlConnection(_connectionString);
            var sql = $@"
                WITH R AS (
                    SELECT ProductID, ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo,
                        CAST(ISNULL(IsSelling, 0) AS bit) AS IsSelling,
                        ROW_NUMBER() OVER (PARTITION BY CategoryID ORDER BY Price DESC) AS rk
                    FROM Products
                    WHERE ISNULL(IsSelling, 0) = 1
                )
                SELECT ProductID, ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling
                FROM R WHERE rk <= @topPerCategory
                ORDER BY CategoryID, Price DESC";
            var rows = await conn.QueryAsync<Product>(sql, new { topPerCategory });
            return rows.ToList();
        }

            /// <summary>
        /// Lấy danh sách mặt hàng liên quan.
        /// </summary>
        /// <param name="categoryID">Mã loại hàng.</param>
        /// <param name="currentProductID">Mã mặt hàng hiện tại.</param>
        /// <param name="take">Số lượng mặt hàng lấy ra.</param>
        /// <returns>Danh sách mặt hàng liên quan.</returns>
        public async Task<IReadOnlyList<Product>> GetRelatedProductsAsync(int categoryID, int currentProductID, int take = 4)
        {
            await using var conn = new SqlConnection(_connectionString);
            var rows = await conn.QueryAsync<Product>($@"
                SELECT TOP (@take) {ProductColumns}
                FROM Products
                WHERE CategoryID = @categoryID AND ProductID <> @currentProductID AND ISNULL(IsSelling, 0) = 1
                ORDER BY ProductName",
                new { categoryID, currentProductID, take });
            return rows.ToList();
        }
    }
}
