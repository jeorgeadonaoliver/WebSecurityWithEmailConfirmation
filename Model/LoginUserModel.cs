namespace WebApplication_SecurityApi.Model
{
    //STEP 5: Create login user model to start authenticating user
    //before they can access the application/Endpoints
    public class LoginUserModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
