using Microsoft.EntityFrameworkCore;
using OmniCart.Identity.Api.Entities;

namespace OmniCart.Identity.Api.Data;

public class OmniCartIdentityDbContext : DbContext
{
    public OmniCartIdentityDbContext(DbContextOptions<OmniCartIdentityDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
}
