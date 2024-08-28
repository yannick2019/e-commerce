using Microsoft.AspNetCore.Identity;

namespace YanikoRestaurant.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<Order>? Orders { get; set; }
    }
}