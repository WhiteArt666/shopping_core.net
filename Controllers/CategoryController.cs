﻿using System;
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
        public async Task<IActionResult> Index(string slug = "", string sort_by = "", string startprice = "", string endprice = "")
        {
            CategoryModel category = _dataContext.Categories.Where(c => c.Slug == slug).FirstOrDefault();


            if (category == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.Slug = slug;
            IQueryable<ProductModel> productsByCategory = _dataContext.Products
                .Include(p => p.ProductVariants)
                    .ThenInclude(pv => pv.Color)
                .Include(p => p.ProductVariants)
                    .ThenInclude(pv => pv.Size)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.CategoryId == category.Id);
            var count = await productsByCategory.CountAsync();
            if (count > 0)
            {
                // Apply sorting based on sort_by parameter
                if (sort_by == "price_increase")
                {
                    productsByCategory = productsByCategory.OrderBy(p => p.Price);
                }
                else if (sort_by == "price_decrease")
                {
                    productsByCategory = productsByCategory.OrderByDescending(p => p.Price);
                }
                else if (sort_by == "price_newest")
                {
                    productsByCategory = productsByCategory.OrderByDescending(p => p.Id);
                }
                else if (sort_by == "price_oldest")
                {
                    productsByCategory = productsByCategory.OrderBy(p => p.Id);
                }
                else if (startprice != "" && endprice != "")
                {
                    decimal startPriceValue;
                    decimal endPriceValue;

                    if (decimal.TryParse(startprice, out startPriceValue) && decimal.TryParse(endprice, out endPriceValue))
                    {
                        productsByCategory = productsByCategory.Where(p => p.Price >= startPriceValue && p.Price <= endPriceValue);
                    }
                    else
                    {
                        productsByCategory = productsByCategory.OrderByDescending(p => p.Id);
                    }
                }
                else
                {
                    productsByCategory = productsByCategory.OrderByDescending(p => p.Id);
                }

                decimal minPrice = await productsByCategory.MinAsync(p => p.Price);
                decimal maxPrice = await productsByCategory.MaxAsync(p => p.Price);


                ViewBag.sort_key = sort_by;

                ViewBag.count = count;

                ViewBag.minprice = minPrice;
                ViewBag.maxprice = maxPrice;
            }

            // Add more sorting options if needed (e.g., name, date)
            return View(await productsByCategory.ToListAsync());
        }
       
	}
}

