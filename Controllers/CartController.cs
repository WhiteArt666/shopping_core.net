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

        /* public async Task<IActionResult> Index()
         {
             var cartitems = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
             decimal grandTotal = cartitems.Sum(x => x.Quantity * x.Price);
             decimal discount = 0;
             decimal shippingPrice = 0;
             string? couponTitle = null;

             if (Request.Cookies.TryGetValue("ShippingPrice", out string? shipCookie))
             {
                 shippingPrice = JsonConvert.DeserializeObject<decimal>(shipCookie);
             }

             // Gộp xử lý từ cookie đơn giản
             if (Request.Cookies.TryGetValue("CouponApplied", out var discountStr)
                 && Request.Cookies.TryGetValue("CouponTitle", out var couponCode))
             {
                 discount = decimal.TryParse(discountStr, out var parsed) ? parsed : 0;
                 couponTitle = couponCode;
             }

             decimal finalTotal = grandTotal + shippingPrice - discount;

             var cartVM = new CartItemViewModel
             {
                 CartItems = cartitems,
                 GrandTotal = grandTotal,
                 ShippingCost = shippingPrice,
                 CouponCode = couponTitle,
                 Discount = discount,
                 FinalTotal = finalTotal
             };

             return View(cartVM);
         }*/
        // ⛳️ Đã sửa phần kiểm tra mã giảm giá để không cho dùng lại sau khi đã thanh toán
        public async Task<IActionResult> Index()
        {
            var cartitems = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
            decimal grandTotal = cartitems.Sum(x => x.Quantity * x.Price);
            decimal discount = 0;
            decimal shippingPrice = 0;
            string? couponTitle = null;

            if (Request.Cookies.TryGetValue("ShippingPrice", out string? shipCookie))
            {
                shippingPrice = JsonConvert.DeserializeObject<decimal>(shipCookie);
            }

            // ✅ Kiểm tra lại tình trạng hợp lệ của mã giảm giá
            if (Request.Cookies.TryGetValue("CouponApplied", out var discountStr)
                && Request.Cookies.TryGetValue("CouponTitle", out var couponCode))
            {
                var coupon = await _dataContext.Coupons.FirstOrDefaultAsync(c => c.Name == couponCode);

                bool isUsed = false;

                // 🔐 Kiểm tra nếu là mã độc quyền hoặc đổi điểm đã được sử dụng
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null && coupon != null)
                {
                    var exclusive = await _dataContext.CustomerVouchers
                        .FirstOrDefaultAsync(cv => cv.CouponId == coupon.Id && cv.UserId == currentUser.Id);
                    if (exclusive != null && exclusive.IsUsed)
                        isUsed = true;

                    var userCoupon = await _dataContext.UserCoupons
                        .FirstOrDefaultAsync(uc => uc.CouponId == coupon.Id && uc.UserId == currentUser.Id);
                    if (userCoupon != null && userCoupon.IsUsed)
                        isUsed = true;
                }

                // ❌ Nếu mã đã hết hạn, hết số lượng, hoặc đã dùng → xoá cookie
                if (coupon == null || coupon.Quantity <= 0 || isUsed)
                {
                    Response.Cookies.Delete("CouponTitle");
                    Response.Cookies.Delete("CouponApplied");
                    TempData["error"] = "Mã giảm giá không còn hiệu lực.";
                }
                else
                {
                    discount = decimal.TryParse(discountStr, out var parsed) ? parsed : 0;
                    couponTitle = couponCode;
                }
            }

            decimal finalTotal = grandTotal + shippingPrice - discount;

            var cartVM = new CartItemViewModel
            {
                CartItems = cartitems,
                GrandTotal = grandTotal,
                ShippingCost = shippingPrice,
                CouponCode = couponTitle,
                Discount = discount,
                FinalTotal = finalTotal
            };

            return View(cartVM);
        }


        public IActionResult Checkout() => View("~/Views/Checkout/Index.cshtml");

        public async Task<IActionResult> Add(int Id)
        {
            ProductModel product = await _dataContext.Products.FindAsync(Id);
            List<CartItemModel> cart = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
            CartItemModel cartItems = cart.FirstOrDefault(c => c.ProductId == Id && c.ProductVariantId == null);

            if (cartItems == null)
                cart.Add(new CartItemModel(product));
            else
                cartItems.Quantity += 1;

            HttpContext.Session.SetJson("Cart", cart);
            TempData["success"] = "Thêm sản phẩm vào giỏ hàng thành công";
            return Redirect(Request.Headers["Referer"].ToString());
        }

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

                if (product == null) return Json(new { success = false, message = "Sản phẩm không tồn tại" });

                var variant = product.ProductVariants.FirstOrDefault(v => v.Id == variantId);
                if (variant == null) return Json(new { success = false, message = "Biến thể không tồn tại" });
                if (variant.Quantity < quantity) return Json(new { success = false, message = "Không đủ hàng" });

                List<CartItemModel> cart = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
                var cartItem = cart.FirstOrDefault(c => c.ProductId == productId && c.ProductVariantId == variantId);

                if (cartItem == null)
                    cart.Add(new CartItemModel(product, variant) { Quantity = quantity });
                else
                {
                    int newQuantity = cartItem.Quantity + quantity;
                    if (newQuantity > variant.Quantity)
                        return Json(new { success = false, message = "Vượt quá tồn kho" });
                    cartItem.Quantity = newQuantity;
                }

                HttpContext.Session.SetJson("Cart", cart);
                return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        public async Task<IActionResult> Decrease(int Id, int? variantId = null)
        {
            var cart = HttpContext.Session.GetJson<List<CartItemModel>>("Cart");
            var item = cart.FirstOrDefault(c => c.ProductId == Id && c.ProductVariantId == variantId);

            if (item != null)
            {
                if (item.Quantity > 1) --item.Quantity;
                else cart.Remove(item);
            }

            if (cart.Count == 0) HttpContext.Session.Remove("Cart");
            else HttpContext.Session.SetJson("Cart", cart);

            TempData["success"] = "Giảm số lượng thành công!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Increase(int Id, int? variantId = null)
        {
            var cart = HttpContext.Session.GetJson<List<CartItemModel>>("Cart");
            var item = cart.FirstOrDefault(c => c.ProductId == Id && c.ProductVariantId == variantId);
            int stock = 0;

            if (variantId.HasValue)
                stock = (await _dataContext.ProductVariants.FindAsync(variantId.Value))?.Quantity ?? 0;
            else
                stock = (await _dataContext.Products.FindAsync(Id))?.Quantity ?? 0;

            if (item != null && item.Quantity < stock)
                ++item.Quantity;

            TempData["success"] = "Tăng số lượng thành công";
            HttpContext.Session.SetJson("Cart", cart);
            return RedirectToAction("Index");
        }

        public IActionResult Remove(int Id, int? variantId = null)
        {
            var cart = HttpContext.Session.GetJson<List<CartItemModel>>("Cart");
            cart.RemoveAll(p => p.ProductId == Id && p.ProductVariantId == variantId);

            if (cart.Count == 0) HttpContext.Session.Remove("Cart");
            else HttpContext.Session.SetJson("Cart", cart);

            TempData["success"] = "Đã xoá sản phẩm khỏi giỏ hàng";
            return RedirectToAction("Index");
        }

        public IActionResult Clear()
        {
            HttpContext.Session.Remove("Cart");
            Response.Cookies.Delete("ShippingPrice");
            Response.Cookies.Delete("CouponTitle");
            Response.Cookies.Delete("CouponApplied");

            TempData["success"] = "Đã xoá toàn bộ giỏ hàng";
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Route("Cart/ClearAfterCheckout")]
        public IActionResult ClearAfterCheckout()
        {
            HttpContext.Session.Remove("Cart");
            Response.Cookies.Delete("ShippingPrice");
            Response.Cookies.Delete("CouponTitle");
            Response.Cookies.Delete("CouponApplied");

            TempData["success"] = "Đơn hàng đã được đặt thành công!";
            return RedirectToAction("Index", "Cart");
        }

        [HttpPost]
        [Route("Cart/GetShipping")]
        public async Task<IActionResult> GetShipping(ShippingModel model, string quan, string tinh, string phuong)
        {
            var ship = await _dataContext.Shippings.FirstOrDefaultAsync(x => x.City == tinh && x.District == quan && x.Ward == phuong);
            decimal price = ship?.Price ?? 50000;

            var cookie = JsonConvert.SerializeObject(price);
            Response.Cookies.Append("ShippingPrice", cookie, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = DateTimeOffset.UtcNow.AddMinutes(30)
            });

            return Json(new { shippingPrice = price });
        }

        [HttpGet]
        [Route("Cart/RemoveShippingCookie")]
        public IActionResult RemoveShippingCookie()
        {
            Response.Cookies.Delete("ShippingPrice");
            return RedirectToAction("Index");
        }

        /*  [HttpPost]
          [Route("Cart/GetCoupon")]
          [IgnoreAntiforgeryToken]
          public async Task<IActionResult> GetCoupon(string coupon_value)
          {
              // ✅ Ưu tiên mã test
              if (coupon_value.ToLower() == "giam50k")
              {
                  Response.Cookies.Append("CouponTitle", coupon_value);
                  Response.Cookies.Append("CouponApplied", "50000");
                  return Json(new { success = true, message = "Áp dụng thành công mã giảm giá!" });
              }

              var coupon = await _dataContext.Coupons
                  .FirstOrDefaultAsync(x => x.Name == coupon_value && x.Quantity >= 1 && x.Status == 1 && x.DateExpired >= DateTime.Now);

              if (coupon == null)
                  return Json(new { success = false, message = "Mã không hợp lệ hoặc hết hạn!" });

              // Kiểm tra voucher độc quyền
              var exclusiveVoucher = await _dataContext.CustomerVouchers
                  .FirstOrDefaultAsync(cv => cv.CouponId == coupon.Id && cv.VoucherType == "Exclusive" && !cv.IsUsed);

              if (exclusiveVoucher != null)
              {
                  if (!User.Identity.IsAuthenticated)
                      return Json(new { success = false, message = "Vui lòng đăng nhập để sử dụng voucher độc quyền." });

                  var currentUser = await _userManager.GetUserAsync(User);
                  if (currentUser?.Id != exclusiveVoucher.UserId)
                      return Json(new { success = false, message = "Voucher này chỉ dành cho người nhận được." });
              }

              // Ghi cookie
              Response.Cookies.Append("CouponTitle", coupon.Name);
              Response.Cookies.Append("CouponApplied", coupon.DiscountAmount.ToString());

              return Json(new { success = true, message = "Áp dụng mã giảm giá thành công!" });
          }*/
        [HttpPost]
