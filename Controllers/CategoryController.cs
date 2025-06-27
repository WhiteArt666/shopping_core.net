using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;

namespace shopping_tutorial.Controllers
{
	public class CategoryController: Controller
	{
        private readonly DataContext _dataContext;
        public CategoryController(DataContext context)
        {
            _dataContext = context;
        }
        public async Task <IActionResult> Index(string Slug="")
        {
            CategoryModel category= _dataContext.Categories.Where(c => c.Slug == Slug).FirstOrDefault();
            if (category == null) return RedirectToAction("Index");
            var productByCategory =_dataContext.Products.Where(p  => p.CategoryId== category.Id);
            return View(await productByCategory.OrderByDescending(p => p.Id).ToListAsync());
        }
       
	}
}

