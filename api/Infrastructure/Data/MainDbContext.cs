using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StockManagement.Api.Infrastructure.Data.Entities;

namespace StockManagement.Api.Infrastructure.Data;

public class MainDbContext(DbContextOptions<MainDbContext> options) : DbContext(options) {
    private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter = new(
        value => value,
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    public DbSet<Product> Products => Set<Product>();
    public DbSet<AppSetting> Settings => Set<AppSetting>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<AppSetting>(entity => {
            entity.HasKey(x => x.Setting);
            entity.Property(x => x.Setting).HasMaxLength(64);
            entity.Property(x => x.Value).IsRequired().HasMaxLength(200);
            entity.ToTable("Settings");
        });

        modelBuilder.Entity<Product>(entity => {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Sku).IsRequired().HasMaxLength(64);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.CreatedAt).HasConversion(UtcDateTimeConverter);
            entity.Property(x => x.UpdatedAt).HasConversion(UtcDateTimeConverter);
        });

        modelBuilder.Entity<StockMovement>(entity => {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.CreatedAt).HasConversion(UtcDateTimeConverter);
            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => new { x.ProductId, x.CreatedAt });
        });
    }
}
