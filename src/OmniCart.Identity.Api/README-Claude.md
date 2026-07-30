# OmniCart.Identity.Api — Comprehensive Documentation (README-Claude.md)

## Table of contents

1. [Architecture Overview](#architecture-overview)
2. [Project Structure](#project-structure)
3. [File-by-file Breakdown](#file-by-file-breakdown)
4. [Configuration & Secrets](#configuration--secrets)
5. [Dependencies & Packages](#dependencies--packages)
6. [JWT Implementation Deep Dive](#jwt-implementation-deep-dive)
7. [ASP.NET Core Identity vs Custom JWT](#aspnet-core-identity-vs-custom-jwt)
8. [Database Schema](#database-schema)
9. [API Endpoints](#api-endpoints)
10. [Code Flows & Examples](#code-flows--examples)
11. [Security Best Practices](#security-best-practices)
12. [Testing & Debugging](#testing--debugging)
13. [Performance Considerations](#performance-considerations)
14. [Recommended Further Improvements](#recommended-further-improvements)
15. [Roadmap for Production](#roadmap-for-production)

---

## Architecture Overview

OmniCart.Identity.Api is a lightweight ASP.NET Core 8 microservice for centralized user authentication and authorization using JWT tokens. It acts as the **identity provider** for the OmniCart microservices ecosystem (Order, Restaurant, Delivery, Notification APIs).

### High-Level Architecture (simple ASCII)

```
Client (Web/Mobile)
  |
  | POST /api/auth/register or /login
  v
OmniCart.Identity.Api (this service)
  - Controllers:
    - AuthController
      - POST /api/auth/register -> AuthService.RegisterAsync
      - POST /api/auth/login    -> AuthService.LoginAsync
  - Services (DI):
    - AuthService (IAuthService)  -- handles registration/login, password hashing
    - JwtService (IJwtService)    -- generates signed JWTs
  - Data Access (EF Core):
    - OmniCartIdentityDbContext
      - DbSet<User>
      - DbSet<Role>
  |
  v
SQL Server (OmniCartIdentityDb)
  - Users table
  - Roles table

Other microservices (Order, Restaurant, etc.) validate the JWT by checking the token signature and claims (role, user ID, etc.).
```

---

## Project Structure

- src/OmniCart.Identity.Api/
  - Entities/
    - Role.cs               # Role domain entity
    - User.cs               # User domain entity
  - Data/
    - OmniCartIdentityDbContext.cs  # EF Core DbContext
  - Dtos/
    - RegisterRequest.cs    # Registration payload
    - LoginRequest.cs       # Login payload
    - AuthResponse.cs       # Response with JWT token
  - Services/
    - IJwtService.cs        # JWT token generation interface
    - JwtService.cs         # JWT token generation implementation
    - IAuthService.cs       # Authentication service interface
    - AuthService.cs        # Authentication logic (register/login)
  - Controllers/
    - AuthController.cs     # HTTP endpoints for auth
  - Migrations/
    - [timestamp]_InitialCreate.cs      # Initial schema migration
    - OmniCartIdentityDbContextModelSnapshot.cs
  - Properties/
    - launchSettings.json   # Dev launch profiles & ports
  - Program.cs                # Startup configuration & DI
  - appsettings.json          # Configuration (connection string, JWT)
  - appsettings.Development.json  # Development overrides
  - OmniCart.Identity.Api.csproj    # Project file with package refs
  - OmniCart.Identity.Api.http      # HTTP request examples
  - README.md                 # Original documentation
  - README-Claude.md          # This comprehensive doc

---

## File-by-file Breakdown

### Entities/Role.cs

**Purpose:** Define the Role domain model.

```csharp
// Minimal role structure
public class Role
{
    public int Id { get; set; }                    // Primary key
    public string Name { get; set; } = null!;      // Role name (Admin, Customer, DeliveryPartner)
}
```

**Usage:** Roles are referenced by users and used in JWT claims for authorization decisions.

---

### Entities/User.cs

**Purpose:** Define the User domain model with relationships.

```csharp
public class User
{
    public int Id { get; set; }                    // Primary key
    public string Username { get; set; } = null!;  // Unique username (for login)
    public string PasswordHash { get; set; } = null!;  // BCrypt hashed password (never store plain text)
    public int RoleId { get; set; }                // Foreign key to Role
    public Role? Role { get; set; }                // Navigation property
}
```

**Security note:** PasswordHash is always hashed using BCrypt; plaintext passwords are never stored.

---

### Data/OmniCartIdentityDbContext.cs

**Purpose:** Entity Framework DbContext for data access abstraction.

```csharp
public class OmniCartIdentityDbContext : DbContext
{
    public OmniCartIdentityDbContext(DbContextOptions<OmniCartIdentityDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
}
```

**Responsibilities:**
- Maps C# entities to SQL Server tables.
- Provides LINQ query capabilities (Users.Where(...), Roles.FirstOrDefault(...)).
- Handles migrations and schema changes.

---

### Dtos/RegisterRequest.cs, LoginRequest.cs, AuthResponse.cs

**Purpose:** Transfer objects for API payloads (not domain entities).

- **RegisterRequest:** `{ username: string, password: string }`
- **LoginRequest:** `{ username: string, password: string }`
- **AuthResponse:** `{ token: string }`

**Why separate DTOs from entities?**
- Decouples API contracts from internal domain models.
- Allows validation, filtering, and transformation without exposing full entity data.
- Simplifies API versioning and schema evolution.

---

### Services/IJwtService.cs & JwtService.cs

**Purpose:** Generate signed JWT tokens.

```csharp
// Interface
public interface IJwtService
{
    string GenerateToken(User user);
}

// Implementation
public class JwtService : IJwtService
{
    public string GenerateToken(User user)
    {
        // Read Jwt:Key from config, encode as UTF-8 bytes
        // Create claims for user identity and role
        // Build JwtSecurityToken with 2-hour expiration
        // Sign using SymmetricSecurityKey (HMAC-SHA256)
        // Return serialized token string
    }
}
```

**Token anatomy (JWT):**
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.
eyJuYW1laWQiOiIxIiwibmFtZSI6ImFkbWluIiwicm9sZSI6IkFkbWluIn0.
abcd1234...
?
?? Header (base64): { "alg": "HS256", "typ": "JWT" }
?? Payload (base64): { "nameid": "1", "name": "admin", "role": "Admin" }
?? Signature (HMAC-SHA256 of header+payload with secret key)
```

---

### Services/IAuthService.cs & AuthService.cs

**Purpose:** Orchestrate authentication (register & login).

```csharp
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
}

public class AuthService : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // 1. Check if user already exists (prevent duplicates)
        // 2. Hash password with BCrypt
        // 3. Create new User entity with default role (RoleId=1 for Customer)
        // 4. Save to database
        // 5. Generate JWT and return AuthResponse
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        // 1. Find user by username
        // 2. Verify password using BCrypt.Verify()
        // 3. If valid, generate JWT and return AuthResponse
        // 4. If invalid, return null (Unauthorized response from controller)
    }
}
```

---

### Controllers/AuthController.cs

**Purpose:** HTTP endpoint definitions for authentication.

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        // Calls IAuthService.RegisterAsync()
        // Returns 200 OK with AuthResponse { token }
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        // Calls IAuthService.LoginAsync()
        // Returns 200 OK with token or 401 Unauthorized
    }
}
```

---

### Program.cs

**Purpose:** Application startup, dependency injection, middleware configuration.

**Key responsibilities:**
1. Register services (DbContext, IJwtService, IAuthService).
2. Configure JWT Bearer authentication.
3. Add EF Core with SQL Server provider.
4. Add Swagger (Swagger UI for docs).
5. Run migrations and seed data at startup.
6. Map controllers and middleware pipeline.

**Startup flow:**
```
Builder phase:
  ?? Register services (DI container)
  ?? Build WebApplication
  ?
App phase:
  ?? Create scope ? perform migration & seeding
  ?? Configure middleware pipeline
  ?  ?? UseSwagger (if dev)
  ?  ?? UseHttpsRedirection
  ?  ?? UseAuthentication (JWT Bearer)
  ?  ?? UseAuthorization (Claims/roles)
  ?  ?? MapControllers
  ?
Server phase:
  ?? app.Run() — listen for requests
```

---

### appsettings.json

**Purpose:** Configuration data (non-sensitive).

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=OmniCartIdentityDb;..."
  },
  "Jwt": {
    "Key": "your-jwt-secret-key-here",
    "Issuer": "OmniCart.Identity",
    "Audience": "OmniCart.Clients"
  }
}
```

**Security note:** In production, **never** store secrets (Jwt:Key, passwords, connection strings) in appsettings.json. Use:
- Azure Key Vault
- AWS Secrets Manager
- Environment variables
- User Secrets (dev only)

---

### Properties/launchSettings.json

We updated the file earlier; rest of README continues unchanged.

---
