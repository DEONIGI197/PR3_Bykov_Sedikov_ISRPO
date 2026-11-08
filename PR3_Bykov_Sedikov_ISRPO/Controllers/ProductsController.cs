using PR3_Bykov_Sedikov_ISRPO.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PR3_Bykov_Sedikov_ISRPO.Controllers;

public record StockData(int Stock);
public record SaleData(bool IsForSale);

[ApiController]
[Route("api/products")]
public class ProductsController(ShopDb db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var query = db.Products.Include(x => x.Sizes).AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Name.Contains(search.Trim()));
        var products = await query.OrderBy(x => x.Id).Select(x => new
        {
            x.Id, x.Name, x.Price, x.Description, x.PurchaseCount, x.Type, x.IsForSale,
            Sizes = x.Sizes.Select(s => new { s.Id, s.Letter, s.Number, s.Stock })
        }).ToListAsync();
        return Ok(products);
    }

    [Authorize(Roles = "Manager")]
    [HttpPatch("{productId:int}/sizes/{sizeId:int}/stock")]
    public async Task<IActionResult> UpdateStock(int productId, int sizeId, StockData data)
    {
        if (data.Stock < 0) return BadRequest("Остаток не может быть отрицательным.");
        var size = await db.ProductSizes.FirstOrDefaultAsync(x => x.Id == sizeId && x.ProductId == productId);
        if (size == null) return NotFound();
        size.Stock = data.Stock;
        await db.SaveChangesAsync();
        return Ok(new { size.Id, size.ProductId, size.Stock });
    }

    [Authorize(Roles = "Manager")]
    [HttpPatch("{productId:int}/sale")]
    public async Task<IActionResult> SetSale(int productId, SaleData data)
    {
        var product = await db.Products.FindAsync(productId);
        if (product == null) return NotFound();
        product.IsForSale = data.IsForSale;
        await db.SaveChangesAsync();
        return Ok(new { product.Id, product.IsForSale });
    }
}
