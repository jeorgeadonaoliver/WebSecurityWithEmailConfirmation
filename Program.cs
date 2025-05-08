using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WebApplication_SecurityApi.Data;
using WebApplication_SecurityApi.Model;
using WebApplication_SecurityApi.Service.Email;

var builder = WebApplication.CreateBuilder(args);


//STEP 2: Set up configuration of ApplicationDbContext
//Register and Write Connection string in appsettings.json
builder.Services.AddDbContext<ApplicationDbContext>( options => 
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});

//SETP 2: Set up Swagger to accept Bearer Tokens. This is the simplest 
//Configuration i can find in the internet
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {your_token}' in the Authorization header."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});
//End of STEP 2


//STEP 4: Register AddIdentity to set up application to use Identity
//Object IdentityUser,IdentityRole came from package(built-in)
builder.Services.AddIdentity<ApplicationUserModel, IdentityRole>(options => {

    //configure behavior of user credentials based on requirements
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

    //This set up is for 2FA(Two-Factor Authentication using MS Identity
    options.SignIn.RequireConfirmedAccount = true;
    options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

//STEP 6: Configure your Token creadentials like Issuer, Audience and Secret Key.
//Must be Credentials MUST BE configure in appsettings.json for best practices
builder.Services.AddAuthentication(option => 
{
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"])
            )
        };
    });
builder.Services.AddAuthorization();
//End of Step 6.

//STEP 8: Add Email confirmation to confirm registration
//This additional features but not required
//Email setting is in appsettings.json
builder.Services.Configure<IdentityOptions>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
});
//Expiration of token for email verification
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromMinutes(5);
});
//Register Background Cleanup Service for expired email confirmation
builder.Services.AddHostedService<ExpiredUserCleanupService>();





builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    //STEP 2: add swagger in pipeline
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1");
        c.RoutePrefix = string.Empty;
    });
    //End step 2
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//STEP 3: Above app.UseAuthorization(), always put app.UseAuthentication();
//Dont forget to run Add-Migration on Package Manager Console
app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.Run();
