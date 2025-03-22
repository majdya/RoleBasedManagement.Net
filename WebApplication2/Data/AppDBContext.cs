
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace RoleBasedManagement.Data
{
    public class AppDBContext : IdentityDbContext<IdentityUser>
    {
    }
}
