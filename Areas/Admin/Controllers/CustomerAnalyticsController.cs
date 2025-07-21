using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;

namespace shopping_tutorial.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CustomerAnalyticsController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly UserManager<AppUserModel> _userManager;

        public CustomerAnalyticsController(DataContext dataContext, UserManager<AppUserModel> userManager)
        {
            _dataContext = dataContext;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var model = new CustomerAnalyticsViewModel();

            try
            {
                // Debug: Kiểm tra dữ liệu cơ bản
                var orderDetailsCount = await _dataContext.OrderDetails.CountAsync();
                var ordersCount = await _dataContext.Orders.CountAsync();
                var usersCount = await _userManager.Users.CountAsync();
                
                ViewBag.Debug = $"OrderDetails: {orderDetailsCount}, Orders: {ordersCount}, Users: {usersCount}";

                // Lấy dữ liệu từ OrderDetails để tính tổng tiền chi tiêu chính xác
                var orderDetailsSummary = await _dataContext.OrderDetails
                    .Join(_dataContext.Orders, 
                          od => od.OrderCode, 
                          o => o.OrderCode, 
                          (od, o) => new { 
                              od.UserName, 
                              od.Price, 
                              od.Quantity, 
                              o.CreatedDate,
                              o.OrderCode 
                          })
                    .Where(x => x.UserName != null && !string.IsNullOrEmpty(x.UserName))
                    .ToListAsync();

                // Nếu không có dữ liệu OrderDetails, thử sử dụng dữ liệu từ Orders
                if (!orderDetailsSummary.Any())
                {
                    var ordersData = await _dataContext.Orders
                        .Where(o => o.UserName != null && !string.IsNullOrEmpty(o.UserName))
                        .Select(o => new { 
                            o.UserName, 
                            Price = o.ShippingCost, // Tạm dùng ShippingCost
                            Quantity = 1, 
                            o.CreatedDate,
                            o.OrderCode 
                        })
                        .ToListAsync();
                    
                    orderDetailsSummary = ordersData;
                }

                var orderSummaries = orderDetailsSummary
                    .GroupBy(x => x.UserName)
                    .Select(g => new { 
                        UserName = g.Key, 
                        TotalSpent = g.Sum(x => x.Price * x.Quantity), // Tính tổng tiền thực tế
                        OrderCount = g.Select(x => x.OrderCode).Distinct().Count(), // Đếm số đơn hàng unique
                        LastOrderDate = g.Max(x => x.CreatedDate)
                    })
                    .ToList();

                var topSpendingCustomerNames = orderSummaries
                    .OrderByDescending(x => x.TotalSpent)
                    .Take(10)
                    .Select(x => x.UserName)
                    .ToList();

                var topOrderCustomerNames = orderSummaries
                    .OrderByDescending(x => x.OrderCount)
                    .Take(10)
                    .Select(x => x.UserName)
                    .ToList();

                // VIP customers: có ít nhất 2 đơn hàng hoặc chi tiêu > 100,000 VNĐ (giảm threshold để test)
                var vipCustomerNames = orderSummaries
                    .Where(x => x.TotalSpent > 100000 || x.OrderCount >= 2)
                    .OrderByDescending(x => x.TotalSpent)
                    .Take(20)
                    .Select(x => x.UserName)
                    .ToList();

                // Thêm debug info
                ViewBag.DebugSummary = $"Order summaries: {orderSummaries.Count}, VIP customers: {vipCustomerNames.Count}";
                ViewBag.TopSpenders = string.Join(", ", orderSummaries.Take(5).Select(x => $"{x.UserName}: {x.TotalSpent:N0}"));
                
                // Debug thêm: Kiểm tra UserNames
                ViewBag.DebugUserNames = "UserNames in orders: " + string.Join(", ", orderSummaries.Select(x => x.UserName).Take(5));
                
                var allIdentityUsers = await _userManager.Users.Select(u => u.UserName).ToListAsync();
                ViewBag.DebugIdentityUsers = "Identity UserNames: " + string.Join(", ", allIdentityUsers.Take(5));

                // Lấy tất cả customers để hiển thị trong dropdown  
                var allCustomers = await _userManager.Users
                    .Where(u => u.UserName != null)
                    .Select(u => new { u.Id, u.UserName, u.Email })
                    .ToListAsync();

                model.AllCustomers = allCustomers.Select(u => new CustomerInfo
                {
                    UserId = u.Id,
                    UserName = u.UserName,
                    Email = u.Email
                }).ToList();

                // Lấy danh sách coupons available
                var availableCoupons = await _dataContext.Coupons
                    .Where(c => c.Status == 1 && c.DateExpired > DateTime.Now && c.Quantity > 0)
                    .Select(c => new { c.Id, c.Name, c.Description })
                    .ToListAsync();

                model.AvailableCoupons = availableCoupons.Select(c => new CouponInfo
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description
                }).ToList();

                // Lấy thông tin user - Sửa logic để tìm được users
                var allUserNames = topSpendingCustomerNames
                    .Union(topOrderCustomerNames)
                    .Union(vipCustomerNames)
                    .Distinct()
                    .ToList();

                // Thử tìm users bằng cả UserName và Email
                var users = await _userManager.Users.ToListAsync();
                var foundUsers = new List<AppUserModel>();
                
                foreach (var userName in allUserNames)
                {
                    // Tìm theo UserName
                    var user = users.FirstOrDefault(u => u.UserName == userName);
                    if (user == null)
                    {
                        // Nếu không tìm thấy theo UserName, thử tìm theo Email
                        user = users.FirstOrDefault(u => u.Email == userName);
                    }
                    if (user != null && !foundUsers.Any(fu => fu.Id == user.Id))
                    {
                        foundUsers.Add(user);
                    }
                }

                ViewBag.DebugFoundUsers = $"Found {foundUsers.Count} users from {allUserNames.Count} usernames";

                // Xử lý Top Customers By Spending
                foreach (var customerName in topSpendingCustomerNames)
                {
                    var user = foundUsers.FirstOrDefault(u => u.UserName == customerName || u.Email == customerName);
                    var summary = orderSummaries.FirstOrDefault(s => s.UserName == customerName);
                    
                    if (user != null && summary != null)
                    {
                        model.TopCustomersBySpending.Add(new TopCustomerBySpending
                        {
                            UserId = user.Id,
                            Email = user.Email,
                            UserName = user.UserName,
                            TotalSpent = summary.TotalSpent,
                            OrderCount = summary.OrderCount,
                            IsVIP = vipCustomerNames.Contains(customerName)
                        });
                    }
                    else
                    {
                        // Nếu không tìm thấy user, vẫn hiển thị dữ liệu với thông tin có sẵn
                        model.TopCustomersBySpending.Add(new TopCustomerBySpending
                        {
                            UserId = "unknown",
                            Email = customerName,
                            UserName = customerName,
                            TotalSpent = summary?.TotalSpent ?? 0,
                            OrderCount = summary?.OrderCount ?? 0,
                            IsVIP = vipCustomerNames.Contains(customerName)
                        });
                    }
                }

                // Xử lý Top Customers By Orders
                foreach (var customerName in topOrderCustomerNames)
                {
                    var user = foundUsers.FirstOrDefault(u => u.UserName == customerName || u.Email == customerName);
                    var summary = orderSummaries.FirstOrDefault(s => s.UserName == customerName);
                    
                    if (user != null && summary != null)
                    {
                        model.TopCustomersByOrders.Add(new TopCustomerByOrders
                        {
                            UserId = user.Id,
                            Email = user.Email,
                            UserName = user.UserName,
                            OrderCount = summary.OrderCount,
                            TotalSpent = summary.TotalSpent,
                            IsVIP = vipCustomerNames.Contains(customerName)
                        });
                    }
                    else
                    {
                        // Nếu không tìm thấy user, vẫn hiển thị dữ liệu với thông tin có sẵn
                        model.TopCustomersByOrders.Add(new TopCustomerByOrders
                        {
                            UserId = "unknown",
                            Email = customerName,
                            UserName = customerName,
                            OrderCount = summary?.OrderCount ?? 0,
                            TotalSpent = summary?.TotalSpent ?? 0,
                            IsVIP = vipCustomerNames.Contains(customerName)
                        });
                    }
                }

                // Xử lý VIP Customers
                foreach (var customerName in vipCustomerNames)
                {
                    var user = foundUsers.FirstOrDefault(u => u.UserName == customerName || u.Email == customerName);
                    var summary = orderSummaries.FirstOrDefault(s => s.UserName == customerName);
                    
                    if (user != null && summary != null)
                    {
                        var hasExclusiveVoucher = await _dataContext.CustomerVouchers
                            .AnyAsync(cv => cv.UserId == user.Id && cv.VoucherType == "Exclusive" && !cv.IsUsed);

                        model.VIPCustomers.Add(new VIPCustomer
                        {
                            UserId = user.Id,
                            Email = user.Email,
                            UserName = user.UserName,
                            TotalSpent = summary.TotalSpent,
                            OrderCount = summary.OrderCount,
                            LastOrderDate = summary.LastOrderDate,
                            HasExclusiveVoucher = hasExclusiveVoucher
                        });
                    }
                    else
                    {
                        // Nếu không tìm thấy user, vẫn hiển thị dữ liệu với thông tin có sẵn
                        model.VIPCustomers.Add(new VIPCustomer
                        {
                            UserId = "unknown",
                            Email = customerName,
                            UserName = customerName,
                            TotalSpent = summary?.TotalSpent ?? 0,
                            OrderCount = summary?.OrderCount ?? 0,
                            LastOrderDate = summary?.LastOrderDate ?? DateTime.MinValue,
                            HasExclusiveVoucher = false
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Có lỗi xảy ra: {ex.Message}";
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SendVoucherToVIP(string userId, int couponId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                
                // Kiểm tra xem khách hàng có tồn tại không
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khách hàng" });
                }

                // Tính toán tổng chi tiêu thực tế từ OrderDetails
                var customerSpending = await _dataContext.OrderDetails
                    .Join(_dataContext.Orders, 
                          od => od.OrderCode, 
                          o => o.OrderCode, 
                          (od, o) => new { 
                              od.UserName, 
                              od.Price, 
                              od.Quantity, 
                              o.OrderCode 
                          })
                    .Where(x => x.UserName == user.UserName)
                    .ToListAsync();

                var totalSpent = customerSpending.Sum(x => x.Price * x.Quantity);
                var orderCount = customerSpending.Select(x => x.OrderCode).Distinct().Count();

                // Kiểm tra xem đã có voucher exclusive chưa sử dụng chưa
                var existingVoucher = await _dataContext.CustomerVouchers
                    .AnyAsync(cv => cv.UserId == userId && cv.CouponId == couponId && cv.VoucherType == "Exclusive" && !cv.IsUsed);

                if (existingVoucher)
                {
                    return Json(new { success = false, message = "Khách hàng đã có voucher này chưa sử dụng" });
                }

                // Tạo voucher mới cho khách hàng được chọn
                var customerVoucher = new CustomerVoucherModel
                {
                    UserId = userId,
                    CouponId = couponId,
                    SentDate = DateTime.Now,
                    SentBy = currentUser.Id,
                    VoucherType = "Exclusive"
                };

                _dataContext.CustomerVouchers.Add(customerVoucher);
                await _dataContext.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Gửi voucher độc quyền thành công cho {user.UserName}. Tổng chi tiêu: {totalSpent:N0} VNĐ, Số đơn hàng: {orderCount}" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateExclusiveVoucher(string selectedUserId, int selectedCouponId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                
                // Kiểm tra khách hàng được chọn
                var user = await _userManager.FindByIdAsync(selectedUserId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy khách hàng được chọn" });
                }

                // Kiểm tra coupon có tồn tại và còn hạn không
                var coupon = await _dataContext.Coupons.FindAsync(selectedCouponId);
                if (coupon == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy voucher" });
                }

                if (coupon.DateExpired <= DateTime.Now || coupon.Status != 1 || coupon.Quantity <= 0)
                {
                    return Json(new { success = false, message = "Voucher đã hết hạn hoặc không khả dụng" });
                }

                // KIỂM TRA VOUCHER ĐỘC QUYỀN: Nếu voucher này đã được tạo độc quyền cho ai đó khác
                var existingExclusiveVoucher = await _dataContext.CustomerVouchers
                    .Where(cv => cv.CouponId == selectedCouponId && cv.VoucherType == "Exclusive" && !cv.IsUsed)
                    .FirstOrDefaultAsync();

                if (existingExclusiveVoucher != null && existingExclusiveVoucher.UserId != selectedUserId)
                {
                    var existingUser = await _userManager.FindByIdAsync(existingExclusiveVoucher.UserId);
                    return Json(new { 
                        success = false, 
                        message = $"Voucher này đã được tạo độc quyền cho khách hàng {existingUser?.UserName ?? "khác"} và chưa được sử dụng" 
                    });
                }

                // Kiểm tra xem khách hàng này đã có voucher exclusive của coupon này chưa
                var userHasThisVoucher = await _dataContext.CustomerVouchers
                    .AnyAsync(cv => cv.UserId == selectedUserId && cv.CouponId == selectedCouponId && cv.VoucherType == "Exclusive" && !cv.IsUsed);

                if (userHasThisVoucher)
                {
                    return Json(new { success = false, message = "Khách hàng đã có voucher này chưa sử dụng" });
                }

                // Tạo voucher độc quyền cho khách hàng được chọn
                var customerVoucher = new CustomerVoucherModel
                {
                    UserId = selectedUserId,
                    CouponId = selectedCouponId,
                    SentDate = DateTime.Now,
                    SentBy = currentUser.Id,
                    VoucherType = "Exclusive"
                };

                _dataContext.CustomerVouchers.Add(customerVoucher);
                await _dataContext.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"Tạo voucher độc quyền thành công cho khách hàng {user.UserName} - {coupon.Name}. Chỉ khách hàng này mới có thể sử dụng voucher này!" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }
    }
}
