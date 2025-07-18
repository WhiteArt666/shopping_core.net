using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;

namespace shopping_tutorial.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/ProductVariant")]
    [Authorize(Roles = "Admin")]
    public class ProductVariantController : Controller
    {
        private readonly DataContext _dataContext;
        
        public ProductVariantController(DataContext context)
        {
            _dataContext = context;
        }
        
        [Route("Index")]
        public async Task<IActionResult> Index(int? productId)
        {
            IQueryable<ProductVariantModel> query = _dataContext.ProductVariants
                .Include(pv => pv.Product)
                .Include(pv => pv.Color)
                .Include(pv => pv.Size)
                .OrderByDescending(pv => pv.Id);
                
            if (productId.HasValue)
            {
                query = query.Where(pv => pv.ProductId == productId.Value);
                ViewBag.ProductId = productId.Value;
                var product = await _dataContext.Products.FindAsync(productId.Value);
                ViewBag.ProductName = product?.Name;
            }
            
            return View(await query.ToListAsync());
        }
        
        [Route("Create")]
        [HttpGet]
        public async Task<IActionResult> Create(int? productId)
        {
            await PopulateViewBags(productId);
            var variant = new ProductVariantModel();
            if (productId.HasValue)
            {
                variant.ProductId = productId.Value;
                // Set default original price from product
                var product = await _dataContext.Products.FindAsync(productId.Value);
                if (product != null)
                {
                    variant.OriginalPrice = product.Price;
                }
            }
            return View(variant);
        }
        
        [Route("Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductVariantModel variant)
        {
            if (ModelState.IsValid)
            {
                // Check if variant with same product, color, and size already exists
                var existingVariant = await _dataContext.ProductVariants
                    .FirstOrDefaultAsync(pv => pv.ProductId == variant.ProductId && 
                                             pv.ColorId == variant.ColorId && 
                                             pv.SizeId == variant.SizeId);
                                             
                if (existingVariant != null)
                {
                    ModelState.AddModelError("", "Biến thể sản phẩm với màu sắc và size này đã tồn tại");
                    await PopulateViewBags(variant.ProductId);
                    return View(variant);
                }
                
                // Generate SKU
                var product = await _dataContext.Products.FindAsync(variant.ProductId);
                var color = await _dataContext.Colors.FindAsync(variant.ColorId);
                var size = await _dataContext.Sizes.FindAsync(variant.SizeId);
                
                variant.SKU = $"{product.Name.Replace(" ", "").Substring(0, Math.Min(3, product.Name.Length))}-{color.Name}-{size.Name}".ToUpper();
                variant.DateCreated = DateTime.Now;
                variant.DateUpdated = DateTime.Now;
                
                _dataContext.Add(variant);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Thêm biến thể sản phẩm thành công";
                return RedirectToAction("Index", new { productId = variant.ProductId });
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
                await PopulateViewBags(variant.ProductId);
                return View(variant);
            }
        }
        
        [Route("Edit")]
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            var variant = await _dataContext.ProductVariants.FindAsync(Id);
            if (variant == null)
            {
                return NotFound();
            }
            await PopulateViewBags(variant.ProductId);
            return View(variant);
        }
        
        [Route("Edit")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int Id, ProductVariantModel variant)
        {
            var existedVariant = await _dataContext.ProductVariants.FindAsync(Id);
            if (existedVariant == null)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                // Check if variant with same product, color, and size already exists (excluding current)
                var duplicateVariant = await _dataContext.ProductVariants
                    .FirstOrDefaultAsync(pv => pv.ProductId == variant.ProductId && 
                                             pv.ColorId == variant.ColorId && 
                                             pv.SizeId == variant.SizeId &&
                                             pv.Id != Id);
                                             
                if (duplicateVariant != null)
                {
                    ModelState.AddModelError("", "Biến thể sản phẩm với màu sắc và size này đã tồn tại");
                    await PopulateViewBags(variant.ProductId);
                    return View(variant);
                }
                
                existedVariant.ProductId = variant.ProductId;
                existedVariant.ColorId = variant.ColorId;
                existedVariant.SizeId = variant.SizeId;
                existedVariant.OriginalPrice = variant.OriginalPrice;
                existedVariant.DiscountPrice = variant.DiscountPrice;
                existedVariant.Quantity = variant.Quantity;
                existedVariant.IsActive = variant.IsActive;
                existedVariant.DateUpdated = DateTime.Now;
                
                // Update SKU
                var product = await _dataContext.Products.FindAsync(variant.ProductId);
                var color = await _dataContext.Colors.FindAsync(variant.ColorId);
                var size = await _dataContext.Sizes.FindAsync(variant.SizeId);
                
                existedVariant.SKU = $"{product.Name.Replace(" ", "").Substring(0, Math.Min(3, product.Name.Length))}-{color.Name}-{size.Name}".ToUpper();
                
                _dataContext.Update(existedVariant);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Cập nhật biến thể sản phẩm thành công";
                return RedirectToAction("Index", new { productId = variant.ProductId });
            }
            else
            {
                TempData["error"] = "Model có một vài thứ đang bị lỗi";
                await PopulateViewBags(variant.ProductId);
                return View(variant);
            }
        }
        
        [Route("Delete")]
        public async Task<IActionResult> Delete(int Id)
        {
            var variant = await _dataContext.ProductVariants.FindAsync(Id);
            if (variant == null)
            {
                return NotFound();
            }
            
            int productId = variant.ProductId;
            _dataContext.ProductVariants.Remove(variant);
            await _dataContext.SaveChangesAsync();
            TempData["success"] = "Xóa biến thể sản phẩm thành công";
            return RedirectToAction("Index", new { productId = productId });
        }
        
        [Route("ToggleActive")]
        public async Task<IActionResult> ToggleActive(int Id)
        {
            var variant = await _dataContext.ProductVariants.FindAsync(Id);
            if (variant == null)
            {
                return NotFound();
            }
            
            variant.IsActive = !variant.IsActive;
            variant.DateUpdated = DateTime.Now;
            
            _dataContext.Update(variant);
            await _dataContext.SaveChangesAsync();
            
            string status = variant.IsActive ? "kích hoạt" : "vô hiệu hóa";
            TempData["success"] = $"Đã {status} biến thể sản phẩm thành công";
            
            return RedirectToAction("Index", new { productId = variant.ProductId });
        }
        
        [Route("GetProductPrice")]
        [HttpGet]
        public async Task<IActionResult> GetProductPrice(int productId)
        {
            var product = await _dataContext.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }
            
            return Json(new { success = true, price = product.Price });
        }
        
        private async Task PopulateViewBags(int? selectedProductId = null)
        {
            ViewBag.Products = new SelectList(await _dataContext.Products.ToListAsync(), "Id", "Name", selectedProductId);
            ViewBag.Colors = new SelectList(await _dataContext.Colors.ToListAsync(), "Id", "Name");
            ViewBag.Sizes = new SelectList(await _dataContext.Sizes.ToListAsync(), "Id", "Name");
        }
    }
}
