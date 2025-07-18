using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;

namespace shopping_tutorial.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Color")]
    [Authorize(Roles = "Admin")]
    public class ColorController : Controller
    {
        private readonly DataContext _dataContext;
        
        public ColorController(DataContext context)
        {
            _dataContext = context;
        }
        
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            return View(await _dataContext.Colors.OrderByDescending(c => c.Id).ToListAsync());
        }
        
        [Route("Create")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        
        [Route("Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ColorModel color)
        {
            if (ModelState.IsValid)
            {
                // Check if color name already exists
                var existingColor = await _dataContext.Colors.FirstOrDefaultAsync(c => c.Name.ToLower() == color.Name.ToLower());
                if (existingColor != null)
                {
                    ModelState.AddModelError("", "Màu sắc đã tồn tại trong hệ thống");
                    return View(color);
                }
                
                color.DateCreated = DateTime.Now;
                _dataContext.Add(color);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Thêm màu sắc thành công";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["error"] = "Model có một vài thứ đang bị lỗi";
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
            
            return View(color);
        }
        
        [Route("Edit")]
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            ColorModel color = await _dataContext.Colors.FindAsync(Id);
            if (color == null)
            {
                return NotFound();
            }
            return View(color);
        }
        
        [Route("Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int Id, ColorModel color)
        {
            var existedColor = await _dataContext.Colors.FindAsync(Id);
            if (existedColor == null)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                // Check if color name already exists (excluding current color)
                var duplicateColor = await _dataContext.Colors.FirstOrDefaultAsync(c => c.Name.ToLower() == color.Name.ToLower() && c.Id != Id);
                if (duplicateColor != null)
                {
                    ModelState.AddModelError("", "Màu sắc đã tồn tại trong hệ thống");
                    return View(color);
                }
                
                existedColor.Name = color.Name;
                existedColor.ColorCode = color.ColorCode;
                existedColor.Description = color.Description;
                
                _dataContext.Update(existedColor);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Cập nhật màu sắc thành công";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["error"] = "Model có một vài thứ đang bị lỗi";
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
            
            return View(color);
        }
        
        [Route("Delete")]
        public async Task<IActionResult> Delete(int Id)
        {
            ColorModel color = await _dataContext.Colors.FindAsync(Id);
            if (color == null)
            {
                return NotFound();
            }
            
            // Check if color is being used in any product variants
            var isUsed = await _dataContext.ProductVariants.AnyAsync(pv => pv.ColorId == Id);
            if (isUsed)
            {
                TempData["error"] = "Không thể xóa màu sắc này vì đang được sử dụng trong biến thể sản phẩm";
                return RedirectToAction("Index");
            }
            
            _dataContext.Colors.Remove(color);
            await _dataContext.SaveChangesAsync();
            TempData["success"] = "Xóa màu sắc thành công";
            return RedirectToAction("Index");
        }
    }
}
