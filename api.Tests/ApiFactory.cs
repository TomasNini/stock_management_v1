using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockManagement.Api.Infrastructure.Data;

namespace StockManagement.Api.Tests;

/// <summary>
/// Hosts the API in memory for tests. Replaces the file SQLite database with a
/// shared in-memory connection so tests never touch stock_management.db or seed data.
/// Program.cs uses the Testing environment to skip UseUrls, Migrate, and SeedData.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program> {
    // SQLite :memory: is empty again if this connection closes. Keep it open for the host lifetime.
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services => {
            // Drop the production DbContext registration before adding the test one.
            var toRemove = services
                .Where(descriptor =>
                    descriptor.ServiceType == typeof(MainDbContext)
                    || descriptor.ServiceType == typeof(DbContextOptions<MainDbContext>))
                .ToList();

            foreach (var descriptor in toRemove) {
                services.Remove(descriptor);
            }

            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            services.AddDbContext<MainDbContext>(options => {
                options.UseSqlite(_connection);
            });
        });
    }

    /// <summary>
    /// Rebuilds an empty schema. Called before each test so cases do not share rows.
    /// </summary>
    public async Task ResetDatabase() {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _connection?.Dispose();
        }

        base.Dispose(disposing);
    }
}
