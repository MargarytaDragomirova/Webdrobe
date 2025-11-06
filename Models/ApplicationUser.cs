using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models
{
    public class ApplicationUser : IdentityUser
    {
        // You can add extra profile fields if you want
        public string? FullName { get; set; }
    }
}
