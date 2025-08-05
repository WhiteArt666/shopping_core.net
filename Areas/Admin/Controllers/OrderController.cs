using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Repository;
using shopping_tutorial.Models;

namespace Shopping_Tutorial.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Order")]
    [Authorize(Roles = "Publisher,Author,Admin")]
    public class OrderController : Controller
    {
        private readonly DataContext _dataContext;
        public OrderController(DataContext context)
        {
            _dataContext = context;
        }
        [HttpGet]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            return View(await _dataContext.Orders.OrderByDescending(p => p.Id).ToListAsync());
        }
        [HttpGet]
        [Route("ViewOrder")]
        public async Task<IActionResult> ViewOrder(string ordercode)
        {
            var DetailsOrder = await _dataContext.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.ProductVariant)
                .ThenInclude(pv => pv.Color)
                .Include(od => od.ProductVariant)
                .ThenInclude(pv => pv.Size)
                .Where(od => od.OrderCode == ordercode)
                .ToListAsync();

            var Order = _dataContext.Orders.Where(o => o.OrderCode == ordercode).First();

            ViewBag.ShippingCost = Order.ShippingCost;
            ViewBag.Status = Order.Status;
            return View(DetailsOrder);
        }
        [HttpPost]
        [Route("UpdateOrder")]
        public async Task<IActionResult> UpdateOrder(string ordercode, int status)
        {
            var order = await _dataContext.Orders.FirstOrDefaultAsync(o => o.OrderCode == ordercode);

            if (order == null)
            {
                return NotFound();
            }

            var oldStatus = order.Status;
            order.Status = status;

            try
            {
                await _dataContext.SaveChangesAsync();

                // ✅ Tích điểm khi đơn hàng hoàn thành (status = 5)
                if (status == 5 && oldStatus != 5)
                {
                    await GivePointsForCompletedOrder(ordercode);
                }

                return Ok(new { success = true, message = "Order status updated successfully" });
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while updating the order status.");
            }
        }

        private async Task GivePointsForCompletedOrder(string ordercode)
        {
            try
            {
                // Lấy thông tin order và user
                var order = await _dataContext.Orders.FirstOrDefaultAsync(o => o.OrderCode == ordercode);
                if (order == null) return;

                var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.Email == order.UserName || u.UserName == order.UserName);
                if (user == null) return;

                // Tính tổng giá trị đơn hàng để tích điểm
                var orderDetails = await _dataContext.OrderDetails
                    .Where(od => od.OrderCode == ordercode)
                    .ToListAsync();

                decimal totalAmount = orderDetails.Sum(od => od.Price * od.Quantity);
                
                // Tích điểm: 1 điểm cho mỗi 10,000 VNĐ
                int pointsToAdd = (int)(totalAmount / 10000);

                if (pointsToAdd > 0)
                {
                    // Cập nhật điểm cho user
                    user.LoyaltyPoints += pointsToAdd;
                    _dataContext.Users.Update(user);

                    // Lưu lịch sử tích điểm
                    var loyaltyHistory = new shopping_tutorial.Models.LoyaltyHistoryModel
                    {
                        UserId = user.Id,
                        Points = pointsToAdd,
                        Reason = $"Hoàn thành đơn hàng {ordercode} - Tổng giá trị: {totalAmount:N0} VNĐ",
                        CreatedAt = DateTime.Now
                    };

                    _dataContext.LoyaltyHistories.Add(loyaltyHistory);
                    await _dataContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Log error nhưng không throw để không ảnh hưởng đến việc cập nhật status
                Console.WriteLine($"Error adding loyalty points: {ex.Message}");
            }
        }
        [HttpGet]
        [Route("Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _dataContext.Orders.FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            try
            {
                _dataContext.Orders.Remove(order);
                await _dataContext.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while deleting the order.");
            }
        }


    }
}