using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using OmniCart.Identity.Api.Data;
using OmniCart.Identity.Api.Dtos;
using OmniCart.Identity.Api.Entities;

namespace OmniCart.Identity.Api.Services;

public class AuthService : IAuthService
{
    private readonly OmniCartIdentityDbContext _db;
    private readonly IJwtService _jwt;

    public AuthService(OmniCartIdentityDbContext db, IJwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (existing != null)
            throw new InvalidOperationException("User already exists");

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = 1
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new AuthResponse { Token = _jwt.GenerateToken(user) };
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null) return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) return null;

        return new AuthResponse { Token = _jwt.GenerateToken(user) };
    }
}