[Route("Cart/GetCoupon")]
[IgnoreAntiforgeryToken]
public async Task<IActionResult> GetCoupon(string coupon_value)
{
    var code = coupon_value.ToLower();

    var coupon = await _dataContext.Coupons
        .FirstOrDefaultAsync(x => x.Name.ToLower() == code && x.Quantity >= 1 && x.Status == 1 && x.DateExpired >= DateTime.Now);

    if (coupon == null)
        return Json(new { success = false, message = "Mã không hợp lệ hoặc đã hết hạn!" });

    var exclusiveVoucher = await _dataContext.CustomerVouchers
        .FirstOrDefaultAsync(cv => cv.CouponId == coupon.Id && cv.VoucherType == "Exclusive" && !cv.IsUsed);

    if (exclusiveVoucher != null)
    {
        if (!User.Identity.IsAuthenticated)
            return Json(new { success = false, message = "Vui lòng đăng nhập để sử dụng voucher độc quyền." });

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser?.Id != exclusiveVoucher.UserId)
            return Json(new { success = false, message = "Voucher này chỉ dành cho người nhận được." });
    }

    Response.Cookies.Append("CouponTitle", coupon.Name);
    Response.Cookies.Append("CouponApplied", coupon.DiscountAmount.ToString());

    return Json(new { success = true, message = $"🎉 Áp dụng mã '{coupon.Name}' thành công!" });
}

    }
}
