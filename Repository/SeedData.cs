using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using shopping_tutorial.Models;

namespace shopping_tutorial.Repository
{
    public class SeedData
    {
        public static void seedingData(DataContext _context)
        {
            _context.Database.Migrate();
            if(!_context.Products.Any())
            {
                CategoryModel ao = new CategoryModel { Name = "Ao", Slug = "ao", Description = "Cac kieu ao  don gian", Status = 1 };
                CategoryModel quan = new CategoryModel { Name = "Quan", Slug = "quan", Description = "Cac kieu quan dai hot trend", Status = 1 };
                BrandModel routine = new BrandModel { Name = "routine", Slug = "routine", Description = "make it simple but significant", Status = 1 };
                BrandModel mrsimple = new BrandModel { Name = "mrsimple", Slug = "mrsimple", Description = "thoi trang  hot trend", Status = 1 };
                _context.Products.AddRange(
                    new ProductModel { Name = "ao thun trang tron", Slug = "ao thun", Description = "ao thun trang don gian", Image = "1.jpg", Category = ao, Price = 12, Brand = routine },
                    new ProductModel { Name = "quan jean xanh", Slug = "quan jean", Description = "quan jean xanh don gian", Image = "1.jpg", Category = quan, Price = 12, Brand = mrsimple }
                );
                _context.SaveChanges();
            }
            if (!_context.Users.Any())
            {
                // Create a new user
                var user = new AppUserModel
                {
                    UserName = "admin",
                    Email = "admin@gmail.com",
                    EmailConfirmed = true, // Skip email confirmation
                    NormalizedUserName = "ADMIN",
                    NormalizedEmail = "ADMIN@GMAIL.COM",

                    PasswordHash = new PasswordHasher<AppUserModel>().HashPassword(null, "Ta!123"), // Securely hash the password
                    SecurityStamp = Guid.NewGuid().ToString(), // Generate a unique security stamp
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                // Add user to the context
                _context.Users.Add(user);
                _context.SaveChanges();
                // Ensure the role exists in the database
                var roleName = "Admin";
                var role = _context.Roles.FirstOrDefault(r => r.Name == roleName);
                if (role == null)
                {
                    role = new IdentityRole
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpper()
                    };
                    _context.Roles.Add(role);
                    _context.SaveChanges();
                }
                // Assign the user to the role
                var userRole = new IdentityUserRole<string>
                {
                    UserId = user.Id,
                    RoleId = role.Id
                };
                _context.UserRoles.Add(userRole);
                // Save changes to the database
                _context.SaveChanges();

                Console.WriteLine("User and role assignment seeded successfully!");
            }
            else
            {
                Console.WriteLine("Users already exist in the database.");
            }
        }
    }
}



