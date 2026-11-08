using System.Security.Claims;
using PR3_Bykov_Sedikov_ISRPO.Data;
using PR3_Bykov_Sedikov_ISRPO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PR3_Bykov_Sedikov_ISRPO.Controllers;

public record ItemData(int ProductId, int SizeId, int Quantity);
public record OrderData(List<ItemData> Items);
public record StatusData(string Status);

[ApiController]
[Authorize]
[Route("api/orders")]
public class OrdersController(ShopDb db) : ControllerBase
{
    private static readonly string[] Statuses = ["New", "Processing", "Shipped", "Completed", "Cancelled"];

    [HttpPost]
    public async Task<IActionResult> Create(OrderData data)
    {
        if (data.Items == null || data.Items.Count == 0 || data.Items.Any(x => x.Quantity <= 0))
            return BadRequest("Укажите товары и положительное количество.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        var order = new Order { UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) };
        foreach (var item in data.Items)
        {
            var product = await db.Products.FindAsync(item.ProductId);
            var size = await db.ProductSizes.FirstOrDefaultAsync(x => x.Id == item.SizeId && x.ProductId == item.ProductId);
            if (product == null || size == null || !product.IsForSale)
                return BadRequest("Товар или размер не найден либо товар снят с продажи.");
            if (size.Stock < item.Quantity)
                return BadRequest($"Недостаточно товара размера {size.Letter}.");

            size.Stock -= item.Quantity;
            product.PurchaseCount += item.Quantity;
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductSizeId = size.Id,
                Quantity = item.Quantity,
                Price = product.Price
            });
        }
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Created($"/api/orders/{order.Id}", new { order.Id, order.CreatedAt, order.Status });
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? status, [FromQuery] int? buyerId)
    {
        var query = db.Orders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (buyerId.HasValue) query = query.Where(x => x.UserId == buyerId.Value);
        var orders = await query.OrderByDescending(x => x.CreatedAt).Select(x => new
        {
            x.Id, x.CreatedAt, x.Status, BuyerId = x.UserId, BuyerLogin = x.User.Login,
            Items = x.Items.Select(i => new
            {
                i.ProductId, ProductName = i.Product.Name, i.ProductSizeId,
                Size = i.ProductSize.Letter, i.Quantity, i.Price
            })
        }).ToListAsync();
        return Ok(orders);
    }

    [Authorize(Roles = "Manager")]
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, StatusData data)
    {
        if (!Statuses.Contains(data.Status)) return BadRequest($"Статусы: {string.Join(", ", Statuses)}");
        var order = await db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        order.Status = data.Status;
        await db.SaveChangesAsync();
        return Ok(new { order.Id, order.Status });
    }
}
