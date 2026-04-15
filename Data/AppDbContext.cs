using Evenote.Services;
using Microsoft.EntityFrameworkCore;

namespace Evenote.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Caderno> Cadernos => Set<Caderno>();
    public DbSet<Nota> Notas => Set<Nota>();
    public DbSet<Tarefa> Tarefas => Set<Tarefa>();
    public DbSet<CalendarioLocal> Calendarios => Set<CalendarioLocal>();
    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<GravacaoTela> GravacoesTela { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Nota>()
            .HasOne<Caderno>()
            .WithMany()
            .HasForeignKey(n => n.CadernoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GravacaoTela>()
            .HasOne<Nota>()
            .WithMany()
            .HasForeignKey(g => g.NotaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
