using Fcg.Payment.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Payment.API.Setup;

public static class DbContextConfiguration
{
    public static void AddDbContextConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PaymentDbContext>(options =>
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Could find connection string, database will not be configured");
            }

            options.UseSqlServer(connectionString);
        }, ServiceLifetime.Scoped);
    }
}