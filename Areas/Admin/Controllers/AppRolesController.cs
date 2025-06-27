using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace shopping_tutorial.Areas.Admin.Controllers
{

    [Area("Admin")]
    [Route("Admin/AppRoles")]
    [Authorize(Roles = "Admin, Publisher")]
    public class AppRolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        public AppRolesController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }
        //list all the roles created by Users
     
        [Route("Index")]
        public IActionResult Index()
        {
            var roles = _roleManager.Roles;
            return View(roles);
        }
        [HttpGet]
        [Route("Create")]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> Create(IdentityRole model)
        {
            //avoid duplicate role
            if (!_roleManager.RoleExistsAsync(model.Name).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(model.Name)).GetAwaiter().GetResult();
            }
            return Redirect("Index");
        }
        [HttpGet]
        [Route("Edit")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var role = await _roleManager.FindByIdAsync(id);
            return View(role);
        }

        [HttpPost]
        [Route("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, IdentityRole model)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound(); // handle missing id 
            }
            if (ModelState.IsValid)// validate model state before proceeding{
            {
                var role = await _roleManager.FindByIdAsync(id);
                if (role == null)
                {
                    return NotFound();
                }
                role.Name = model.Name; // update role properties with model data
                try
                {
                    await _roleManager.UpdateAsync(role);
                    TempData["success"] = "Role updated successfully";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while updating the role.");
                }
            }
            return View(model ?? new IdentityRole { Id = id });
        }
        [HttpGet]
        [Route("Delete")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var role = await _roleManager.FindByIdAsync(id);
            return View(role);
        }
    }
}
