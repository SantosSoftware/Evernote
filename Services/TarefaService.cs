using Evenote.Data;

namespace Evenote.Services;

// ── Modelos ──────────────────────────────────────────────────────────────────

public enum Prioridade { Nenhuma, Baixa, Media, Alta }

public enum TipoRecorrencia { Nenhuma, Diaria, Semanal, Mensal, Anual }

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

    // Recorrência
    public TipoRecorrencia Recorrencia { get; set; } = TipoRecorrencia.Nenhuma;
    public int IntervaloRecorrencia { get; set; } = 1;
    public string DiasSemanais { get; set; } = ""; // DayOfWeek ints separados por vírgula
    public DateTime? RecorrenciaFim { get; set; }

    // Notificação
    public bool Notificada { get; set; } = false;
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
        if (!tarefa.Concluida && tarefa.Recorrencia != TipoRecorrencia.Nenhuma)
        {
            // Tarefa recorrente concluída: avança para a próxima ocorrência
            var proxima = ProximaOcorrencia(tarefa);
            if (proxima.HasValue)
            {
                tarefa.DataVencimento = proxima.Value;
                tarefa.Notificada     = false;
                // Não marca como concluída — apenas reagenda
                _db.SaveChanges();
                return;
            }
            // Se não há próxima ocorrência (RecorrenciaFim atingido), conclui normalmente
        }

        tarefa.Concluida = !tarefa.Concluida;
        _db.SaveChanges();
    }

    public static DateTime? ProximaOcorrencia(Tarefa t)
    {
        if (t.DataVencimento == null || t.Recorrencia == TipoRecorrencia.Nenhuma)
            return null;

        var intervalo = t.IntervaloRecorrencia < 1 ? 1 : t.IntervaloRecorrencia;
        var base_ = t.DataVencimento.Value;

        DateTime proxima;
        if (t.Recorrencia == TipoRecorrencia.Semanal && !string.IsNullOrEmpty(t.DiasSemanais))
        {
            var dias = t.DiasSemanais
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out var n) ? (DayOfWeek?)n : null)
                .Where(d => d.HasValue).Select(d => d!.Value)
                .OrderBy(d => (int)d).ToList();

            if (dias.Count == 0) goto calcSimples;

            // Procura o próximo dia da semana após hoje
            var hoje = base_.DayOfWeek;
            var proximo = dias.FirstOrDefault(d => (int)d > (int)hoje);
            if (proximo != default)
            {
                proxima = base_.AddDays((int)proximo - (int)hoje);
            }
            else
            {
                // Volta para o primeiro dia na semana seguinte (+ intervalo)
                var primeiro = dias.First();
                var diasAte = (7 * intervalo) - (int)hoje + (int)primeiro;
                proxima = base_.AddDays(diasAte);
            }
            goto verificarFim;
        }

        calcSimples:
        proxima = t.Recorrencia switch
        {
            TipoRecorrencia.Diaria  => base_.AddDays(intervalo),
            TipoRecorrencia.Mensal  => base_.AddMonths(intervalo),
            TipoRecorrencia.Anual   => base_.AddYears(intervalo),
            _                       => base_.AddDays(7 * intervalo),
        };

        verificarFim:
        if (t.RecorrenciaFim.HasValue && proxima.Date > t.RecorrenciaFim.Value.Date)
            return null;

        return proxima;
    }

    public void Excluir(Tarefa tarefa)
    {
        _db.Tarefas.Remove(tarefa);
        _db.SaveChanges();
    }

    public void SalvarAlteracoes() => _db.SaveChanges();
}
