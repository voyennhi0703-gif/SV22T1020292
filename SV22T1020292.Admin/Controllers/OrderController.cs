using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SV22T1020292.BusinessLayers;
using SV22T1020292.Models.Common;
using SV22T1020292.Models.Sales;
using SV22T1020292.Models.Catalog;
using SV22T1020292.Models.Partner;
using System.Globalization;

namespace SV22T1020292.Admin.Controllers
{
    /// <summary>
    /// Controller quản lý đơn hàng
    /// </summary>
    public class OrderController : Controller
    {
        private const int ORDER_PAGE_SIZE = 15;
        private const int PRODUCT_PAGE_SIZE = 20;
        private const string ORDER_SEARCH_SESSION = "OrderSearchInput";
        private const string PRODUCT_SEARCH_SESSION = "ProductSearchForOrder";

        // --- QUẢN LÝ DANH SÁCH & TRA CỨU ---

        /// <summary>
        /// Hiển thị danh sách đơn hàng
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<OrderSearchInput>(ORDER_SEARCH_SESSION);
            if (input == null)
            {
                input = new OrderSearchInput
                {
                    Page = 1,
                    PageSize = ORDER_PAGE_SIZE,
                    SearchValue = "",
                    Status = 0,
                    DateFrom = null,
                    DateTo = null
                };
            }
            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và lọc đơn hàng
        /// </summary>
        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            var result = await SalesDataService.ListOrdersAsync(input);
            ApplicationContext.SetSessionData(ORDER_SEARCH_SESSION, input);
            return PartialView(result);
        }

        // --- LẬP ĐƠN HÀNG (GIỎ HÀNG) ---

        /// <summary>
        /// Hiển thị giao diện tạo đơn hàng (giỏ hàng)
        /// </summary>
        public async Task<IActionResult> Create(int page = 1, string searchValue = "")
        {
            // Luôn đồng bộ session theo query: nếu searchValue rỗng → xóa lọc, hiển thị toàn bộ mặt hàng (không giữ từ khóa cũ trong session).
            searchValue = searchValue?.Trim() ?? "";
            var productSearch = new SV22T1020292.Models.Common.ProductSearchInput
            {
                Page = page < 1 ? 1 : page,
                PageSize = PRODUCT_PAGE_SIZE,
                SearchValue = searchValue
            };
            ApplicationContext.SetSessionData(PRODUCT_SEARCH_SESSION, productSearch);

            // Lấy danh sách sản phẩm phân trang
            var products = await CatalogDataService.ListProductsAsync(productSearch);
            ViewBag.Products = products;
            ViewBag.ProductSearch = productSearch.SearchValue;

            // Lấy danh sách khách hàng
            var customersResult = await PartnerDataService.ListCustomersAsync(new SV22T1020292.Models.Common.PaginationSearchInput { Page = 1, PageSize = 0, SearchValue = "" });
            ViewBag.Customers = customersResult.DataItems;

            // Lấy danh sách tỉnh/thành
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();

            var cart = ShoppingCartService.GetShoppingCart()
                .Select(x => new SV22T1020292.Models.Common.OrderCartLine
                {
                    ProductID = x.ProductID,
                    ProductName = x.ProductName,
                    Unit = x.Unit,
                    Quantity = x.Quantity,
                    SalePrice = x.SalePrice
                }).ToList();
            ViewBag.Cart = cart;

            return View();
        }

