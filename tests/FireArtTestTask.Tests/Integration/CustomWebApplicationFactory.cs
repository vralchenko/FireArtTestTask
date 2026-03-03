using FireArtTestTask.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FireArtTestTask.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add InMemory database (name captured once, shared across all scopes)
            var dbName = "TestDb_" + Guid.NewGuid();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
        });

        builder.UseEnvironment("Development");
    }
}
