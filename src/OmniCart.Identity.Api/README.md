# OmniCart.Identity.Api — Documentation

## Project overview

OmniCart.Identity.Api is a small ASP.NET Core Web API (NET 8) implementing basic user authentication with JWT tokens and EF Core persistence. It provides registration and login endpoints and a minimal role/user model. The project is intentionally lightweight (custom services) rather than using full ASP.NET Core Identity.

## Purpose of this document

This file explains the project structure, key files, configuration, packages, JWT implementation, running and testing instructions, and recommended production improvements including when/why to adopt ASP.NET Core Identity.

---

## File / Folder summary (what each file does)

- Program.cs
  - App startup and DI configuration.
  - Registers DbContext (SQL Server), services (IJwtService, IAuthService), JWT Bearer authentication, controllers, Swagger.
  - Runs EF Core migrations at startup and seeds roles and a default admin user.

- Entities/
  - Role.cs — simple role entity (Id, Name).
  - User.cs — user entity with Id, Username, PasswordHash, RoleId and Role navigation property.

- Data/OmniCartIdentityDbContext.cs
  - DbContext exposing Users and Roles DbSet<TEntity>.

- Dtos/
  - RegisterRequest.cs — { username, password }
  - LoginRequest.cs — { username, password }
  - AuthResponse.cs — { token }

- Services/
  - IJwtService.cs — interface for JWT generation.
  - JwtService.cs — generates JWT tokens using configuration (Jwt:Key, Issuer, Audience). Adds claims for NameIdentifier, Name and Role. Uses HMAC-SHA256 symmetric signing.
  - IAuthService.cs — auth use-case interface.
  - AuthService.cs — registers users (hashes password with BCrypt) and logs users in (verifies password, returns JWT). Uses EF Core to persist and read users. Default role id set on register to 1 (seeded roles expected).

- Controllers/AuthController.cs
  - API endpoints POST /api/auth/register and POST /api/auth/login.

- Properties/launchSettings.json
  - Local dev ports and profile configuration used by dotnet run and Visual Studio.

- appsettings.json
  - ConnectionStrings:DefaultConnection — SQL Server connection string (populate with valid server credentials).
  - Jwt:Key, Jwt:Issuer, Jwt:Audience — JWT signing/validation configuration (Jwt:Key should be stored securely).

- Migrations/
  - EF Core migration files created when running `dotnet ef migrations add InitialCreate` (may be present under src/OmniCart.Identity.Api/Migrations).

---

## Packages required (key packages used)

- Microsoft.EntityFrameworkCore (8.0.x)
- Microsoft.EntityFrameworkCore.SqlServer (8.0.x)
- Microsoft.EntityFrameworkCore.Tools (10.x for design-time tooling)
- Microsoft.AspNetCore.Authentication.JwtBearer (8.0.x)
- System.IdentityModel.Tokens.Jwt (used transitively for token handling)
- BCrypt.Net-Next (for password hashing)
- Swashbuckle.AspNetCore (Swagger)

These are referenced in OmniCart.Identity.Api.csproj.

---

## JWT implementation details

- Where
  - JwtService.cs generates tokens.
  - Program.cs configures JwtBearer authentication and validation parameters.

- How tokens are created
  - JwtService reads Jwt:Key from configuration (symmetric secret) and encodes it as UTF-8 bytes.
  - Claims created: ClaimTypes.NameIdentifier (user.Id), ClaimTypes.Name (user.Username), ClaimTypes.Role (user.Role?.Name).
  - Token lifetime: configured to 2 hours in JwtService (hard-coded).
  - Signing algorithm: HMAC-SHA256, using SymmetricSecurityKey.

- How validation is configured
  - Program.cs registers AddAuthentication().AddJwtBearer(...) with TokenValidationParameters:
    - ValidateIssuer, ValidateAudience, ValidateLifetime, ValidateIssuerSigningKey set to true.
    - ValidIssuer and ValidAudience read from configuration (Jwt:Issuer, Jwt:Audience).
    - IssuerSigningKey is constructed from the same Jwt:Key.

- Using the token
  - Client receives token from /api/auth/login or /api/auth/register in AuthResponse.token.
  - Include header `Authorization: Bearer <token>` on subsequent requests to protected endpoints.

---

## Why use (or not use) ASP.NET Core Identity for JWT auth

Short answer: ASP.NET Core Identity is not strictly required to implement JWT authentication; this project uses a custom lightweight approach. However, Identity provides a robust, secure, and feature-rich user management system that is strongly recommended for production.

- When ASP.NET Core Identity is helpful
  - You need complete user management features (password reset, email confirmation, lockout, tokens, claims management, external logins, two-factor auth).
  - You want standardized, well-tested flows and storage abstractions (Identity tables and stores).
  - You want future extensibility (roles, claims, stores that integrate with EF Core out-of-the-box).

- When a custom JWT solution may be ok
  - Small projects, prototypes, or microservices where you only need minimal auth (register / login) and want full control.
  - When you intentionally avoid large Identity baggage and only need token issuance/validation.

