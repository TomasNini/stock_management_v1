namespace StockManagement.Api.Infrastructure.Data.Entities;

public class StockMovement {
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Delta { get; set; }
    public DateTime CreatedAt { get; set; }
}
