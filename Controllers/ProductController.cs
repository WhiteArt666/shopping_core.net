using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;

namespace shopping_tutorial.Controllers
{
	public class ProductController : Controller
	{
		private readonly DataContext _dataContext;
		public ProductController(DataContext context)
		{
			_dataContext = context;
		}
		public IActionResult Index()
		{
			return View();
		}

		public async Task<IActionResult> Search(string searchTerm)
		{
			var products = await _dataContext.Products
			.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm))
			.ToListAsync();

			ViewBag.Keyword = searchTerm;

			return View(products);
		}

		public async Task<IActionResult> Details(int Id)
		{
			if (Id <= 0) return RedirectToAction("Index");
			var productById = await _dataContext.Products
				.Include(p => p.Ratings)
				.Include(p => p.ProductVariants)
					.ThenInclude(pv => pv.Color)
				.Include(p => p.ProductVariants)
					.ThenInclude(pv => pv.Size)
				.Include(p => p.Category)
				.Include(p => p.Brand)
				.FirstOrDefaultAsync(p => p.Id == Id);
			
			if (productById == null) return RedirectToAction("Index");
			
			//get ratings for this product
			var productRatings = await _dataContext.Ratings.Where(r => r.ProductId == Id).ToListAsync();
			ViewBag.ProductRatings = productRatings;
			
			//related product
			var relatedProducts = await _dataContext.Products
			.Where(p => p.CategoryId == productById.CategoryId && p.Id != productById.Id)
			.Take(4)
			.ToListAsync();

			ViewBag.RelatedProducts = relatedProducts;

			var viewModel = new ProductDetailsViewModel
			{
				ProductDetails = productById,

			};


			return View(viewModel);
		}
		[HttpPost]
		[ValidateAntiForgeryToken]

		public async Task<IActionResult> CommentProduct(RatingModel rating)
		{
			if (ModelState.IsValid)
			{

				var ratingEntity = new RatingModel
				{
					ProductId = rating.ProductId,
					Name = rating.Name,
					Email = rating.Email,
					Comment = rating.Comment,
					Star = rating.Star

				};

				_dataContext.Ratings.Add(ratingEntity);
				await _dataContext.SaveChangesAsync();

				TempData["success"] = "Thêm đánh giá thành công";

				return Redirect(Request.Headers["Referer"]);
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

				return RedirectToAction("Detail", new { id = rating.ProductId });
			}

			return Redirect(Request.Headers["Referer"]);
		}      
	}
}

