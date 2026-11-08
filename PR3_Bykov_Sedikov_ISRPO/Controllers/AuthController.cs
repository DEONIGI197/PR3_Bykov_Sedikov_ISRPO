using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using PR3_Bykov_Sedikov_ISRPO.Data;
using PR3_Bykov_Sedikov_ISRPO.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace PR3_Bykov_Sedikov_ISRPO.Controllers;

public record RegisterData(string Login, string Password, string Phone, string Address);
public record LoginData(string Login, string Password);

[ApiController]
[Route("api/auth")]
public class AuthController(ShopDb db, IPasswordHasher<User> hasher, IConfiguration config) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterData data)
    {
        if (string.IsNullOrWhiteSpace(data.Login) || string.IsNullOrWhiteSpace(data.Phone)
            || string.IsNullOrWhiteSpace(data.Address) || string.IsNullOrWhiteSpace(data.Password)
            || data.Password.Length < 6)
            return BadRequest("Заполните все поля. Пароль должен содержать не менее 6 символов.");
        var login = data.Login.Trim();
        if (await db.Users.AnyAsync(x => x.Login == login))
            return Conflict("Такой логин уже занят.");

        var user = new User
        {
            Login = login,
            Phone = data.Phone.Trim(),
            Address = data.Address.Trim()
        };
        user.PasswordHash = hasher.HashPassword(user, data.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return StatusCode(201, new { user.Id, user.Login, user.Role });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginData data)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Login == data.Login);
        if (user == null || hasher.VerifyHashedPassword(user, user.PasswordHash, data.Password)
            == PasswordVerificationResult.Failed)
            return Unauthorized("Неверный логин или пароль.");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var sign = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
            SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);
        var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"],
            claims, expires: expires, signingCredentials: sign);
        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token), expires, user.Role });
    }
}
