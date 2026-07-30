namespace OmniCart.Identity.Api.Dtos;

public class LoginRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}
