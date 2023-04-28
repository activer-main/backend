using ActiverWebAPI.Models.DBEntity;
using Microsoft.EntityFrameworkCore;

namespace ActiverWebAPI.Context;

public class ActiverDbContext : DbContext
{
    public ActiverDbContext(DbContextOptions<ActiverDbContext> options) : base(options)
    {

    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging();
        base.OnConfiguring(optionsBuilder);
    }

    public DbSet<User> User { get; set; }
    public DbSet<Activity> Activity { get; set; }
    public DbSet<Tag> Tag { get; set; }
}
