using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Models;
using shopping_tutorial.Repository;
using System.Security.Claims;

namespace shopping_tutorial.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/User")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<AppUserModel> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly DataContext _dataContext;
        public UserController(UserManager<AppUserModel> userManager  ,RoleManager<IdentityRole> roleManager, DataContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _dataContext = context;
        }
        [HttpGet]
        [Route("Index")]
        public async Task<IActionResult> Index() // có await thì phải có async 
        {// sử dụng 3 bảng AppNetUserRoles, AppNetUsers, AppNetRoles dựa vào datacontext 
            var usersWithRoles = await (from u in _dataContext.Users
                                        join ur in _dataContext.UserRoles on u.Id equals ur.UserId
                                        join r in _dataContext.Roles on ur.RoleId equals r.Id
                                        select new { User = u, RoleName = r.Name }).ToListAsync();

            var loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);//get the logged-in user's ID
            ViewBag.loggedInUserId = loggedInUserId;
            return View(usersWithRoles);
        }
        //public async Task<IActionResult> Index() // có await thì phải có async 
        //{// sử dụng 3 bảng AppNetUserRoles, AppNetUsers, AppNetRoles dựa vào datacontext 
        //    var usersWithRoles = await (from u in _dataContext.Users
        //                                join ur in _dataContext.UserRoles on u.Id equals ur.UserId
        //    into UserRoles
        //    from ur in UserRoles.DefaultIfEmpty()
        //    Include users without roles
        //                                join r in _dataContext.Roles on ur.RoleId equals r.Id
        //                                into userRolesWithRoles
        //                                from urr in userRolesWithRoles.DefaultIfEmpty()
        //                                select new UserWithRoleName { User = u, RoleName = urr.Name }).ToListAsync();
        //    var loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);//get the logged-in user's ID
        //    ViewBag.loggedInUserId = loggedInUserId;
        //    return View(usersWithRoles);


        [HttpGet]
        [Route("Create")]
        public async Task <IActionResult> Create()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");
            return View(new AppUserModel()); // đẩy model qua view 
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Create")]
        public async Task<IActionResult> Create(AppUserModel user)
        {
            if (ModelState.IsValid)
            {
                var createUserRusult = await _userManager.CreateAsync(user, user.PasswordHash);
                if (createUserRusult.Succeeded)
                {
                    var createUser = await _userManager.FindByEmailAsync(user.Email);//tìm user dựa vào email 
                    var userId = createUser.Id; //lấy user id 
                    var role = _roleManager.FindByIdAsync(user.RoleId); // lấy RoleId 
                    //gán quyền 
                    var addToRoleResult = await _userManager.AddToRoleAsync(createUser,role.Result.Name);// lấy role dựa vào name và chỉ gán 1 quyền do AddToRoleAsync 
                    if (!addToRoleResult.Succeeded)
                    {
                        foreach (var error in createUserRusult.Errors) //lấy lỗi dựa trên identityresult 
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                    return RedirectToAction("Index", "User");
                }
                else
                {
                    foreach (var error in createUserRusult.Errors) //lấy lỗi dựa trên identityresult 
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(user);
                }

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
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");
            return View(user); // đẩy model qua view 
        }
        [HttpGet]
        [Route("Edit")]
        public async Task<IActionResult> Edit(string id)
        {
            // kiểm tra xem id có tồn tại ko 
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");
            return View(user);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Edit")]
        public async Task<IActionResult> Delete(string id, AppUserModel user)
        {
            var existingUser = await _userManager.FindByIdAsync(id); //lấy ra user dựa vào id 
            if(existingUser == null)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                // update other user properties(excluding password)
                existingUser.UserName = user.UserName;
                existingUser.Email = user.Email;
                existingUser.PhoneNumber = user.PhoneNumber;
                existingUser.RoleId = user.RoleId;

                var updateUserResult = await _userManager.UpdateAsync(existingUser);//thực hiện update 
                if (updateUserResult.Succeeded)
                {
                    return RedirectToAction("Index", "User");
                }
                else
                {
                    AddIdentityErrors(updateUserResult);
                    return View(existingUser);
                }
            }
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");

            //Model validation failed
            TempData["error"] = "Model validation failed";
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
            string errorMessage = string.Join("\n", errors);
            return View(existingUser);
        }
        private void AddIdentityErrors(IdentityResult identityResult)
        {
            foreach(var error in identityResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        [HttpGet]
        [Route("Delete")]
        public async Task<IActionResult> Delete(string id)// Vì id user kiểu string 
        {
            if (string.IsNullOrEmpty(id)){
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(id);
            if(user == null)
            {
                return NotFound();
            }
            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                return View("Error");
            }
            TempData["success"] = "Người dùng đã được xóa thành công";
            return RedirectToAction("Index");
        }

    }
}

