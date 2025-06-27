using Microsoft.AspNetCore.Identity;

namespace shopping_tutorial.Models
{
    public class AppUserModel:IdentityUser
    {
        public string Ocucpation { get; set; }
        public string RoleId { get; set; }
    }
}
