namespace ClothingStoreApi.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string Description { get; set; } = "";
    public int PurchaseCount { get; set; }
    public string Type { get; set; } = "";
    public bool IsForSale { get; set; } = true;
    public List<ProductSize> Sizes { get; set; } = [];
}

public class ProductSize
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Letter { get; set; } = "";
    public int Number { get; set; }
    public int Stock { get; set; }
}
