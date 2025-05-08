using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApplication_SecurityApi.Model;
using WebApplication_SecurityApi.Service.Email;

namespace WebApplication_SecurityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        //STEP 7: Inject UserManager Class for registration of new user.
        //This is the simplest way of implementation of user registration
        private readonly UserManager<ApplicationUserModel> _userManager;

        //Inject this IConfiguration to access data from appsettings.json
        //Best practice not to expose sensitive data here
        private readonly IConfiguration _config;

        //Inject this RoleManager class to add roles for newly registered user
        //depends on the requirements, you can modify how you implement it
        private readonly RoleManager<IdentityRole> _roleManager;
        public AuthController(UserManager<ApplicationUserModel> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration config)
        {
            _userManager = userManager; 
            _roleManager = roleManager;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserModel model)
        {
            var user = new ApplicationUserModel { UserName = model.Email, Email = model.Email };

            //adding new user in the application
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Ensure the role exists before assigning
                var roleExists = await _roleManager.RoleExistsAsync(model.Role);
                if (!roleExists)
                {
                    //adding new role if not exist
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));
                }

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                var confirmationLink = Url.Action("ConfirmEmail", "Auth", new { userId = user.Id, token }, Request.Scheme);
                var emailSender = new EmailSender(_config);
                await emailSender.SendEmailAsync(user.Email, "Confirm Your Email", $"Click <a href='{confirmationLink}'>here</a> to confirm.");

                // Assign role to the new user
                await _userManager.AddToRoleAsync(user, model.Role);

                return Ok(new { Message = "User registered successfully with role: " + model.Role });
            }

            return BadRequest(result.Errors);
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return BadRequest("Invalid user.");

            var result = await _userManager.ConfirmEmailAsync(user, token);

            return result.Succeeded ? Ok("Email confirmed successfully.") : BadRequest("Email confirmation failed.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                if (user.TwoFactorEnabled)
                {
                    // Generate OTP
                    var otp = new Random().Next(100000, 999999).ToString();
                    user.TwoFactorCode = otp;
                    user.TwoFactorExpiry = DateTime.UtcNow.AddMinutes(5);
                    await _userManager.UpdateAsync(user);

                    var emailSender = new EmailSender(_config);
                    // Send email
                    await emailSender.SendEmailAsync(user.Email?? "", "Your OTP Code", $"Your code is: {otp}");

                    return Ok(new { Requires2FA = true, UserId = user.Id });
                }

                return Ok(new { Token = GenerateJwt(user) });
            }
            return Unauthorized();
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null || user.TwoFactorCode != model.Code || user.TwoFactorExpiry < DateTime.UtcNow)
                return Unauthorized("Invalid or expired OTP");

            // Clear OTP fields after successful use
            user.TwoFactorCode = null;
            user.TwoFactorExpiry = null;
            await _userManager.UpdateAsync(user);

            return Ok(new { Token = GenerateJwt(user) });
        }

        [Authorize]
        [HttpGet("secure-data")]
        public IActionResult GetSecureData()
        {
            return Ok(new { Data = "This is a protected API." });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("AdminDashboard")]
        public IActionResult AdminDashboard()
        {
            return Ok(new { Message = "Welcome Admin!" });
        }

        private string GenerateJwt(IdentityUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"] ?? "");

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                        new Claim(ClaimTypes.Name, user.UserName?? ""),
                        new Claim(ClaimTypes.Role, "Admin") // Assign roles dynamically if needed
                    }),

                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
