using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Data;
using shopping_tutorial.Models.ViewModels;
using shopping_tutorial.Repository;

namespace shopping_tutorial.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly DataContext _context;

        public AdminController(DataContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var now = DateTime.Now;

            var revenueByMonth = await _context.Orders
                .Where(o => o.CreatedDate.Year == now.Year && o.Status == 2)
                .GroupBy(o => o.CreatedDate.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(o => o.ShippingCost) })
                .ToListAsync();

            var ordersByMonth = await _context.Orders
                .Where(o => o.CreatedDate.Year == now.Year)
                .GroupBy(o => o.CreatedDate.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToListAsync();

            var model = new DashboardViewModel
            {
                RevenueByMonth = revenueByMonth.ToDictionary(x => x.Month, x => x.Total),
                OrdersByMonth = ordersByMonth.ToDictionary(x => x.Month, x => x.Count)
            };

            return View(model);
        }
    }
}
