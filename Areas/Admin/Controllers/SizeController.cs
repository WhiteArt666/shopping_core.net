using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;

namespace shopping_tutorial.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Size")]
    [Authorize(Roles = "Admin")]
    public class SizeController : Controller
    {
        private readonly DataContext _dataContext;
        
        public SizeController(DataContext context)
        {
            _dataContext = context;
        }
        
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            return View(await _dataContext.Sizes.OrderByDescending(s => s.Id).ToListAsync());
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
        public async Task<IActionResult> Create(SizeModel size)
        {
            if (ModelState.IsValid)
            {
                // Check if size name already exists
                var existingSize = await _dataContext.Sizes.FirstOrDefaultAsync(s => s.Name.ToLower() == size.Name.ToLower());
                if (existingSize != null)
                {
                    ModelState.AddModelError("", "Size đã tồn tại trong hệ thống");
                    return View(size);
                }
                
                size.DateCreated = DateTime.Now;
                _dataContext.Add(size);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Thêm size thành công";
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
            
            return View(size);
        }
        
        [Route("Edit")]
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            SizeModel size = await _dataContext.Sizes.FindAsync(Id);
            if (size == null)
            {
                return NotFound();
            }
            return View(size);
        }
        
        [Route("Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int Id, SizeModel size)
        {
            var existedSize = await _dataContext.Sizes.FindAsync(Id);
            if (existedSize == null)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                // Check if size name already exists (excluding current size)
                var duplicateSize = await _dataContext.Sizes.FirstOrDefaultAsync(s => s.Name.ToLower() == size.Name.ToLower() && s.Id != Id);
                if (duplicateSize != null)
                {
                    ModelState.AddModelError("", "Size đã tồn tại trong hệ thống");
                    return View(size);
                }
                
                existedSize.Name = size.Name;
                existedSize.Description = size.Description;
                
                _dataContext.Update(existedSize);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Cập nhật size thành công";
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
            
            return View(size);
        }
        
        [Route("Delete")]
        public async Task<IActionResult> Delete(int Id)
        {
            SizeModel size = await _dataContext.Sizes.FindAsync(Id);
            if (size == null)
            {
                return NotFound();
            }
            
            // Check if size is being used in any product variants
            var isUsed = await _dataContext.ProductVariants.AnyAsync(pv => pv.SizeId == Id);
            if (isUsed)
            {
                TempData["error"] = "Không thể xóa size này vì đang được sử dụng trong biến thể sản phẩm";
                return RedirectToAction("Index");
            }
            
            _dataContext.Sizes.Remove(size);
            await _dataContext.SaveChangesAsync();
            TempData["success"] = "Xóa size thành công";
            return RedirectToAction("Index");
        }
    }
}
