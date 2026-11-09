using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests.TestHelpers
{
    /// <summary>
    /// Boots the real RaceDay.Api application (full middleware pipeline: routing,
    /// authentication, authorization) but replaces the SQL Server DbContext with
    /// an EF Core InMemory provider, using a fresh, uniquely named database per
    /// factory instance so tests never interfere with each other.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"RaceDayTestDb_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<RaceDayContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<RaceDayContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });

                // Never talk to Azure from tests - swap in the fake storage service.
                var blobDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBlobStorageService));
                if (blobDescriptor != null)
                {
                    services.Remove(blobDescriptor);
                }
                services.AddSingleton<IBlobStorageService, FakeBlobStorageService>();
            });
        }
    }
}
