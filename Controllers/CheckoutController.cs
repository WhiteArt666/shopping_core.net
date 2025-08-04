using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;
using System.Security.Claims;

namespace shopping_tutorial.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly UserManager<AppUserModel> _userManager;
        private const decimal AmountPerPoint = 10_000m;

        public CheckoutController(DataContext context, UserManager<AppUserModel> userManager)
        {
            _dataContext = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Checkout()
        {
            // ✅ Lấy thông tin đăng nhập
            var userId = _userManager.GetUserId(User);
            var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return RedirectToAction("Login", "Account");

            // 1. Tạo đơn hàng (OrderModel)
            string orderCode = Guid.NewGuid().ToString();
            var order = new OrderModel
            {
                OrderCode = orderCode,
                UserName = user.Email, // dùng Email
                Status = 1,
                CreatedDate = DateTime.Now,
                ShippingCost = 0,
                CouponCode = Request.Cookies["CouponTitle"]
            };

            if (Request.Cookies.TryGetValue("ShippingPrice", out var shipJson))
                order.ShippingCost = JsonConvert.DeserializeObject<decimal>(shipJson);

            _dataContext.Add(order);
            await _dataContext.SaveChangesAsync();

            // 2. Xử lý mã giảm giá (nếu có)
            if (!string.IsNullOrEmpty(order.CouponCode))
            {
                var coupon = await _dataContext.Coupons
                    .FirstOrDefaultAsync(c => c.Name == order.CouponCode);

                if (coupon != null)
                {
                    coupon.Quantity -= 1;
                    _dataContext.Update(coupon);

                    var exclusiveVoucher = await _dataContext.CustomerVouchers
                        .FirstOrDefaultAsync(v => v.CouponId == coupon.Id && !v.IsUsed);
                    if (exclusiveVoucher != null)
                    {
                        exclusiveVoucher.IsUsed = true;
                        _dataContext.Update(exclusiveVoucher);
                    }

                    var userCoupon = await _dataContext.UserCoupons
                        .FirstOrDefaultAsync(uc => uc.CouponId == coupon.Id
                                                && uc.UserId == user.Id
                                                && !uc.IsUsed);
                    if (userCoupon != null)
                    {
                        userCoupon.IsUsed = true;
                        _dataContext.Update(userCoupon);
                    }

                    await _dataContext.SaveChangesAsync();
                }
            }

            // 3. Tạo OrderDetails + cập nhật tồn kho
            var cartItems = HttpContext.Session.GetJson<List<CartItemModel>>("Cart")
                            ?? new List<CartItemModel>();

            foreach (var cart in cartItems)
            {
                var detail = new OrderDetails
                {
                    UserName = user.Email,
                    OrderCode = orderCode,
                    ProductId = cart.ProductId,
                    ProductVariantId = cart.ProductVariantId,
                    Price = cart.Price,
                    Quantity = cart.Quantity,
                    Size = cart.Size,
                    Color = cart.Color,
                    ColorCode = cart.ColorCode
                };

                if (cart.ProductVariantId.HasValue)
                {
                    var variant = await _dataContext.ProductVariants
                        .FirstAsync(v => v.Id == cart.ProductVariantId.Value);
                    variant.Quantity -= cart.Quantity;
                    variant.Sold += cart.Quantity;
                    _dataContext.Update(variant);
                }
                else
                {
                    var product = await _dataContext.Products
                        .FirstAsync(p => p.Id == cart.ProductId);
                    product.Quantity -= cart.Quantity;
                    product.Sold += cart.Quantity;
                    _dataContext.Update(product);
                }

                _dataContext.Add(detail);
            }

            await _dataContext.SaveChangesAsync();

            // 4. Cộng điểm tích lũy
            decimal total = await _dataContext.OrderDetails
                .Where(d => d.OrderCode == orderCode)
                .SumAsync(d => d.Price * d.Quantity);

            int earnedPoints = (int)(total / AmountPerPoint);
            if (earnedPoints > 0)
            {
                user.LoyaltyPoints += earnedPoints;

                _dataContext.LoyaltyHistories.Add(new LoyaltyHistoryModel
                {
                    UserId = user.Id,
                    Points = earnedPoints,
                    Reason = $"Tích điểm từ đơn #{orderCode}",
                    CreatedAt = DateTime.Now
                });

                _dataContext.Update(user);
                await _dataContext.SaveChangesAsync();
            }

            // 5. Xoá session + cookie
            HttpContext.Session.Remove("Cart");
            Response.Cookies.Delete("ShippingPrice");
            Response.Cookies.Delete("CouponTitle");
            Response.Cookies.Delete("CouponApplied");

            TempData["success"] = "Đơn hàng đã được tạo, vui lòng chờ duyệt đơn hàng nhé.";
            return RedirectToAction("ClearAfterCheckout", "Cart");
        }
    }
}
