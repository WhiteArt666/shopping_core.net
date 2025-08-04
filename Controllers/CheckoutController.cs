using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using shopping_tutorial.Areas.Admin.Repository;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;
using System.Security.Claims;
using System.Text;

namespace shopping_tutorial.Controllers
{
	public class CheckoutController : Controller
	{
		private readonly DataContext _dataContext;
        private readonly IConfiguration _configuration;

        public CheckoutController(DataContext context, IConfiguration configuration)
        {
            _dataContext = context;
            _configuration = configuration;
        }


        public async Task<IActionResult> Checkout()
		{
			var userEmail = User.FindFirstValue(ClaimTypes.Email);
			if(userEmail == null)
			{
				return RedirectToAction("Login", "Account");
			}
			else
			{
				var ordercode = Guid.NewGuid().ToString();
				var orderItem = new OrderModel();
				orderItem.OrderCode = ordercode;
				orderItem.UserName = userEmail;
				orderItem.Status = 1; // 1 là đơn hàng mới 
				orderItem.CreatedDate = DateTime.Now;
				// Retrieve shipping price from cookie
				var shippingPriceCookie = Request.Cookies["ShippingPrice"];
				decimal shippingPrice = 0;

				if (shippingPriceCookie != null)
				{
					var shippingPriceJson = shippingPriceCookie;
					shippingPrice = JsonConvert.DeserializeObject<decimal>(shippingPriceJson);
				}
				orderItem.ShippingCost = shippingPrice;
				//Nhận coupon code
				var CouponCode = Request.Cookies["CouponTitle"];
				orderItem.CouponCode = CouponCode;
				
				_dataContext.Add(orderItem);// thêm dữ liệu tạo đơn hàng mới 
				_dataContext.SaveChanges();
				List<CartItemModel> cartitems = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
				foreach(var cart in cartitems)
				{
					var orderdetails = new OrderDetails();
					orderdetails.UserName = userEmail;
					orderdetails.OrderCode = ordercode;
					orderdetails.ProductId = cart.ProductId;
					orderdetails.ProductVariantId = cart.ProductVariantId;
					orderdetails.Price = cart.Price;
					orderdetails.Quantity = cart.Quantity;
					
					// Lưu thông tin variant
					orderdetails.Size = cart.Size;
					orderdetails.Color = cart.Color;
					orderdetails.ColorCode = cart.ColorCode;
					
					// Update product/variant quantity
					if (cart.ProductVariantId.HasValue)
					{
						// Có variant - cập nhật số lượng variant
						var variant = await _dataContext.ProductVariants.Where(v => v.Id == cart.ProductVariantId.Value).FirstAsync();
						variant.Quantity -= cart.Quantity;
						variant.Sold += cart.Quantity;
						_dataContext.Update(variant);
					}
					else
					{
						// Không có variant - cập nhật số lượng product
						var product = await _dataContext.Products.Where(p => p.Id == cart.ProductId).FirstAsync();
						product.Quantity -= cart.Quantity;
						product.Sold += cart.Quantity;
						_dataContext.Update(product);
					}
					
					_dataContext.Add(orderdetails);// thêm dữ liệu tạo đơn hàng mới 
					_dataContext.SaveChanges();
				}
				HttpContext.Session.Remove("Cart");
				//send mail order when success 
				//var receive = userEmail;// email nhận sẽ là email của người đặt hàng 
				//var subject = "Đăng nhập trên thiết bị thành công";
				//var message = "Đặt hàng thành công, trải nhiệm dịch vụ nhé";

				//await _emailSender.SendEmailAsync(receive, subject, message);

				//Message checkout successfully 
				TempData["success"] = "Đơn hàng đã được tạo, vui lòng chờ duyệt đơn hàng nhé";
				return RedirectToAction("Index", "Cart");
			}
			return View();
		}
        [HttpPost]
        public async Task<IActionResult> CheckoutWithMomo()
        {
            var cartitems = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
            if (cartitems.Count == 0) return RedirectToAction("Index", "Cart");

            var orderId = Guid.NewGuid().ToString();
            var amount = cartitems.Sum(x => x.Quantity * x.Price).ToString();

            // Load Momo Config
            var endpoint = _configuration["MomoAPI:MomoApiUrl"];
            var partnerCode = _configuration["MomoAPI:PartnerCode"];
            var accessKey = _configuration["MomoAPI:AccessKey"];
            var secretKey = _configuration["MomoAPI:SecretKey"];
            var returnUrl = _configuration["MomoAPI:ReturnUrl"];
            var notifyUrl = _configuration["MomoAPI:NotifyUrl"];
            var requestType = _configuration["MomoAPI:RequestType"];

            var requestId = Guid.NewGuid().ToString();
            var orderInfo = "Thanh toán đơn hàng tại LocalBrand";
            var extraData = "";

            string rawHash = $"partnerCode={partnerCode}&accessKey={accessKey}&requestId={requestId}&amount={amount}&orderId={orderId}&orderInfo={orderInfo}&returnUrl={returnUrl}&notifyUrl={notifyUrl}&extraData={extraData}";
            var signature = CreateSignature(secretKey, rawHash);

            var request = new MomoRequestModel
            {
                PartnerCode = partnerCode,
                AccessKey = accessKey,
                RequestId = requestId,
                Amount = amount,
                OrderId = orderId,
                OrderInfo = orderInfo,
                ReturnUrl = returnUrl,
                NotifyUrl = notifyUrl,
                ExtraData = extraData,
                RequestType = requestType,
                Signature = signature
            };

            using var httpClient = new HttpClient();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(endpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            var momoResponse = JsonConvert.DeserializeObject<MomoResponseModel>(responseBody);

            return Redirect(momoResponse.PayUrl); // Chuyển hướng sang cổng Momo
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
        [HttpGet]
        public IActionResult PaymentCallBack(string message, int resultCode, string orderId)
        {
            if (resultCode == 0)
            {
                TempData["success"] = "Thanh toán MoMo thành công!";
                return RedirectToAction("CheckoutSuccess", "Checkout");
            }
            else
            {
                TempData["error"] = $"Thanh toán thất bại: {message}";
                return RedirectToAction("Index", "Cart");
            }
        }


    }

}
