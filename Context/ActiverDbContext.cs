using Microsoft.EntityFrameworkCore;

namespace ActiverWebAPI.Context;

public class ActiverDbContext : DbContext
{
    public ActiverDbContext(DbContextOptions<ActiverDbContext> options) : base(options)
    {

    }
}
