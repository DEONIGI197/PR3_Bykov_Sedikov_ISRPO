using ClothingStoreApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothingStoreApi.Data;

public class StoreDbContext(DbContextOptions<StoreDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductSize> ProductSizes => Set<ProductSize>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(x => x.Login).IsUnique();
        modelBuilder.Entity<ProductSize>().HasIndex(x => new { x.ProductId, x.Letter }).IsUnique();
        modelBuilder.Entity<OrderItem>().HasOne(x => x.ProductSize).WithMany().OnDelete(DeleteBehavior.Restrict);
    }
}
