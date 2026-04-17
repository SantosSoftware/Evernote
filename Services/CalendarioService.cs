using Parch.Data;

namespace Parch.Services;

// ── Modelos ───────────────────────────────────────────────────────────────────

public class CalendarioLocal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = "";
    public string Cor { get; set; } = "#f4a54a";
    public DateTime CriadoEm { get; set; } = DateTime.Now;
}

public class Evento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Titulo { get; set; } = "";
    public DateTime Inicio { get; set; } = DateTime.Now;
    public DateTime Fim { get; set; } = DateTime.Now.AddHours(1);
    public bool DiaInteiro { get; set; } = false;
    public Guid CalendarioId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.Now;
}

// ── Serviço ───────────────────────────────────────────────────────────────────

public class CalendarioService
{
    private readonly AppDbContext _db;

    public CalendarioService(AppDbContext db) => _db = db;

    public IReadOnlyList<CalendarioLocal> Calendarios =>
        _db.Calendarios.OrderBy(c => c.CriadoEm).ToList();

    public CalendarioLocal? PrimeiroCalendario =>
        _db.Calendarios.OrderBy(c => c.CriadoEm).FirstOrDefault();

    public CalendarioLocal? ObterCalendario(Guid id) =>
        _db.Calendarios.Find(id);

    public List<Evento> EventosDoDia(DateTime data) =>
        EventosDoPeriodo(data.Date, data.Date);

    public List<Evento> EventosDoPeriodo(DateTime inicio, DateTime fim) =>
        _db.Eventos
            .Where(e => e.Inicio.Date <= fim.Date && e.Fim.Date >= inicio.Date)
            .OrderBy(e => e.Inicio)
            .ToList();

    public bool TemEventoNoDia(DateTime data) =>
        _db.Eventos.Any(e => e.Inicio.Date <= data.Date && e.Fim.Date >= data.Date);

    public void AdicionarEvento(Evento evento)
    {
        _db.Eventos.Add(evento);
        _db.SaveChanges();
    }

    public void ExcluirEvento(Evento evento)
    {
        _db.Eventos.Remove(evento);
        _db.SaveChanges();
    }
}
