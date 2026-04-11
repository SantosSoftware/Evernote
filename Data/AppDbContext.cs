using Evenote.Services;
using Microsoft.EntityFrameworkCore;

namespace Evenote.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Caderno> Cadernos => Set<Caderno>();
    public DbSet<Nota> Notas => Set<Nota>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Nota>()
            .HasOne<Caderno>()
            .WithMany()
            .HasForeignKey(n => n.CadernoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
