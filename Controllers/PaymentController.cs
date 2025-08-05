using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;
using System.Security.Claims;
using System.Text;

namespace shopping_tutorial.Controllers
{
    public class PaymentController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IConfiguration _configuration;

        public PaymentController(DataContext context, IConfiguration configuration)
        {
            _dataContext = context;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePaymentMomo(string FullName, decimal Amount, string OrderInfo)
        {
            // Kiểm tra user đã đăng nhập
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (userEmail == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra giỏ hàng
            var cartitems = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
            if (cartitems.Count == 0) 
            {
                TempData["error"] = "Giỏ hàng trống!";
                return RedirectToAction("Index", "Cart");
            }

            try
            {
                // Tính tổng tiền từ giỏ hàng, bao gồm phí ship và giảm giá
                decimal grandTotal = cartitems.Sum(x => x.Quantity * x.Price);
                decimal shippingCost = 0;
                decimal discount = 0;

                // Lấy phí ship từ cookie
                if (Request.Cookies.TryGetValue("ShippingPrice", out string shipCookie))
                {
                    shippingCost = JsonConvert.DeserializeObject<decimal>(shipCookie);
                }

                // Lấy giảm giá từ cookie
                if (Request.Cookies.TryGetValue("CouponApplied", out var discountStr))
                {
                    discount = decimal.TryParse(discountStr, out var parsed) ? parsed : 0;
                }

                decimal finalAmount = grandTotal + shippingCost - discount;

                var orderId = Guid.NewGuid().ToString();
                var amount = ((long)finalAmount).ToString(); // Chuyển thành long rồi về string

                // Validate amount không được rỗng và phải > 0
                if (string.IsNullOrEmpty(amount) || finalAmount <= 0)
                {
                    TempData["error"] = "Số tiền thanh toán không hợp lệ";
                    return RedirectToAction("Index", "Cart");
                }

                // Load Momo Config
                var endpoint = _configuration["MomoAPI:MomoApiUrl"];
                var partnerCode = _configuration["MomoAPI:PartnerCode"];
                var accessKey = _configuration["MomoAPI:AccessKey"];
                var secretKey = _configuration["MomoAPI:SecretKey"];
                var returnUrl = _configuration["MomoAPI:ReturnUrl"];
                var notifyUrl = _configuration["MomoAPI:NotifyUrl"];
                var requestType = _configuration["MomoAPI:RequestType"];

                // Kiểm tra config có đầy đủ không
                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(partnerCode) || 
                    string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey) ||
                    string.IsNullOrEmpty(returnUrl) || string.IsNullOrEmpty(notifyUrl) ||
                    string.IsNullOrEmpty(requestType))
                {
                    Console.WriteLine($"Missing config - endpoint: {endpoint}, partnerCode: {partnerCode}, accessKey: {accessKey}, secretKey: {secretKey?.Substring(0, 5)}..., returnUrl: {returnUrl}, notifyUrl: {notifyUrl}, requestType: {requestType}");
                    TempData["error"] = "Cấu hình MoMo chưa đầy đủ trong appsettings.json";
                    return RedirectToAction("Index", "Cart");
                }

                var requestId = Guid.NewGuid().ToString();
                var orderInfo = $"Thanh toán đơn hàng tại LocalBrand - {finalAmount:N0} VNĐ";
                var extraData = ""; // MoMo yêu cầu không được null

                // Debug: In ra các giá trị
                Console.WriteLine($"Creating MoMo request with:");
                Console.WriteLine($"  partnerCode: {partnerCode}");
                Console.WriteLine($"  accessKey: {accessKey}");
                Console.WriteLine($"  requestId: {requestId}");
                Console.WriteLine($"  amount: {amount}");
                Console.WriteLine($"  orderId: {orderId}");
                Console.WriteLine($"  orderInfo: {orderInfo}");
                Console.WriteLine($"  returnUrl: {returnUrl}");
                Console.WriteLine($"  notifyUrl: {notifyUrl}");
                Console.WriteLine($"  extraData: '{extraData}'");
                Console.WriteLine($"  requestType: {requestType}");

                // Tạo rawHash theo định dạng mới của MoMo API v2
                string rawHash = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={notifyUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType={requestType}";
                var signature = CreateSignature(secretKey, rawHash);
                
                Console.WriteLine($"  rawHash: {rawHash}");
                Console.WriteLine($"  signature: {signature}");

                var request = new MomoRequestModel
                {
                    PartnerCode = partnerCode,
                    AccessKey = accessKey,
                    RequestId = requestId,
                    Amount = amount,
                    OrderId = orderId,
                    OrderInfo = orderInfo,
                    RedirectUrl = returnUrl,
                    IpnUrl = notifyUrl,
                    ExtraData = extraData,
                    RequestType = requestType,
                    Signature = signature,
                    Lang = "vi"
                };

                // Validate các field required trước khi gửi
                if (string.IsNullOrEmpty(request.PartnerCode) || 
                    string.IsNullOrEmpty(request.AccessKey) ||
                    string.IsNullOrEmpty(request.RequestId) ||
                    string.IsNullOrEmpty(request.Amount) ||
                    string.IsNullOrEmpty(request.OrderId) ||
                    string.IsNullOrEmpty(request.OrderInfo) ||
                    string.IsNullOrEmpty(request.RedirectUrl) ||
                    string.IsNullOrEmpty(request.IpnUrl) ||
                    request.ExtraData == null ||
                    string.IsNullOrEmpty(request.RequestType) ||
                    string.IsNullOrEmpty(request.Signature))
                {
                    TempData["error"] = "Dữ liệu request MoMo không đầy đủ";
                    return RedirectToAction("Index", "Cart");
                }

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                
                // Log request để debug
                Console.WriteLine($"MoMo Request URL: {endpoint}");
                Console.WriteLine($"MoMo Request: {JsonConvert.SerializeObject(request, Formatting.Indented)}");
                
                var response = await httpClient.PostAsync(endpoint, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                // Log response để debug
                Console.WriteLine($"MoMo Response Status: {response.StatusCode}");
                Console.WriteLine($"MoMo Response: {responseBody}");
                
                if (response.IsSuccessStatusCode)
                {
                    var momoResponse = JsonConvert.DeserializeObject<MomoResponseModel>(responseBody);

                    if (momoResponse != null && momoResponse.ResultCode == 0 && !string.IsNullOrEmpty(momoResponse.PayUrl))
                    {
                        return Redirect(momoResponse.PayUrl);
                    }
                    else
                    {
                        var errorDetails = momoResponse != null 
                            ? $"ResultCode: {momoResponse.ResultCode}, Message: {momoResponse.Message}" 
                            : "Invalid response from MoMo";
                        
                        TempData["error"] = $"MoMo response error: {errorDetails}";
                        return RedirectToAction("Index", "Cart");
                    }
                }
                else
                {
                    TempData["error"] = $"HTTP Error: {response.StatusCode} - {responseBody}";
                    return RedirectToAction("Index", "Cart");
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = "Có lỗi xảy ra khi xử lý thanh toán: " + ex.Message;
                return RedirectToAction("Index", "Cart");
            }
        }

        [HttpPost]
        public IActionResult CreatePaymentUrlVnpay(string Name, decimal Amount, string OrderDescription, string OrderType)
        {
            // Placeholder cho VNPay - có thể implement sau
            TempData["info"] = "Chức năng thanh toán VNPay đang được phát triển";
            return RedirectToAction("Index", "Cart");
        }

        private string CreateSignature(string secretKey, string rawData)
        {
            var encoding = new System.Text.UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(secretKey);
            byte[] messageBytes = encoding.GetBytes(rawData);
            using (var hmacsha256 = new System.Security.Cryptography.HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return BitConverter.ToString(hashmessage).Replace("-", "").ToLower();
            }
        }
    }
}
