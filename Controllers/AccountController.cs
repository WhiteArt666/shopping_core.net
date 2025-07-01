using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Areas.Admin.Repository;
using shopping_tutorial.Models;
using shopping_tutorial.Models.ViewModels;
using shopping_tutorial.Repository;
using Shopping_Tutorial.Repository;
using System.Security.Claims;

namespace Shopping_Tutorial.Controllers
{
	public class AccountController : Controller
	{
		private UserManager<AppUserModel> _userManage;
		private SignInManager<AppUserModel> _signInManager;
		private readonly IEmailSender _emailSender;
		private readonly DataContext _dataContext;
		public AccountController(IEmailSender emailSender, UserManager<AppUserModel> userManage,
			SignInManager<AppUserModel> signInManager, DataContext context)
		{
			_userManage = userManage;
			_signInManager = signInManager;
			_emailSender = emailSender;
			_dataContext = context;

		}
		public IActionResult Login(string returnUrl)
		{
			return View(new LoginViewModel { ReturnUrl = returnUrl });
		}

		[HttpPost]
		public async Task<IActionResult> SendMailForgotPass(AppUserModel user)
		{
			var checkMail = await _userManage.Users.FirstOrDefaultAsync(u => u.Email == user.Email);

			if (checkMail == null)
			{
				TempData["error"] = "Email not found";
				return RedirectToAction("ForgotPass", "Account");
			}
			else
			{
				string token = Guid.NewGuid().ToString();
				//update token to user
				_dataContext.Update(checkMail);
				await _dataContext.SaveChangesAsync();
				var receiver = checkMail.Email;
				var subject = "Change password for user " + checkMail.Email;
				var message = "Click on link to change password " +
					"<a href='" + $"{Request.Scheme}://{Request.Host}/Account/NewPass?email=" + checkMail.Email + "&token=" + token + "'>";

				await _emailSender.SendEmailAsync(receiver, subject, message);
			}


			TempData["success"] = "An email has been sent to your registered email address with password reset instructions.";
			return RedirectToAction("ForgotPass", "Account");
		}
		public IActionResult ForgotPass()
		{
			return View();
		}
		public async Task<IActionResult> History()
		{
			if ((bool)!User.Identity?.IsAuthenticated)
			{
				// User is not logged in, redirect to login
				return RedirectToAction("Login", "Account"); // Replace "Account" with your controller name
			}
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var userEmail = User.FindFirstValue(ClaimTypes.Email);

			var Orders = await _dataContext.Orders
				.Where(od => od.UserName == userEmail).OrderByDescending(od => od.Id).ToListAsync();
			ViewBag.UserEmail = userEmail;
			return View(Orders);
		}

		public async Task<IActionResult> CancelOrder(string ordercode)
		{
			if ((bool)!User.Identity?.IsAuthenticated)
			{
				// User is not logged in, redirect to login
				return RedirectToAction("Login", "Account");
			}
			try
			{
				var order = await _dataContext.Orders.Where(o => o.OrderCode == ordercode).FirstAsync();
				order.Status = 3;
				_dataContext.Update(order);
				await _dataContext.SaveChangesAsync();
			}
			catch (Exception ex)
			{

				return BadRequest("An error occurred while canceling the order.");
			}


			return RedirectToAction("History", "Account");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(LoginViewModel loginVM)
		{
			if (ModelState.IsValid)
			{
				Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(loginVM.Username, loginVM.Password, false, false);
				if (result.Succeeded)
				{
					//TempData["success"] = "Đăng nhập thành công";
					//var receiver = "demologin979@gmail.com";
					//var subject = "Đăng nhập trên thiết bị thành công.";
					//var message = "Đăng nhập thành công, trải nghiệm dịch vụ nhé.";

					//await _emailSender.SendEmailAsync(receiver, subject, message);
					return Redirect(loginVM.ReturnUrl ?? "/");
				}
				ModelState.AddModelError("", "Sai tài khoản hặc mật khẩu");
			}
			return View(loginVM);
		}


		public IActionResult Create()
		{
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(UserModel user)
		{
			if (ModelState.IsValid)
			{
				AppUserModel newUser = new AppUserModel { UserName = user.Username, Email = user.Email };
				IdentityResult result = await _userManage.CreateAsync(newUser, user.Password);
				if (result.Succeeded)
				{
					TempData["success"] = "Tạo thành viên thành công";
					return Redirect("/account/login");
				}
				foreach (IdentityError error in result.Errors)
				{
					ModelState.AddModelError("", error.Description);
				}
			}
			return View(user);
		}


		public async Task<IActionResult> Logout(string returnUrl = "/")
		{
			await _signInManager.SignOutAsync();
			await HttpContext.SignOutAsync();
			return Redirect(returnUrl);
		}
	}
}