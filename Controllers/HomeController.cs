﻿using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;

namespace shopping_tutorial.Controllers;

public class HomeController : Controller
{
    private readonly DataContext _dataContext;
    private readonly ILogger<HomeController> _logger;
    private readonly UserManager<AppUserModel> _userManager;


    public HomeController(ILogger<HomeController> logger, DataContext context, UserManager<AppUserModel> userManager)
    {
        _logger = logger;
        _dataContext = context;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        var products = _dataContext.Products
            .Include("Category")
            .Include("Brand")
            .Include(p => p.ProductVariants)
                .ThenInclude(pv => pv.Color)
            .Include(p => p.ProductVariants)
                .ThenInclude(pv => pv.Size)
            .ToList();

        var sliders = _dataContext.Sliders.Where(s => s.Status == 1).ToList();
        ViewBag.Sliders = sliders;

        // Best Selling: Products with highest Sold count
        var bestSellingProducts = _dataContext.Products
            .Include("Category")
            .Include("Brand")
            .Include(p => p.ProductVariants)
                .ThenInclude(pv => pv.Color)
            .Include(p => p.ProductVariants)
                .ThenInclude(pv => pv.Size)
            .OrderByDescending(p => p.Sold)
            .Take(4)
            .ToList();
        ViewBag.BestSellingProducts = bestSellingProducts;

        // On Selling: Products with highest Quantity
        var onSellingProducts = _dataContext.Products
            .Include("Category")
            .Include("Brand")
            .Include(p => p.ProductVariants)
                .ThenInclude(pv => pv.Color)
            .Include(p => p.ProductVariants)
                .ThenInclude(pv => pv.Size)
            .OrderByDescending(p => p.Price)
            .Take(4)
            .ToList();
        ViewBag.OnSellingProducts = onSellingProducts;

        // Top Rating: Random products
        var topRatingProducts = _dataContext.Products
            .Include("Category")
            .Include("Brand")
            .Include(p => p.ProductVariants)
                .ThenInclude(pv => pv.Color)
            .Include(p => p.ProductVariants)
                .ThenInclude(pv => pv.Size)
            .OrderBy(p => p.Sold)
            .Take(4)
            .ToList();
        ViewBag.TopRatingProducts = topRatingProducts;

        return View(products);
    }
    public async Task<IActionResult> ContactAsync()
    {
        var contact = await _dataContext.Contact.FirstAsync();
        return View(contact);
    }

    public async Task<IActionResult> Wishlist()
    {
        var wishlist_product = await (from w in _dataContext.Wishlists
                                      join p in _dataContext.Products on w.ProductId equals p.Id
                                      select new { Product = p, Wishlists = w })
                           .ToListAsync();

        return View(wishlist_product);
    }


    [HttpPost]
    public async Task<IActionResult> AddWishlist(int Id, WishlistModel wishlistmodel)
    {
        var user = await _userManager.GetUserAsync(User);

        var wishlistProduct = new WishlistModel
        {
            ProductId = Id,
            UserId = user.Id
        };

        _dataContext.Wishlists.Add(wishlistProduct);
        try
        {
            await _dataContext.SaveChangesAsync();
            return Ok(new { success = true, message = "Add to wishlisht Successfully" });
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while adding to wishlist table.");
        }

    }

    public async Task<IActionResult> DeleteWishlist(int Id)
        {
            WishlistModel wishlist = await _dataContext.Wishlists.FindAsync(Id);

            _dataContext.Wishlists.Remove(wishlist);

            await _dataContext.SaveChangesAsync();
            TempData["success"] = "Yêu thích đã được xóa thành công";
            return RedirectToAction("Wishlist", "Home");
        }
    public async Task<IActionResult> Compare()
    {
        var compare_product = await (from c in _dataContext.Compares
                                     join p in _dataContext.Products on c.ProductId equals p.Id
                                     join u in _dataContext.Users on c.UserId equals u.Id
                                     select new { User = u, Product = p, Compares = c })
                           .ToListAsync();

        return View(compare_product);
    }
    [HttpPost]
    public async Task<IActionResult> AddCompare(int Id)
    {
        var user = await _userManager.GetUserAsync(User);

        var compareProduct = new CompareModel
        {
            ProductId = Id,
            UserId = user.Id
        };

        _dataContext.Compares.Add(compareProduct);
        try
        {
            await _dataContext.SaveChangesAsync();
            return Ok(new { success = true, message = "Add to compare Successfully" });
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while adding to compare table.");
        }

    }
    public async Task<IActionResult> DeleteCompare(int Id)
        {
            CompareModel compare = await _dataContext.Compares.FindAsync(Id);

            _dataContext.Compares.Remove(compare);

            await _dataContext.SaveChangesAsync();
            TempData["success"] = "So sánh đã được xóa thành công";
            return RedirectToAction("Compare", "Home");
        }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int statuscode)
    {
        if (statuscode == 404)
        {
            return View("NotFound");
        }
        else
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}

