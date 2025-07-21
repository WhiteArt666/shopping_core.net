using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using shopping_tutorial.Models;
using shopping_tutorial.Models.ViewModels;
using shopping_tutorial.Repository;

namespace shopping_tutorial.Controllers
{
    public class CartController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly UserManager<AppUserModel> _userManager;
        
        public CartController(DataContext _context, UserManager<AppUserModel> userManager)
        {
            _dataContext = _context;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            List<CartItemModel> cartitems = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
            // Nhận shipping giá từ cookie
            var shippingPriceCookie = Request.Cookies["ShippingPrice"];
            decimal shippingPrice = 0;

            if (shippingPriceCookie != null)
            {
                var shippingPriceJson = shippingPriceCookie;
                shippingPrice = JsonConvert.DeserializeObject<decimal>(shippingPriceJson);
            }

            //Nhận Coupon code từ cookie
            var coupon_code = Request.Cookies["CouponTitle"];



            CartItemViewModel cartVM = new()
            {
                CartItems = cartitems,
                GrandTotal = cartitems.Sum(x => x.Quantity * x.Price),
                ShippingCost = shippingPrice,
                CouponCode = coupon_code,
            };
            return View(cartVM);
        }
        public IActionResult Checkout()
        {
            return View("~/Views/Checkout/Index.cshtml");
        }
        public async Task<IActionResult> Add(int Id)
        {
            ProductModel product = await _dataContext.Products.FindAsync(Id);
            List<CartItemModel> cart = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
            CartItemModel cartItems = cart.Where(c => c.ProductId == Id && c.ProductVariantId == null).FirstOrDefault();

            if (cartItems == null)
            {
                cart.Add(new CartItemModel(product));
            }
            else
            {
                cartItems.Quantity += 1;
            }
            HttpContext.Session.SetJson("Cart", cart);
            TempData["success"] = "Add Item to cart Successfully";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        // Thêm method mới để add variant vào giỏ hàng
        [HttpPost]
        public async Task<IActionResult> AddVariant(int productId, int variantId, int quantity = 1)
        {
            try
            {
                var product = await _dataContext.Products
                    .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Color)
                    .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Size)
                    .FirstOrDefaultAsync(p => p.Id == productId);

                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                var variant = product.ProductVariants.FirstOrDefault(v => v.Id == variantId);
                if (variant == null)
                {
                    return Json(new { success = false, message = "Variant không tồn tại" });
                }

                if (variant.Quantity < quantity)
                {
                    return Json(new { success = false, message = "Không đủ số lượng trong kho" });
                }

                List<CartItemModel> cart = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
                
                // Tìm cart item với cùng product và variant
                CartItemModel cartItem = cart.FirstOrDefault(c => c.ProductId == productId && c.ProductVariantId == variantId);

                if (cartItem == null)
                {
                    // Tạo cart item mới với variant
                    cart.Add(new CartItemModel(product, variant) { Quantity = quantity });
                }
                else
                {
                    // Cập nhật quantity
                    int newQuantity = cartItem.Quantity + quantity;
                    if (newQuantity > variant.Quantity)
                    {
                        return Json(new { success = false, message = "Vượt quá số lượng trong kho" });
                    }
                    cartItem.Quantity = newQuantity;
                }

                HttpContext.Session.SetJson("Cart", cart);
                return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
        public async Task<IActionResult> Decrease(int Id, int? variantId = null)
        {
            List<CartItemModel> cart = HttpContext.Session.GetJson<List<CartItemModel>>("Cart");
            CartItemModel cartItem = cart.Where(c => c.ProductId == Id && c.ProductVariantId == variantId).FirstOrDefault();
            
            if (cartItem != null)
            {
                if (cartItem.Quantity > 1)
                {
                    --cartItem.Quantity;
                }
                else
                {
                    cart.RemoveAll(p => p.ProductId == Id && p.ProductVariantId == variantId);
                }
            }
            
            if (cart.Count == 0)
            {
                HttpContext.Session.Remove("Cart");
            }
            else
            {
                HttpContext.Session.SetJson("Cart", cart);
            }
            TempData["success"] = "Decrease quantity  Item to cart Successfully";
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Increase(int Id, int? variantId = null)
        {
            List<CartItemModel> cart = HttpContext.Session.GetJson<List<CartItemModel>>("Cart");
            CartItemModel cartItem = cart.Where(c => c.ProductId == Id && c.ProductVariantId == variantId).FirstOrDefault();
            
            if (cartItem != null)
            {
                // Kiểm tra số lượng tồn kho
                int availableQuantity = 0;
                
                if (variantId.HasValue)
                {
                    var variant = await _dataContext.ProductVariants.FindAsync(variantId.Value);
                    availableQuantity = variant?.Quantity ?? 0;
                }
                else
                {
                    var product = await _dataContext.Products.FindAsync(Id);
                    availableQuantity = product?.Quantity ?? 0;
                }
                
                if (cartItem.Quantity < availableQuantity)
                {
                    ++cartItem.Quantity;
                    TempData["success"] = "Tăng số lượng thành công! ";
                }
                else
                {
                    TempData["success"] = "Đã đến giới hạn sản phẩm tồn kho! ";
                }
            }
            
            if (cart.Count == 0)
            {
                HttpContext.Session.Remove("Cart");
            }
            else
            {
                HttpContext.Session.SetJson("Cart", cart);
            }

            return RedirectToAction("Index");
        }
        public IActionResult Remove(int Id, int? variantId = null)
        {
            List<CartItemModel> cart = HttpContext.Session.GetJson<List<CartItemModel>>("Cart");
            cart.RemoveAll(p => p.ProductId == Id && p.ProductVariantId == variantId);
            if (cart.Count == 0)
            {
                HttpContext.Session.Remove("Cart");
            }
            else
            {
                HttpContext.Session.SetJson("Cart", cart);
            }
            TempData["success"] = "Remove  Item quantity of cart Successfully";
            return RedirectToAction("Index");
        }
        public IActionResult Clear()
        {

            HttpContext.Session.Remove("Cart");
            TempData["success"] = "Clear All Item quantity  of cart Successfully";
            return RedirectToAction("Index");


        }
        [HttpPost]
        [Route("Cart/GetShipping")]
        public async Task<IActionResult> GetShipping(ShippingModel shippingModel, string quan, string tinh, string phuong)
        {

            var existingShipping = await _dataContext.Shippings
                .FirstOrDefaultAsync(x => x.City == tinh && x.District == quan && x.Ward == phuong);

            decimal shippingPrice = 0; // Set mặc định giá tiền

            if (existingShipping != null)
            {
                shippingPrice = existingShipping.Price;
            }
            else
            {
                //Set mặc định giá tiền nếu ko tìm thấy
                shippingPrice = 50000;
            }
            var shippingPriceJson = JsonConvert.SerializeObject(shippingPrice);
            try
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                    Secure = true // using HTTPS
                };

                Response.Cookies.Append("ShippingPrice", shippingPriceJson, cookieOptions);
            }
            catch (Exception ex)
            {
                //
                Console.WriteLine($"Error adding shipping price cookie: {ex.Message}");
            }
            return Json(new { shippingPrice });
        }

