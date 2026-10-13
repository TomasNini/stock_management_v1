namespace StockManagement.Api.Infrastructure.Data.Entities;

public class Product {
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Sku { get; set; }
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public int MinQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
