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
            // ✅ Force logging to file thay vì console
            System.IO.File.AppendAllText(@"C:\temp\momo_debug.log", 
                $"=== CreatePaymentMomo CALLED at {DateTime.Now} ===\n" +
                $"FullName: '{FullName}'\n" +
                $"Amount: {Amount}\n" +
                $"OrderInfo: '{OrderInfo}'\n");
            
            // Kiểm tra user đã đăng nhập
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (userEmail == null)
            {
                System.IO.File.AppendAllText(@"C:\temp\momo_debug.log", "ERROR: User not authenticated\n");
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra giỏ hàng
            var cartitems = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
            if (cartitems.Count == 0) 
            {
                System.IO.File.AppendAllText(@"C:\temp\momo_debug.log", "ERROR: Cart is empty\n");
                TempData["error"] = "Giỏ hàng trống!";
                return RedirectToAction("Index", "Cart");
            }

            try
            {
                // Test config ngay từ đầu
                var endpoint = _configuration["MomoAPI:MomoApiUrl"];
                var partnerCode = _configuration["MomoAPI:PartnerCode"];
                var accessKey = _configuration["MomoAPI:AccessKey"];
                var secretKey = _configuration["MomoAPI:SecretKey"];
                var returnUrl = _configuration["MomoAPI:ReturnUrl"];
                var notifyUrl = _configuration["MomoAPI:NotifyUrl"];
                var requestType = _configuration["MomoAPI:RequestType"];

                System.IO.File.AppendAllText(@"C:\temp\momo_debug.log", 
                    $"=== CONFIG TEST ===\n" +
                    $"endpoint: '{endpoint}'\n" +
                    $"partnerCode: '{partnerCode}'\n" +
                    $"accessKey: '{accessKey}'\n" +
                    $"secretKey: '{(string.IsNullOrEmpty(secretKey) ? "NULL" : secretKey.Substring(0, Math.Min(5, secretKey.Length)))}...'\n" +
                    $"returnUrl: '{returnUrl}'\n" +
                    $"notifyUrl: '{notifyUrl}'\n" +
                    $"requestType: '{requestType}'\n");

                // Nếu có config null, dừng ngay
                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(partnerCode) || 
                    string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey) ||
                    string.IsNullOrEmpty(returnUrl) || string.IsNullOrEmpty(notifyUrl) ||
                    string.IsNullOrEmpty(requestType))
                {
                    System.IO.File.AppendAllText(@"C:\temp\momo_debug.log", "ERROR: Missing MoMo config\n");
                    TempData["error"] = "Cấu hình MoMo chưa đầy đủ trong appsettings.json";
                    return RedirectToAction("Index", "Cart");
                }

                // Simple test: Tạo request với hardcoded values
                var orderId = Guid.NewGuid().ToString();
                var requestId = Guid.NewGuid().ToString();
                var amount = "50000"; // Test với số cố định
                var orderInfo = "Test payment";
                var extraData = "";

                System.IO.File.AppendAllText(@"C:\temp\momo_debug.log", 
                    $"=== REQUEST DATA ===\n" +
                    $"orderId: '{orderId}'\n" +
                    $"requestId: '{requestId}'\n" +
                    $"amount: '{amount}'\n" +
                    $"orderInfo: '{orderInfo}'\n" +
                    $"extraData: '{extraData}'\n");

                // Tạo signature
                string rawHash = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={notifyUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType={requestType}";
                var signature = CreateSignature(secretKey, rawHash);

                System.IO.File.AppendAllText(@"C:\temp\momo_debug.log", 
                    $"rawHash: '{rawHash}'\n" +
                    $"signature: '{signature}'\n");

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

                // Validate từng field một
                System.IO.File.AppendAllText(@"C:\temp\momo_debug.log", 
                    $"=== FINAL REQUEST VALIDATION ===\n" +
                    $"request.PartnerCode: '{request.PartnerCode}'\n" +
                    $"request.AccessKey: '{request.AccessKey}'\n" +
                    $"request.RequestId: '{request.RequestId}'\n" +
                    $"request.Amount: '{request.Amount}'\n" +
                    $"request.OrderId: '{request.OrderId}'\n" +
                    $"request.OrderInfo: '{request.OrderInfo}'\n" +
                    $"request.RedirectUrl: '{request.RedirectUrl}'\n" +
                    $"request.IpnUrl: '{request.IpnUrl}'\n" +
                    $"request.ExtraData: '{request.ExtraData}'\n" +
                    $"request.RequestType: '{request.RequestType}'\n" +
                    $"request.Signature: '{request.Signature}'\n" +
                    $"request.Lang: '{request.Lang}'\n");

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
                    System.IO.File.AppendAllText(@"C:\temp\momo_debug.log", "ERROR: Request validation failed\n");
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
