namespace SV22T1020292.Models.Sales
{
    /// <summary>
    /// �on h�ng
    /// </summary>
    public class Order
    {
        /// <summary>
        /// M� don h�ng
        /// </summary>
        public int OrderID { get; set; }
        /// <summary>
        /// M� kh�ch h�ng
        /// </summary>
        public int? CustomerID { get; set; }
        /// <summary>
        /// Th?i di?m d?t h�ng (th?i di?m t?o don h�ng)
        /// </summary>
        public DateTime OrderTime { get; set; }
        /// <summary>
        /// T?nh/th�nh giao h�ng
        /// </summary>
        public string? DeliveryProvince { get; set; }
        /// <summary>
        /// �?a ch? giao h�ng
        /// </summary>
        public string? DeliveryAddress { get; set; }
        /// <summary>
        /// M� nh�n vi�n x? l� don h�ng (ngu?i nh?n/duy?t don h�ng)
        /// </summary>
        public int? EmployeeID { get; set; }
        /// <summary>
        /// Th?i di?m duy?t don h�ng (th?i di?m nh�n vi�n nh?n/duy?t don h�ng)
        /// </summary>
        public DateTime? AcceptTime { get; set; }
        /// <summary>
        /// M� ngu?i giao h�ng
        /// </summary>
        public int? ShipperID { get; set; }
        /// <summary>
        /// Th?i di?m ngu?i giao h�ng nh?n don h�ng d? giao
        /// </summary>
        public DateTime? ShippedTime { get; set; }
        /// <summary>
        /// Th?i di?m k?t th�c don h�ng
        /// </summary>
        public DateTime? FinishedTime { get; set; }
        /// <summary>
        /// Trạng thái hiện tại của đơn hàng (giá trị int theo OrderStatusEnum)
        /// </summary>
        public int Status { get; set; }
    }
}
