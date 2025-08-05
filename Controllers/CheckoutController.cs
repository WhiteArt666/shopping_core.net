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

            // ✅ Tích điểm cho khách hàng khi đặt hàng thành công  
            await AddLoyaltyPoints(userEmail, orderItem.Id, cartitems);

            // Đánh dấu mã giảm giá đã sử dụng
            if (!string.IsNullOrEmpty(CouponCode))
            {
                await MarkCouponAsUsed(CouponCode, userEmail);
            }

			//send mail order when success 
			//var receive = userEmail;// email nhận sẽ là email của người đặt hàng 
			//var subject = "Đăng nhập trên thiết bị thành công";
			//var message = "Đặt hàng thành công, trải nhiệm dịch vụ nhé";

			//await _emailSender.SendEmailAsync(receive, subject, message);

			//Message checkout successfully 
			TempData["success"] = "Đơn hàng đã được tạo, vui lòng chờ duyệt đơn hàng nhé";
			return RedirectToAction("ClearAfterCheckout", "Cart");
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
            var orderInfo = $"Thanh toán đơn hàng tại LocalBrand - {amount} VNĐ";
            var extraData = "";

            // Tạo rawHash theo định dạng mới của MoMo API v2
            string rawHash = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={notifyUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType={requestType}";
            var signature = CreateSignature(secretKey, rawHash);

            var request = new 
            {
                partnerCode = partnerCode,
                partnerName = "LocalBrand Store", 
                storeId = "LocalBrandStore",
                requestId = requestId,
                amount = amount,
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = returnUrl,
                ipnUrl = notifyUrl,
                lang = "vi",
                extraData = extraData,
                requestType = requestType,
                signature = signature
            };

            using var httpClient = new HttpClient();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            
            try 
            {
                var response = await httpClient.PostAsync(endpoint, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                // Log response để debug
                Console.WriteLine($"MoMo Response: {responseBody}");
                
                var momoResponse = JsonConvert.DeserializeObject<MomoResponseModel>(responseBody);

                if (momoResponse != null && momoResponse.ResultCode == 0 && !string.IsNullOrEmpty(momoResponse.PayUrl))
                {
                    return Redirect(momoResponse.PayUrl);
                }
                else
                {
                    var errorMsg = momoResponse?.Message ?? "Không có phản hồi từ MoMo";
                    TempData["error"] = $"Lỗi thanh toán MoMo: {errorMsg}";
                    return RedirectToAction("Index", "Cart");
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Lỗi kết nối MoMo: {ex.Message}";
                return RedirectToAction("Index", "Cart");
            }
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

        [HttpPost]
        public IActionResult MomoNotify()
        {
            // Xử lý thông báo từ MoMo (IPN - Instant Payment Notification)
            // Đây là endpoint để MoMo gửi thông báo về kết quả thanh toán
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> CheckoutSuccess()
        {
            // Khi thanh toán thành công, tạo đơn hàng
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (userEmail == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartitems = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
            if (cartitems.Count == 0)
            {
                TempData["error"] = "Giỏ hàng trống!";
                return RedirectToAction("Index", "Cart");
            }

            try
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

                _dataContext.Add(orderItem);
                _dataContext.SaveChanges();

                // Tạo order details
                foreach (var cart in cartitems)
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
                        var variant = await _dataContext.ProductVariants.Where(v => v.Id == cart.ProductVariantId.Value).FirstAsync();
                        variant.Quantity -= cart.Quantity;
                        variant.Sold += cart.Quantity;
                        _dataContext.Update(variant);
                    }
                    else
                    {
                        var product = await _dataContext.Products.Where(p => p.Id == cart.ProductId).FirstAsync();
                        product.Quantity -= cart.Quantity;
                        product.Sold += cart.Quantity;
                        _dataContext.Update(product);
                    }

                    _dataContext.Add(orderdetails);
                }

                await _dataContext.SaveChangesAsync();

                // Xóa giỏ hàng và cookies
                HttpContext.Session.Remove("Cart");
                Response.Cookies.Delete("ShippingPrice");
                Response.Cookies.Delete("CouponTitle");
                Response.Cookies.Delete("CouponApplied");

                // ✅ Tích điểm cho khách hàng khi thanh toán MoMo thành công  
                await AddLoyaltyPoints(userEmail, orderItem.Id, cartitems);

                // Đánh dấu mã giảm giá đã sử dụng
                if (!string.IsNullOrEmpty(CouponCode))
                {
                    await MarkCouponAsUsed(CouponCode, userEmail);
                }

                ViewBag.OrderCode = ordercode;
                return View();
            }
            catch (Exception ex)
            {
                TempData["error"] = "Có lỗi xảy ra khi tạo đơn hàng: " + ex.Message;
                return RedirectToAction("Index", "Cart");
            }
        }

        private async Task MarkCouponAsUsed(string couponCode, string userEmail)
        {
            try
            {
                var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user == null) return;

                var coupon = await _dataContext.Coupons.FirstOrDefaultAsync(c => c.Name == couponCode);
                if (coupon == null) return;

                // Đánh dấu voucher độc quyền đã sử dụng
                var exclusiveVoucher = await _dataContext.CustomerVouchers
                    .FirstOrDefaultAsync(cv => cv.CouponId == coupon.Id && cv.UserId == user.Id && cv.VoucherType == "Exclusive" && !cv.IsUsed);
                if (exclusiveVoucher != null)
                {
                    exclusiveVoucher.IsUsed = true;
                    exclusiveVoucher.UsedDate = DateTime.Now;
                    _dataContext.Update(exclusiveVoucher);
                }

                // Đánh dấu voucher đổi điểm đã sử dụng
                var userCoupon = await _dataContext.UserCoupons
                    .FirstOrDefaultAsync(uc => uc.CouponId == coupon.Id && uc.UserId == user.Id && !uc.IsUsed);
                if (userCoupon != null)
                {
                    userCoupon.IsUsed = true;
                    _dataContext.Update(userCoupon);
                }

                // Giảm số lượng coupon
                if (coupon.Quantity > 0)
                {
                    coupon.Quantity--;
                    _dataContext.Update(coupon);
                }

                await _dataContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking coupon as used: {ex.Message}");
            }
        }

        // ✅ Method tích điểm cho khách hàng
        private async Task AddLoyaltyPoints(string userEmail, int orderId, List<CartItemModel> cartItems)
        {
            try
            {
                var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                if (user == null) return;

                // Tính tổng tiền đơn hàng
                decimal totalAmount = cartItems.Sum(item => item.Price * item.Quantity);
                
                // Tích điểm: 1 điểm cho mỗi 10,000 VNĐ
                int pointsEarned = (int)(totalAmount / 10000);
                
                if (pointsEarned > 0)
                {
                    // Cộng điểm vào tài khoản
                    user.LoyaltyPoints += pointsEarned;
                    _dataContext.Users.Update(user);

                    // Lưu lịch sử tích điểm
                    var loyaltyHistory = new LoyaltyHistoryModel
                    {
                        UserId = user.Id,
                        Points = pointsEarned,
                        Reason = $"Tích điểm từ đơn hàng #{orderId} - {totalAmount:N0} VNĐ",
                        CreatedAt = DateTime.Now
                    };
                    
                    _dataContext.LoyaltyHistories.Add(loyaltyHistory);
                    await _dataContext.SaveChangesAsync();
                    
                    Console.WriteLine($"Added {pointsEarned} loyalty points for user {userEmail}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding loyalty points: {ex.Message}");
            }
        }


    }

}