- Security tradeoffs
  - Implementing security correctly is hard: password hashing, account lockout, email verification and secure token flows are complex.
  - ASP.NET Core Identity reduces risk by providing vetted implementations; for production scenarios it lowers maintenance and security burden.

Recommendation: For production, adopt ASP.NET Core Identity (or an external identity provider) and issue JWTs from a trusted token issuing flow (e.g., IdentityServer, Azure AD, or Identity + custom token service). For small internal services a custom approach can be acceptable if you implement robust security controls.

---

## Running locally (quickstart)

1. Configure secrets
   - Set a strong secret for Jwt:Key. Prefer environment variables or user-secrets during development:
     - dotnet user-secrets init --project src/OmniCart.Identity.Api\OmniCart.Identity.Api.csproj
     - dotnet user-secrets set "Jwt:Key" "<your-strong-key>" --project src/OmniCart.Identity.Api\OmniCart.Identity.Api.csproj
   - Or set environment variable ASPNETCORE_ENVIRONMENT=Development and provide settings there.

2. Update SQL Server connection string
   - In src/OmniCart.Identity.Api/appsettings.json: ConnectionStrings:DefaultConnection — set Server, Database, User Id and Password or use Integrated Security.

3. Create and apply EF migrations
   - Install dotnet-ef tool if needed: `dotnet tool install --global dotnet-ef`
   - From repo root:
     - dotnet ef migrations add InitialCreate --project src\OmniCart.Identity.Api --startup-project src\OmniCart.Identity.Api
     - dotnet ef database update --project src\OmniCart.Identity.Api --startup-project src\OmniCart.Identity.Api
   - Alternatively, run the app; Program.cs calls `db.Database.Migrate()` on startup which will apply pending migrations.

4. Run the API
   - dotnet run --project src\OmniCart.Identity.Api
   - Swagger is available at `/swagger` in Development.

5. Test with Postman
   - Register: POST https://localhost:5001/api/auth/register
     - Body JSON: { "username": "user1", "password": "Password1!" }
     - Response: { "token": "<jwt>" }
   - Login: POST https://localhost:5001/api/auth/login
     - Body JSON: { "username": "admin", "password": "Admin@123" }
     - Response: { "token": "<jwt>" }
   - Use token: Authorization: Bearer <token>

---

## Seeding

- Program.cs seeds roles (Admin, Customer, DeliveryPartner) and a default admin account (username: admin, password: Admin@123) if not present.
- The seeding runs after performing `db.Database.Migrate()` during app startup.

---

## Security notes and recommended improvements for production

- Secrets: Move Jwt:Key to a secure store (Azure Key Vault, AWS Secrets Manager, environment variables, or user secrets for local dev).
- Password policies: Enforce strong passwords and consider password strength rules, account lockout, email verification.
- Token features: Implement refresh tokens, token revocation, and short access token lifetimes.
- Use ASP.NET Core Identity (or external provider) for full-featured identity management.
- HTTPS: Always use HTTPS in production and secure cookie options.
- Rate limiting and logging: Add request throttling, logging, monitoring and alerting.
- CORS: Configure allowed origins for browser-based clients.

---

## Common troubleshooting

- "Port already in use": update Properties/launchSettings.json or run with `--urls` arg to use different ports.
- Empty Roles/Users: Ensure migrations are applied and app startup seeding ran against the same DB connection string.
- Token validation failures: Confirm Jwt:Key, Issuer and Audience match between token issuer (JwtService) and validation parameters in Program.cs.

---

## Quick setup script (user-secrets and migrations)

Run these commands from your repository root. They set local user-secrets (so you do not commit secrets into appsettings.json), create the initial EF Core migration, and apply it to the database.

1) Initialize user-secrets for the Identity project (run once):

```powershell
dotnet user-secrets init --project "src\OmniCart.Identity.Api\OmniCart.Identity.Api.csproj"
```

2) Set your JWT key and connection string in user-secrets (replace placeholders):

```powershell
dotnet user-secrets set "Jwt:Key" "<YOUR_STRONG_JWT_KEY>" --project "src\OmniCart.Identity.Api\OmniCart.Identity.Api.csproj"

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=OmniCartIdentityDb;User Id=YOUR_DB_USER;Password=YOUR_DB_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true" --project "src\OmniCart.Identity.Api\OmniCart.Identity.Api.csproj"
```

3) Create the initial EF Core migration (if not already created):

```powershell
dotnet ef migrations add InitialCreate --project src\OmniCart.Identity.Api --startup-project src\OmniCart.Identity.Api
```

4) Apply migrations to the configured database:

```powershell
dotnet ef database update --project src\OmniCart.Identity.Api --startup-project src\OmniCart.Identity.Api
```

5) Run the API locally (development profile uses https://localhost:5001 by default):

```powershell
dotnet run --project src\OmniCart.Identity.Api
```

Notes:
- If you previously committed secrets to the repository, rotate those secrets immediately before continuing.
- On CI / production, set the same configuration via environment variables or a secret manager (Azure Key Vault, AWS Secrets Manager, etc.).

---

(End of quick setup)

