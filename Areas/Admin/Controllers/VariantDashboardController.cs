using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;

namespace shopping_tutorial.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/VariantDashboard")]
    [Authorize(Roles = "Admin")]
    public class VariantDashboardController : Controller
    {
        private readonly DataContext _dataContext;
        
        public VariantDashboardController(DataContext context)
        {
            _dataContext = context;
        }
        
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            // Statistics for dashboard
            var stats = new
            {
                TotalProducts = await _dataContext.Products.CountAsync(),
                TotalColors = await _dataContext.Colors.CountAsync(),
                TotalSizes = await _dataContext.Sizes.CountAsync(),
                TotalVariants = await _dataContext.ProductVariants.CountAsync(),
                ActiveVariants = await _dataContext.ProductVariants.CountAsync(pv => pv.IsActive),
                InactiveVariants = await _dataContext.ProductVariants.CountAsync(pv => !pv.IsActive),
                VariantsWithDiscount = await _dataContext.ProductVariants.CountAsync(pv => pv.DiscountPrice > 0),
                OutOfStockVariants = await _dataContext.ProductVariants.CountAsync(pv => pv.Quantity == 0)
            };
            
            ViewBag.Stats = stats;
            
            // Recent variants
            var recentVariants = await _dataContext.ProductVariants
                .Include(pv => pv.Product)
                .Include(pv => pv.Color)
                .Include(pv => pv.Size)
                .OrderByDescending(pv => pv.DateCreated)
                .Take(10)
                .ToListAsync();
                
            ViewBag.RecentVariants = recentVariants;
            
            // Top selling variants
            var topSellingVariants = await _dataContext.ProductVariants
                .Include(pv => pv.Product)
                .Include(pv => pv.Color)
                .Include(pv => pv.Size)
                .Where(pv => pv.Sold > 0)
                .OrderByDescending(pv => pv.Sold)
                .Take(10)
                .ToListAsync();
                
            ViewBag.TopSellingVariants = topSellingVariants;
            
            return View();
        }
    }
}
