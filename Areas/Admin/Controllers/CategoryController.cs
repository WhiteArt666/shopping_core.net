using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;

namespace shopping_tutorial.Areas.Admin.Controllers
{

    [Area("Admin")]
    [Route("Admin/Category")]
    [Authorize(Roles = "Admin")]

    public class CategoryController : Controller
    {
        private readonly DataContext _dataContext;
        public CategoryController(DataContext context)
        {
            _dataContext = context;

        }
        [Route("Index")]
        public async Task<IActionResult> Index(int pg = 1)
        {
            List<CategoryModel> category = _dataContext.Categories.ToList(); //33 items 
            const int pageSize = 10;  // 10 items/ trang giống trong Paginate 
            if (pg < 1)
            {
                pg = 1;
            }
            int recsCount = category.Count(); // 33 items 
            var pager = new Paginate(recsCount, pg, pageSize);
            int recSkip = (pg - 1) * pageSize; // (3 - 1 ) * 10

            // category.Skip(20).Take(10).ToList(); 
            var data = category.Skip(recSkip).Take(pager.PageSize).ToList();
            ViewBag.Pager = pager;
            return View(data);
        }
        [Route("Create")]
        public IActionResult Create()
        {

            return View();
        }
        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryModel category)
        {
            if (ModelState.IsValid)
            {
                //code them du lieu ne
                category.Slug = category.Name.Replace(" ", "-");
                var slug = await _dataContext.Categories.FirstOrDefaultAsync(p => p.Slug == category.Slug);
                if (slug != null)
                {
                    ModelState.AddModelError("", "Danh mục đã có trong database");
                    return View(category);
                }




                _dataContext.Add(category);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Thêm danh mục thành công";
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

            // Ghi log lỗi nếu không hợp lệ
            TempData["error"] = "ModelState không hợp lệ";


            return View(category);
        }
        [HttpGet]
        [Route("Edit/{Id}")]
        public async Task<IActionResult> Edit(int Id)
        {
            CategoryModel category = await _dataContext.Categories.FindAsync(Id);

            return View(category);
        }
        [HttpPost]
        [Route("Edit/{Id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryModel category)
        {
            if (ModelState.IsValid)
            {
                //code them du lieu ne
                category.Slug = category.Name.Replace(" ", "-");
                var slug = await _dataContext.Categories.FirstOrDefaultAsync(p => p.Slug == category.Slug);
                if (slug != null)
                {
                    ModelState.AddModelError("", "Danh mục đã có trong database");
                    return View(category);
                }




                _dataContext.Update(category);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Cập nhật danh mục thành công";
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

            return View(category);
        }
        [HttpGet]
        [Route("Delete/{Id}")]
        public async Task<IActionResult> Delete(int Id)
        {
            CategoryModel category = await _dataContext.Categories.FindAsync(Id);

            _dataContext.Categories.Remove(category);
            await _dataContext.SaveChangesAsync();
            TempData["error"] = "Danh mục đã xóa";
            return RedirectToAction("Index");
        }

    }
}