        /// <summary>
        /// Tìm kiếm mặt hàng để thêm vào đơn hàng
        /// </summary>
        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCT_SEARCH_SESSION, input);
            return PartialView(result);
        }

        /// <summary>
        /// Hiển thị giỏ hàng
        /// </summary>
        public IActionResult ShowShoppingCart()
        {
            var cart = ShoppingCartService.GetShoppingCart();
            return PartialView("ShowShoppingCart", cart);
        }

        /// <summary>
        /// Thêm mặt hàng vào giỏ hàng (form POST từ trang Lập đơn hàng).
        /// Giá lấy từ CSDL nếu form không gửi <see cref="OrderDetailViewInfo.SalePrice"/>.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(OrderDetailViewInfo item, int? returnPage, string? returnSearch)
        {
            if (item.Quantity <= 0)
            {
                TempData["ErrorMessage"] = "Số lượng không hợp lệ.";
                return RedirectToOrderCreate(returnPage, returnSearch);
            }

            var product = await CatalogDataService.GetProductAsync(item.ProductID);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy mặt hàng.";
                return RedirectToOrderCreate(returnPage, returnSearch);
            }

            if (!product.IsSelling)
            {
                TempData["ErrorMessage"] = "Mặt hàng đang ngừng bán.";
                return RedirectToOrderCreate(returnPage, returnSearch);
            }

            if (item.SalePrice <= 0)
                item.SalePrice = product.Price;

            item.ProductName = product.ProductName;
            item.Unit = product.Unit;
            item.Photo = product.Photo ?? "";

            ShoppingCartService.AddCartItem(item);
            TempData["SuccessMessage"] = "Đã thêm mặt hàng vào giỏ.";
            return RedirectToOrderCreate(returnPage, returnSearch);
        }

        /// <summary>
        /// Xóa mặt hàng khỏi giỏ hàng (form POST; tham số <c>productId</c> khớp view).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int productId, int? returnPage, string? returnSearch)
        {
            ShoppingCartService.RemoveCartItem(productId);
            return RedirectToOrderCreate(returnPage, returnSearch);
        }

        /// <summary>
        /// Xóa toàn bộ giỏ hàng (form POST).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearCart(int? returnPage, string? returnSearch)
        {
            ShoppingCartService.ClearCart();
            TempData["SuccessMessage"] = "Đã xóa toàn bộ giỏ hàng.";
            return RedirectToOrderCreate(returnPage, returnSearch);
        }

        private IActionResult RedirectToOrderCreate(int? returnPage, string? returnSearch) =>
            RedirectToAction(nameof(Create), new { page = returnPage.GetValueOrDefault(1), searchValue = returnSearch ?? "" });

        /// <summary>
        /// Cập nhật số lượng và giá của mặt hàng trong giỏ hàng
        /// </summary>
        [HttpPost]
        public IActionResult UpdateCartItem(int id, int quantity, decimal salePrice)
        {
            if (quantity <= 0 || salePrice <= 0)
                return Json("Số lượng và giá bán không hợp lệ");

            ShoppingCartService.UpdateCartItem(id, quantity, salePrice);
            return Json("");
        }

        /// <summary>
        /// Khởi tạo đơn hàng (POST) - nhận dữ liệu từ form lập đơn hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(string? customerId, string? deliveryProvince, string? deliveryAddress, int? returnPage, string? returnSearch)
        {
            var cart = ShoppingCartService.GetShoppingCart();
            if (cart.Count == 0)
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống. Vui lòng chọn mặt hàng.";
                return RedirectToOrderCreate(returnPage, returnSearch);
            }

            if (string.IsNullOrWhiteSpace(deliveryProvince) || string.IsNullOrWhiteSpace(deliveryAddress))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ thông tin giao hàng.";
                return RedirectToOrderCreate(returnPage, returnSearch);
            }

            int customerID = 0;

            // Ưu tiên: chọn khách hàng có sẵn từ dropdown
            if (!string.IsNullOrEmpty(customerId) && int.TryParse(customerId, out int parsedCustomerId) && parsedCustomerId > 0)
            {
                customerID = parsedCustomerId;
            }
            // Ngược lại: tạo khách hàng mới với thông tin cơ bản
            else
            {
                // Tạo tên khách hàng tạm thời
                var tempName = $"Khách lẻ {DateTime.Now:yyyyMMddHHmmss}";
                customerID = await PartnerDataService.AddCustomerAsync(new SV22T1020292.Models.Partner.Customer
                {
                    CustomerName = tempName,
                    ContactName = tempName,
                    Province = deliveryProvince,
                    Address = deliveryAddress,
                    Email = ""
                });
            }

            if (customerID <= 0)
            {
                TempData["ErrorMessage"] = "Không thể xác định hoặc tạo khách hàng.";
                return RedirectToOrderCreate(returnPage, returnSearch);
            }

            // Đơn mới: không gán nhân viên phụ trách — chỉ gán khi duyệt đơn (Accept).
            Order order = new Order
            {
                CustomerID = customerID,
                OrderTime = DateTime.Now,
                DeliveryProvince = deliveryProvince,
                DeliveryAddress = deliveryAddress,
                EmployeeID = null,
                Status = (int)OrderStatusEnum.New
            };

            int orderID = await SalesDataService.AddOrderAsync(order);
            if (orderID > 0)
            {
                foreach (var item in cart)
                {
                    await SalesDataService.AddDetailAsync(new OrderDetail
                    {
                        OrderID = orderID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice
                    });
                }
                ShoppingCartService.ClearCart();
                TempData["SuccessMessage"] = $"Đã lập đơn hàng #{orderID} thành công.";
                return RedirectToAction(nameof(Detail), new { id = orderID });
            }

            TempData["ErrorMessage"] = "Không thể lập đơn hàng.";
            return RedirectToOrderCreate(returnPage, returnSearch);
        }

        /// <summary>
        /// Hiển thị chi tiết đơn hàng
        /// </summary>
        public async Task<IActionResult> Detail(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return RedirectToAction("Index");

            var details = await SalesDataService.ListDetailsAsync(id);
            var customer = order.CustomerID > 0
                ? await PartnerDataService.GetCustomerAsync(order.CustomerID.Value)
                : null;

            var model = new SV22T1020292.Admin.Models.OrderDetailPageModel
            {
                Order = order,
                Customer = customer,
                Details = details
            };
            return View(model);
        }

        /// <summary>
        /// Duyệt đơn hàng
        /// </summary>
        public async Task<IActionResult> Accept(int id, int? employeeID)
        {
            if (Request.Method == "GET")
            {
                var employeesResult = await HRDataService.ListEmployeesAsync(
                    new PaginationSearchInput { Page = 1, PageSize = 0, SearchValue = "" });

                var model = new SV22T1020292.Admin.Models.OrderAcceptDialogModel
                {
                    OrderId = id,
                    Employees = employeesResult.DataItems.ToList()
                };
                return View(model);
            }

            if (employeeID == null || employeeID <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn nhân viên phụ trách.";
                return RedirectToAction("Detail", new { id });
            }

            bool result = await SalesDataService.AcceptOrderAsync(id, employeeID.Value);
            if (!result)
                TempData["ErrorMessage"] = "Không thể duyệt đơn hàng này.";
            else
                TempData["SuccessMessage"] = "Duyệt đơn hàng thành công.";

            return RedirectToAction("Detail", new { id });
        }

        /// <summary>
        /// Chuyển trạng thái giao hàng
        /// </summary>
        public async Task<IActionResult> Shipping(int id, int? shipperID)
        {
            if (Request.Method == "GET")
            {
                var shippersResult = await PartnerDataService.ListShippersAsync(
                    new PaginationSearchInput { Page = 1, PageSize = 0, SearchValue = "" });

                var model = new SV22T1020292.Admin.Models.OrderShippingDialogModel
                {
                    OrderId = id,
                    Shippers = shippersResult.DataItems.ToList()
                };
                return View(model);
            }

            if (shipperID == null || shipperID <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn người giao hàng.";
                return RedirectToAction("Detail", new { id });
            }

            bool result = await SalesDataService.ShipOrderAsync(id, shipperID.Value);
            if (!result)
                TempData["ErrorMessage"] = "Không thể chuyển trạng thái sang đang giao hàng.";
            else
                TempData["SuccessMessage"] = "Đã chuyển đơn hàng sang trạng thái đang giao hàng.";

            return RedirectToAction("Detail", new { id });
        }

        /// <summary>
        /// Hộp thoại xác nhận hoàn tất đơn hàng (GET — nội dung load vào modal).
        /// </summary>
        [HttpGet]
        public IActionResult Finish(int id)
        {
            return View("Finish", id);
        }

        /// <summary>
        /// Hoàn tất đơn hàng (POST).
        /// </summary>
        [HttpPost]
        [ActionName("Finish")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinishConfirmed(int id)
        {
            bool result = await SalesDataService.CompleteOrderAsync(id);
            if (!result)
                TempData["ErrorMessage"] = "Không thể hoàn tất đơn hàng này.";
            else
                TempData["SuccessMessage"] = "Đơn hàng đã hoàn tất thành công.";
            return RedirectToAction("Detail", new { id });
        }

        /// <summary>
        /// Từ chối đơn hàng
        /// </summary>
        public async Task<IActionResult> Reject(int id)
        {
            int employeeID = 1;
            bool result = await SalesDataService.RejectOrderAsync(id, employeeID);
            if (!result) TempData["ErrorMessage"] = "Không thể từ chối đơn hàng này.";
            else TempData["SuccessMessage"] = "Đã từ chối đơn hàng thành công.";
            return RedirectToAction("Detail", new { id });
        }

        /// <summary>
        /// Hộp thoại xác nhận hủy đơn (GET — load vào modal).
        /// </summary>
        [HttpGet]
        public IActionResult Cancel(int id)
        {
            return View("Cancel", id);
        }

        /// <summary>
        /// Hủy đơn hàng (POST).
        /// </summary>
        [HttpPost]
        [ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            bool result = await SalesDataService.CancelOrderAsync(id);
            if (!result)
                TempData["ErrorMessage"] = "Không thể hủy đơn hàng này.";
            else
                TempData["SuccessMessage"] = "Đã hủy đơn hàng thành công.";
            return RedirectToAction("Detail", new { id });
        }

        /// <summary>
        /// Xóa vĩnh viễn đơn hàng
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            bool result = await SalesDataService.DeleteOrderAsync(id);
            if (!result) TempData["ErrorMessage"] = "Không thể xóa đơn hàng này.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Chỉnh sửa mặt hàng trong đơn hàng (chỉ khi đơn hàng chưa được duyệt)
        /// </summary>
        public async Task<IActionResult> EditCartItem(int id, int productId)
        {
            var detail = await SalesDataService.GetDetailAsync(id, productId);
            return View(detail);
        }

        /// <summary>
        /// Cập nhật chi tiết mặt hàng trong đơn hàng
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> UpdateCartItem(OrderDetail data)
        {
            if (data.Quantity <= 0 || data.SalePrice <= 0)
                return Json("Số lượng và giá bán không hợp lệ");
            
            bool result = await SalesDataService.UpdateDetailAsync(data);
            if (!result) return Json("Không thể cập nhật chi tiết đơn hàng");

            return Json("");
        }

        /// <summary>
        /// Xóa mặt hàng khỏi đơn hàng
        /// </summary>
        /// <param name="id"></param>
        /// <param name="productId"></param>
        /// <returns></returns>
        public async Task<IActionResult> DeleteCartItem(int id, int productId)
        {
            bool result = await SalesDataService.DeleteDetailAsync(id, productId);
            if (!result) TempData["ErrorMessage"] = "Không thể xóa mặt hàng khỏi đơn hàng";
            return RedirectToAction("Detail", new { id });
        }
    }
}