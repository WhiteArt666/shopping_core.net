using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;

namespace shopping_tutorial.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Brand")]
    [Authorize(Roles = "Admin")]
    public class BrandController : Controller
    {
        private readonly DataContext _dataContext;
        public BrandController(DataContext context)
        {
            _dataContext = context;

        }
        //[Route("Index")]
        //public async Task<IActionResult> Index()
        //{
        //    return View(await _dataContext.Brands.OrderByDescending(p => p.Id).ToListAsync());
        //}


        [Route("Index")]
        public async Task<IActionResult> Index(int pg = 1)
        {
            List<BrandModel> brand = _dataContext.Brands.ToList();
            const int pageSize = 10;
            if (pg < 1)
            {
                pg = 1;
            }
            int recsCount = brand.Count();
            var pager = new Paginate(recsCount, pg, pageSize);
            int recSkip = (pg - 1) * pageSize;
            var data = brand.Skip(recSkip).Take(pager.PageSize).ToList();
            ViewBag.Pager = pager;
            return View(data);
        }
        [HttpGet]
        [Route("Create")]
        public async Task<IActionResult> Create()
        {
            return View();
        }
        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BrandModel brand)
        {
            if (ModelState.IsValid)
            {
                //code them du lieu ne
                brand.Slug = !string.IsNullOrEmpty(brand.Name)
                 ? brand.Name.Trim().Replace(" ", "-").ToLower()
                 : Guid.NewGuid().ToString(); // fallback nếu null

                var slug = await _dataContext.Brands.FirstOrDefaultAsync(p => p.Slug == brand.Slug);
                if (slug != null)
                {
                    ModelState.AddModelError("", "Thương hiệu đã có trong database");
                    return View(brand);
                }

                _dataContext.Add(brand);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Thêm thương hiệu thành công";
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

            return View(brand);
        }
        [HttpGet]
        [Route("Edit/{Id}")]
        public async Task<IActionResult> Edit(int Id)
        {
            BrandModel brand = await _dataContext.Brands.FindAsync(Id);

            return View(brand);
        }
        [HttpPost]
        [Route("Edit/{Id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BrandModel brand)
        {
            if (ModelState.IsValid)
            {
                //code them du lieu ne
                brand.Slug = brand.Name.Replace(" ", "-");
                var slug = await _dataContext.Brands.FirstOrDefaultAsync(p => p.Slug == brand.Slug);
                if (slug != null)
                {
                    ModelState.AddModelError("", "Thương hiệu đã có trong database");
                    return View(brand);
                }

                _dataContext.Update(brand);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Cập nhật thương hiệu thành công";
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

            return View(brand);
        }
        [HttpGet]
        [Route("Delete/{Id}")]
        public async Task<IActionResult> Delete(int Id)
        {
            BrandModel brand = await _dataContext.Brands.FindAsync(Id);

            _dataContext.Brands.Remove(brand);
            await _dataContext.SaveChangesAsync();
            TempData["error"] = "Thương hiệu đã xóa";
            return RedirectToAction("Index");
        }
    }

}
