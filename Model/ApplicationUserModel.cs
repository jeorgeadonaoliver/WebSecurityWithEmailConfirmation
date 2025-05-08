using Microsoft.AspNetCore.Identity;

namespace WebApplication_SecurityApi.Model
{
    //STEP 5: Create custom Class that represent User of this application
    //IdentityUser class in auto generate by Identity package. Must be inherited
    //by custom class to register it in the configuration
    public class ApplicationUserModel : IdentityUser
    {
        //This is IMPORTANT to track if account confirmation is still valid.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //This is IMPORTANT for 2FA in generating OTP
        public string? TwoFactorCode { get; set; }
        //This is IMPORTANT for OTP Expiration
        public DateTime? TwoFactorExpiry { get; set; }
    }
}
