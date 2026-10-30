using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StoreApi.Data;

namespace StoreApi.Tests;

public sealed class StoreApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=unused",
                ["Jwt:Key"] = "integration-test-key-that-is-at-least-32-bytes",
                ["Jwt:Issuer"] = "StoreApi.Tests",
                ["Jwt:Audience"] = "StoreApi.Tests"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<StoreDbContext>>();
            services.RemoveAll<DbContextOptions<StoreDbContext>>();
            services.RemoveAll<StoreDbContext>();
            services.AddDbContext<StoreDbContext>(options =>
                options.UseInMemoryDatabase($"StoreApiTests-{Guid.NewGuid()}"));
        });
    }
}