        [HttpGet]
        [Route("Cart/RemoveShippingCookie")]
        public IActionResult RemoveShippingCookie()
        {
            Response.Cookies.Delete("ShippingPrice");
            return RedirectToAction("Index", "Cart");
        }

        [HttpPost]
        [Route("Cart/GetCoupon")]
        public async Task<IActionResult> GetCoupon(CouponModel couponModel, string coupon_value)
        {
            var validCoupon = await _dataContext.Coupons
                .FirstOrDefaultAsync(x => x.Name == coupon_value && x.Quantity >= 1);

            // ✅ Kiểm tra null trước khi dùng
            if (validCoupon == null)
            {
                return Ok(new { success = false, message = "Coupon không tồn tại hoặc đã hết số lượng" });
            }

            // KIỂM TRA VOUCHER ĐỘC QUYỀN
            var exclusiveVoucher = await _dataContext.CustomerVouchers
                .Where(cv => cv.CouponId == validCoupon.Id && cv.VoucherType == "Exclusive" && !cv.IsUsed)
                .FirstOrDefaultAsync();

            if (exclusiveVoucher != null)
            {
                // Nếu có voucher độc quyền, kiểm tra xem user hiện tại có phải là người được gửi không
                if (User.Identity.IsAuthenticated)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser != null && currentUser.Id != exclusiveVoucher.UserId)
                    {
                        // User hiện tại không phải là người được gửi voucher độc quyền
                        var exclusiveUser = await _userManager.FindByIdAsync(exclusiveVoucher.UserId);
                        return Ok(new { 
                            success = false, 
                            message = $"Voucher này là độc quyền và chỉ dành cho khách hàng {exclusiveUser?.UserName ?? "đặc biệt"}. Bạn không thể sử dụng voucher này." 
                        });
                    }
                    else if (currentUser != null && currentUser.Id == exclusiveVoucher.UserId)
                    {
                        // User hiện tại chính là người được gửi voucher độc quyền - cho phép sử dụng
                        // Tiếp tục xử lý bình thường
                    }
                }
                else
                {
                    // User chưa đăng nhập nhưng voucher là độc quyền
                    return Ok(new { 
                        success = false, 
                        message = "Voucher này là độc quyền. Vui lòng đăng nhập để kiểm tra quyền sử dụng." 
                    });
                }
            }

            string couponTitle = validCoupon.Name + " | " + validCoupon.Description;

            TimeSpan remainingTime = validCoupon.DateExpired - DateTime.Now;
            int daysRemaining = remainingTime.Days;

            if (daysRemaining >= 0)
            {
                try
                {
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                        Secure = true,
                        SameSite = SameSiteMode.Strict
                    };

                    Response.Cookies.Append("CouponTitle", couponTitle, cookieOptions);
                    
                    // Thông báo đặc biệt cho voucher độc quyền
                    if (exclusiveVoucher != null)
                    {
                        return Ok(new { success = true, message = "Áp dụng voucher độc quyền thành công! Voucher này chỉ dành riêng cho bạn." });
                    }
                    
                    return Ok(new { success = true, message = "Áp dụng mã giảm giá thành công" });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi lưu coupon vào cookie: {ex.Message}");
                    return Ok(new { success = false, message = "Không thể áp dụng mã giảm giá" });
                }
            }
            else
            {
                return Ok(new { success = false, message = "Mã giảm giá đã hết hạn" });
            }
        }


    }
}

