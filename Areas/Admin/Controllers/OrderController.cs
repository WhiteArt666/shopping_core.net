using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Repository;

namespace shopping_tutorial.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    [Authorize]
    public class OrderController : Controller
	{
        private readonly DataContext _dataContext; //để lấy cơ sở dữ liệu mình cần 
        public OrderController(DataContext context)
        {
            _dataContext = context;

        }
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            return View(await _dataContext.Orders.OrderByDescending(p => p.Id).ToListAsync());
        }
        public async Task<IActionResult> ViewOrder(string ordercode)
        {
            var DetailsOrder = await _dataContext.OrderDetails.Include(od=>od.Product).Where(od => od.OrderCode == ordercode).ToListAsync(); // od là order 
            return View(DetailsOrder);
        }
        [HttpPost]
        // [Route("UpdateOrder")]
        public async Task<IActionResult> UpdateOrder(string ordercode, int status)
        {
            var order = await _dataContext.Orders.FirstOrDefaultAsync(o => o.OrderCode == ordercode);
            if(order == null)
            {
                return NotFound();
            }
            order.Status = status;
            try
            {
                await _dataContext.SaveChangesAsync();
                return Ok(new { success = true, message = "Order status updated successfully" });
            }
            catch(Exception ex)
            {
                return StatusCode(500, "An error occurred while updating the order status");
            }
        }
    }
}
