using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using shopping_tutorial.Areas.Admin.Repository;
using shopping_tutorial.Models;
using shopping_tutorial.Models.ViewModels;
using shopping_tutorial.Repository;
using System.Security.Claims;

namespace shopping_tutorial.Controllers
{
    public class AccountController : Controller
    {
        private UserManager<AppUserModel> _userManage; // quản lí user 
        private SignInManager<AppUserModel> _signInManager; //quản lí đăng nhập 
        private readonly IEmailSender _emailSender;
        //public AccountController(IEmailSender emailSender, SignInManager<AppUserModel> signInManager, UserManager<AppUserModel> userManage)
        //{
        //    _signInManager = signInManager;
        //    _userManage = userManage;
        //    _emailSender = emailSender;
        //}
        public AccountController(SignInManager<AppUserModel> signInManager, UserManager<AppUserModel> userManage)
        {
            _signInManager = signInManager;
            _userManage = userManage;

        }

        public IActionResult Login(string returnUrl)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl});
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel loginVM)
        {
            if (ModelState.IsValid)
            {
                Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(loginVM.Username, loginVM.Password, false, false);
                if (result.Succeeded)
                {
                    TempData["success"] = "Đăng nhập thành công ";
                    //////Send email 
                    //var receive = "bangy07102004@gmail.com";
                    //var subject = "Đăng nhập trên thiết bị thành công";
                    //var message = "Đăng nhập thành công, trải nhiệm dịch vụ nhé";
                    //await _emailSender.SendEmailAsync(receive, subject, message);
                    return Redirect(loginVM.ReturnUrl ?? "/");
                }
                ModelState.AddModelError("", "sai tên tài khoản hoặc mật khẩu ");
            }
            return View(loginVM);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(UserModel user)
        {
            if (ModelState.IsValid)
            {
                AppUserModel newUser = new AppUserModel { UserName = user.Username, Email = user.Email };
                IdentityResult result = await _userManage.CreateAsync(newUser, user.Password);// mã hóa passwrod 
                if (result.Succeeded)
                {
                    // Assign the "Admin" role to the new user
                    var roleAssignResult = await _userManage.AddToRoleAsync(newUser, "Admin");

                    if (!roleAssignResult.Succeeded)
                    {
                        // If role assignment fails, add errors to the ModelState
                        foreach (IdentityError error in roleAssignResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        return View(user);
                    }

                    TempData["success"] = "Tạo khách hàng thành công và đã gán quyền Admin.";

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
            await _signInManager.SignOutAsync();//thoát khỏi 
            return Redirect(returnUrl);
        }
    }
}
