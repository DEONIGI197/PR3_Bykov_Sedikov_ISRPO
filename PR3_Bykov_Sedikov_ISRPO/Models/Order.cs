namespace PR3_Bykov_Sedikov_ISRPO.Models;

public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "New";
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public List<OrderItem> Items { get; set; } = [];
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int ProductSizeId { get; set; }
    public ProductSize ProductSize { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
