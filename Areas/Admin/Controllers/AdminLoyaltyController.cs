using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;

namespace shopping_tutorial.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("Admin/Loyalty")]
    public class AdminLoyaltyController : Controller
    {
        private readonly DataContext _context;
        public AdminLoyaltyController(DataContext context)
        {
            _context = context;
        }

        [HttpGet("UserPoints")]
        public async Task<IActionResult> UserPoints()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.LoyaltyPoints
                }).ToListAsync();

            return View(users);
        }

        [HttpGet("History/{userId}")]
        public async Task<IActionResult> History(string userId)
        {
            var history = await _context.LoyaltyHistories
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();

            ViewBag.UserId = userId;
            return View(history);
        }
    }
}
