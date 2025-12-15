using Microsoft.EntityFrameworkCore;

using PulseBoard.Domain.Events.Entities;
using PulseBoard.Infrastructure.Entities;

namespace PulseBoard.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<EventRecord> EventRecords => Set<EventRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
