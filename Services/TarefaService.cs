using Evenote.Data;

namespace Evenote.Services;

// ── Modelos ──────────────────────────────────────────────────────────────────

public enum Prioridade { Nenhuma, Baixa, Media, Alta }

public class Tarefa
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Titulo { get; set; } = "";
    public string Descricao { get; set; } = "";
    public DateTime? DataVencimento { get; set; }
    public Prioridade Prioridade { get; set; } = Prioridade.Nenhuma;
    public bool Sinalizada { get; set; } = false;
    public bool Concluida { get; set; } = false;
    public DateTime CriadoEm { get; set; } = DateTime.Now;
}

// ── Serviço ───────────────────────────────────────────────────────────────────

public class TarefaService
{
    private readonly AppDbContext _db;

    public TarefaService(AppDbContext db) => _db = db;

    public List<Tarefa> TarefasAtivas =>
        _db.Tarefas.Where(t => !t.Concluida).OrderBy(t => t.CriadoEm).ToList();

    public List<Tarefa> TarefasHoje =>
        _db.Tarefas
            .Where(t => !t.Concluida && t.DataVencimento.HasValue
                && t.DataVencimento.Value.Date == DateTime.Today)
            .OrderBy(t => t.CriadoEm).ToList();

    public List<Tarefa> TarefasSinalizadas =>
        _db.Tarefas.Where(t => !t.Concluida && t.Sinalizada).OrderBy(t => t.CriadoEm).ToList();

    public List<Tarefa> TarefasConcluidas =>
        _db.Tarefas.Where(t => t.Concluida).OrderByDescending(t => t.CriadoEm).ToList();

    public int TotalAtivas => _db.Tarefas.Count(t => !t.Concluida);

    public Tarefa Adicionar(Tarefa tarefa)
    {
        _db.Tarefas.Add(tarefa);
        _db.SaveChanges();
        return tarefa;
    }

    public void ToggleConcluida(Tarefa tarefa)
    {
        tarefa.Concluida = !tarefa.Concluida;
        _db.SaveChanges();
    }

    public void Excluir(Tarefa tarefa)
    {
        _db.Tarefas.Remove(tarefa);
        _db.SaveChanges();
    }

    public void SalvarAlteracoes() => _db.SaveChanges();
}
