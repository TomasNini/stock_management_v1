using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StockManagement.Api.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsEnvironment("Testing")) {
    builder.WebHost.UseUrls("http://localhost:5011");
}

builder.Services.AddControllers().AddJsonOptions(options => {
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddDbContext<MainDbContext>(options => {
    options.UseSqlite(builder.Configuration.GetConnectionString("Main"));
});

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UsePathBase("/api");
app.UseRouting();
app.UseCors();
app.MapControllers();

using (var scope = app.Services.CreateScope()) {
    var dbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
    if (app.Environment.IsEnvironment("Testing")) {
        dbContext.Database.EnsureCreated();
    } else {
        dbContext.Database.Migrate();
        await SeedData.Apply(dbContext);
    }
}

app.Run();

public partial class Program;
