using System.Security.Claims;
using ClothingStoreApi.Data;
using ClothingStoreApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClothingStoreApi.Controllers;

public record OrderLineRequest(int ProductId, int SizeId, int Quantity);
public record CreateOrderRequest(List<OrderLineRequest> Items);
public record StatusRequest(string Status);

[ApiController]
[Authorize]
[Route("api/orders")]
public class OrdersController(StoreDbContext db) : ControllerBase
{
    private static readonly string[] Statuses = ["New", "Processing", "Shipped", "Completed", "Cancelled"];

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request)
    {
        if (request.Items == null || request.Items.Count == 0 || request.Items.Any(x => x.Quantity <= 0))
            return BadRequest("Укажите товары и положительное количество.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        var order = new Order { UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) };
        foreach (var line in request.Items)
        {
            var product = await db.Products.FindAsync(line.ProductId);
            var size = await db.ProductSizes.FirstOrDefaultAsync(x => x.Id == line.SizeId && x.ProductId == line.ProductId);
            if (product == null || size == null || !product.IsForSale)
                return BadRequest("Товар или размер не найден либо товар снят с продажи.");
            if (size.Stock < line.Quantity)
                return BadRequest($"Недостаточно товара размера {size.Letter}.");

            size.Stock -= line.Quantity;
            product.PurchaseCount += line.Quantity;
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductSizeId = size.Id,
                Quantity = line.Quantity,
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
    public async Task<IActionResult> UpdateStatus(int id, StatusRequest request)
    {
        if (!Statuses.Contains(request.Status)) return BadRequest($"Статусы: {string.Join(", ", Statuses)}");
        var order = await db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        order.Status = request.Status;
        await db.SaveChangesAsync();
        return Ok(new { order.Id, order.Status });
    }
}
