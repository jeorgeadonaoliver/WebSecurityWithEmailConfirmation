namespace WebApplication_SecurityApi.Model
{
    //STEP 5: Create register user model to start register user of the application
    //depending on requirements, you can modify it. this is just the basics
    public class RegisterUserModel
    {
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; }  = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
