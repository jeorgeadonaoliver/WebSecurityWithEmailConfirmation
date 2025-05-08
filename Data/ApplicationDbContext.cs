using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication_SecurityApi.Model;

namespace WebApplication_SecurityApi.Data
{
    //STEP 1: Set up DbContext using IdentityDbContext
    //DO NOT FORGET to include ApplicationUserModel to register as IdentityDbContext
    public class ApplicationDbContext : IdentityDbContext<ApplicationUserModel>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            
        }
    }
}
