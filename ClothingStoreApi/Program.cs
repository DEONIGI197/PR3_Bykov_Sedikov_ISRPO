using System.Text;
using System.Security.Cryptography;
using ClothingStoreApi.Data;
using ClothingStoreApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var key = builder.Configuration["Jwt:Key"] ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
builder.Configuration["Jwt:Key"] = key;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<StoreDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Store")));
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
    db.Database.EnsureCreated();

    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new Product { Name = "Белая футболка", Price = 990, Description = "Хлопковая футболка", Type = "Футболка", Sizes = [new() { Letter = "S", Number = 44, Stock = 10 }, new() { Letter = "M", Number = 46, Stock = 8 }, new() { Letter = "L", Number = 48, Stock = 6 }] },
            new Product { Name = "Черная футболка", Price = 1190, Description = "Футболка свободного кроя", Type = "Футболка", Sizes = [new() { Letter = "M", Number = 46, Stock = 7 }, new() { Letter = "XL", Number = 50, Stock = 4 }] });
        db.SaveChanges();
    }

    var managerLogin = builder.Configuration["Manager:Login"];
    var managerPassword = builder.Configuration["Manager:Password"];
    if (!string.IsNullOrWhiteSpace(managerLogin) && !string.IsNullOrWhiteSpace(managerPassword)
        && !db.Users.Any(x => x.Login == managerLogin))
    {
        var manager = new User { Login = managerLogin, Phone = "", Address = "", Role = "Manager" };
        manager.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>()
            .HashPassword(manager, managerPassword);
        db.Users.Add(manager);
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
