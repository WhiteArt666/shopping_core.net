using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;

namespace Shopping_Tutorial.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Coupon")]
    [Authorize(Roles = "Publisher,Author,Admin")]
    public class CouponController : Controller
    {
        private readonly DataContext _dataContext;
        public CouponController(DataContext context)
        {
            _dataContext = context;
        }
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var coupon_list = await _dataContext.Coupons.ToListAsync();
            ViewBag.Coupons = coupon_list;
            return View();
        }
        [Route("Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CouponModel coupon)
        {


            if (ModelState.IsValid)
            {

                _dataContext.Add(coupon);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Thêm coupon thành công";
                return RedirectToAction("Index");

            }
            else
            {
                TempData["error"] = "Model có một vài thứ đang lỗi";
                List<string> errors = new List<string>();
                foreach (var value in ModelState.Values)
                {
                    foreach (var error in value.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }
                string errorMessage = string.Join("\n", errors);
                return BadRequest(errorMessage);
            }

            return View();
        }
        [Route("Edit/{id}")]
[HttpGet]
public async Task<IActionResult> Edit(int id)
{
    var coupon = await _dataContext.Coupons.FindAsync(id);
    if (coupon == null)
        return NotFound();

    return View(coupon);
}

[Route("Edit/{id}")]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(CouponModel model)
{
    if (!ModelState.IsValid)
        return View(model);

    _dataContext.Update(model);
    await _dataContext.SaveChangesAsync();
    TempData["success"] = "Cập nhật mã thành công";
    return RedirectToAction("Index");
}

[Route("Delete/{id}")]
[HttpPost]
public async Task<IActionResult> Delete(int id)
{
    var coupon = await _dataContext.Coupons.FindAsync(id);
    if (coupon != null)
    {
        _dataContext.Coupons.Remove(coupon);
        await _dataContext.SaveChangesAsync();
    }
    return RedirectToAction("Index");
}

    }
}