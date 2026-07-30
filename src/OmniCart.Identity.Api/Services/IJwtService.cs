using OmniCart.Identity.Api.Entities;

namespace OmniCart.Identity.Api.Services;

public interface IJwtService
{
    string GenerateToken(User user);
}
