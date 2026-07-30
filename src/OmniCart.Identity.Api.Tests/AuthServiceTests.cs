using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Microsoft.Extensions.Configuration;
using OmniCart.Identity.Api.Data;
using OmniCart.Identity.Api.Services;
using OmniCart.Identity.Api.Dtos;
using OmniCart.Identity.Api.Entities;

namespace OmniCart.Identity.Api.Tests;

public class AuthServiceTests
{
    private OmniCartIdentityDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<OmniCartIdentityDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var db = new OmniCartIdentityDbContext(options);
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        return db;
    }

    private JwtService CreateJwtServiceWithTestKey()
    {
        var config = new ConfigurationManager();
        // Deterministic test key (32+ chars)
        config["Jwt:Key"] = "test_jwt_key_1234567890_test_key_32";
        config["Jwt:Issuer"] = "OmniCart.Identity";
        config["Jwt:Audience"] = "OmniCart.Clients";
        return new JwtService(config);
    }

    [Fact]
    public async Task RegisterAsync_CreatesUserAndReturnsToken()
    {
        // Arrange
        var db = CreateInMemoryDb("AuthServiceTestsDb_Register");
        db.Roles.Add(new Role { Name = "Admin" });
        db.Roles.Add(new Role { Name = "Customer" });
        db.SaveChanges();

        var jwtService = CreateJwtServiceWithTestKey();
        var authService = new AuthService(db, jwtService);

        var request = new RegisterRequest { Username = "testuser", Password = "Password1!" };

        // Act
        var result = await authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal(1, await db.Users.CountAsync());
    }

    [Fact]
    public async Task RegisterAsync_WithExistingUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var db = CreateInMemoryDb("AuthServiceTestsDb_Duplicate");
        db.Roles.Add(new Role { Name = "Customer" });
        db.SaveChanges();

        var existingUser = new User
        {
            Username = "existingUser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"),
            RoleId = db.Roles.First().Id
        };
        db.Users.Add(existingUser);
        db.SaveChanges();

        var jwtService = CreateJwtServiceWithTestKey();
        var authService = new AuthService(db, jwtService);

        var request = new RegisterRequest { Username = "existingUser", Password = "Password1!" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await authService.RegisterAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var db = CreateInMemoryDb("AuthServiceTestsDb_LoginValid");
        var customerRole = new Role { Name = "Customer" };
        db.Roles.Add(customerRole);
        db.SaveChanges();

        var user = new User
        {
            Username = "existing",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"),
            RoleId = customerRole.Id
        };
        db.Users.Add(user);
        db.SaveChanges();

        var jwtService = CreateJwtServiceWithTestKey();
        var authService = new AuthService(db, jwtService);

        var request = new LoginRequest { Username = "existing", Password = "Password1!" };

        // Act
        var result = await authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsNull()
    {
        // Arrange
        var db = CreateInMemoryDb("AuthServiceTestsDb_LoginInvalid");
        var customerRole = new Role { Name = "Customer" };
        db.Roles.Add(customerRole);
        db.SaveChanges();

        var user = new User
        {
            Username = "existing2",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword1!"),
            RoleId = customerRole.Id
        };
        db.Users.Add(user);
        db.SaveChanges();

        var jwtService = CreateJwtServiceWithTestKey();
        var authService = new AuthService(db, jwtService);

        var request = new LoginRequest { Username = "existing2", Password = "WrongPassword" };

        // Act
        var result = await authService.LoginAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        var db = CreateInMemoryDb("AuthServiceTestsDb_LoginNonexistent");
        var jwtService = CreateJwtServiceWithTestKey();
        var authService = new AuthService(db, jwtService);

        var request = new LoginRequest { Username = "noone", Password = "DoesntMatter" };

        // Act
        var result = await authService.LoginAsync(request);

        // Assert
        Assert.Null(result);
    }
}
