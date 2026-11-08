using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClothingStoreApi.Data;
using ClothingStoreApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ClothingStoreApi.Controllers;

public record RegisterRequest(string Login, string Password, string Phone, string Address);
public record LoginRequest(string Login, string Password);

[ApiController]
[Route("api/auth")]
public class AuthController(StoreDbContext db, IPasswordHasher<User> hasher, IConfiguration config) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Phone)
            || string.IsNullOrWhiteSpace(request.Address) || string.IsNullOrWhiteSpace(request.Password)
            || request.Password.Length < 6)
            return BadRequest("Заполните все поля. Пароль должен содержать не менее 6 символов.");
        if (await db.Users.AnyAsync(x => x.Login == request.Login))
            return Conflict("Такой логин уже занят.");

        var user = new User
        {
            Login = request.Login.Trim(),
            Phone = request.Phone.Trim(),
            Address = request.Address.Trim()
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Created($"/api/users/{user.Id}", new { user.Id, user.Login, user.Role });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Login == request.Login);
        if (user == null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
            == PasswordVerificationResult.Failed)
            return Unauthorized("Неверный логин или пароль.");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
            SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);
        var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"],
            claims, expires: expires, signingCredentials: credentials);
        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token), expires, user.Role });
    }
}
