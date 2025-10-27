using Microsoft.AspNetCore.Identity;

namespace api.Model
{
    public class User : IdentityUser
    {
        public string? Initials { get; set; }
    }
}
