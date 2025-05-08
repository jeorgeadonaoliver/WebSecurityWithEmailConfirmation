using Microsoft.AspNetCore.Identity;
using WebApplication_SecurityApi.Model;

namespace WebApplication_SecurityApi.Service.Email
{
    public class ExpiredUserCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ExpiredUserCleanupService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUserModel>>();
                    var users = userManager.Users
                        .Where(u => !u.EmailConfirmed && u.CreatedAt < DateTime.UtcNow.AddMinutes(-10))
                        .ToList();

                    foreach (var user in users)
                    {
                        await userManager.DeleteAsync(user); // This should now work
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

    }
}
