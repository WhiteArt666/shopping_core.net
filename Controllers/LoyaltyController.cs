using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;
using System.Security.Claims;

public class LoyaltyController : Controller
{
    private readonly DataContext _ctx;

    public LoyaltyController(DataContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<IActionResult> History()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return RedirectToAction("Login", "Account");

        var history = await _ctx.LoyaltyHistories
                                .Where(h => h.UserId == userId)
                                .OrderByDescending(h => h.CreatedAt)
                                .ToListAsync();

        return View("LoyaltyHistory", history);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Redeem(int couponId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var coupon = await _ctx.Coupons.FirstOrDefaultAsync(c => c.Id == couponId && c.RequiredPoints != null);

        if (user == null || coupon == null)
            return NotFound();

        if (user.LoyaltyPoints < coupon.RequiredPoints)
        {
            TempData["error"] = "Bạn không đủ điểm để đổi mã này.";
            return RedirectToAction("AvailableCoupons");
        }

        user.LoyaltyPoints -= coupon.RequiredPoints.Value;
        _ctx.Users.Update(user);

        _ctx.Add(new UserCouponModel
        {
            UserId = user.Id,
            CouponId = coupon.Id,
            RedeemedAt = DateTime.Now,
            IsUsed = false
        });

        await _ctx.SaveChangesAsync();

        TempData["success"] = $"Đã đổi thành công mã: {coupon.Name}";
        return RedirectToAction("MyCoupons");
    }
    [Authorize]
[HttpGet]
public async Task<IActionResult> AvailableCoupons()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == userId);

    if (user == null)
        return RedirectToAction("Login", "Account");

    var coupons = await _ctx.Coupons
        .Where(c => c.RequiredPoints != null && c.Status == 1 && c.DateExpired >= DateTime.Now)
        .ToListAsync();

    ViewBag.LoyaltyPoints = user.LoyaltyPoints;

    return View("AvailableCoupons", coupons);
}

[Authorize]
public async Task<IActionResult> MyCoupons()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    var myCoupons = await _ctx.UserCoupons
        .Include(uc => uc.Coupon)
        .Where(uc => uc.UserId == userId)
        .ToListAsync();

    return View("MyCoupons", myCoupons);
}

}
