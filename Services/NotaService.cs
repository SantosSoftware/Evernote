namespace Evenote.Services;

// ── Modelos ──────────────────────────────────────────────────────────────────

public class Caderno
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = "";
    public string CriadoPor { get; set; } = "alexandre";
    public DateTime CriadoEm { get; set; } = DateTime.Now;
    public DateTime AtualizadoEm { get; set; } = DateTime.Now;
}

public class Nota
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Titulo { get; set; } = "";
    public string Conteudo { get; set; } = ""; // HTML
    public DateTime AtualizadoEm { get; set; } = DateTime.Now;
    public bool Excluida { get; set; } = false;
    public Guid CadernoId { get; set; } // toda nota pertence a um caderno
}

// ── Serviço ───────────────────────────────────────────────────────────────────

/// <summary>
/// Serviço Scoped: uma instância por conexão SignalR (por aba do browser).
/// Centraliza notas e cadernos para que todas as páginas compartilhem o mesmo estado.
/// </summary>
public class NotaService
{
    // Caderno padrão criado na inicialização
    private readonly Caderno _cadernoPrincipal = new()
    {
        Nome = "Caderno Principal",
        CriadoEm = DateTime.Now.AddHours(-3),
        AtualizadoEm = DateTime.Now.AddHours(-3)
    };

    private readonly List<Caderno> _cadernos;
    private readonly List<Nota> _notas;

    public NotaService()
    {
        _cadernos = new List<Caderno> { _cadernoPrincipal };

        // Notas de exemplo, todas vinculadas ao caderno principal
        _notas = new List<Nota>
        {
            new Nota
            {
                Titulo = "MBA - IA Generativa e Engenharia de Prompts",
                Conteudo = "<h1>MBA - IA GENERATIVA E ENGENHARIA DE PROMPTS COM LLMs</h1><h2>IA Generativa (o que é)</h2><ol><li>IA Generativa é um tipo de inteligência artificial capaz de <strong>criar conteúdo novo</strong>.</li><li>Em vez de apenas classificar ou prever rótulos, ela <strong>gera</strong> texto, imagens, áudio, vídeo e código.</li><li>Ela aprende padrões a partir de grandes volumes de dados (ex.: textos da internet, livros, código).</li><li>A partir desse aprendizado, produz saídas <strong>parecidas com as que veria nos dados</strong>, mas não \"copia\" literalmente.</li></ol>",
                AtualizadoEm = DateTime.Now.AddHours(-4),
                CadernoId = _cadernoPrincipal.Id
            },
            new Nota
            {
                Titulo = "Coisas a se fazer",
                Conteudo = "<p>Boas-vindas à sua nota de tarefas! Adicione suas tarefas aqui.</p><ul><li>Estudar Blazor</li><li>Criar projeto Evenote</li></ul>",
                AtualizadoEm = DateTime.Now.AddHours(-8),
                CadernoId = _cadernoPrincipal.Id
            },
            new Nota
            {
                Titulo = "2026/04/10",
                Conteudo = "<p>Fiz extração de um sisu. R$ 400,00. Depois fiquei em casa deitado com remédios para não ter ressaca.</p>",
                AtualizadoEm = DateTime.Now.AddHours(-9),
                CadernoId = _cadernoPrincipal.Id
            }
        };
    }

    // ── Cadernos ─────────────────────────────────────────────────────────────

    public IReadOnlyList<Caderno> Cadernos => _cadernos.AsReadOnly();

    public Caderno? ObterCaderno(Guid id) =>
        _cadernos.FirstOrDefault(c => c.Id == id);

    public Caderno AdicionarCaderno(string nome)
    {
        var caderno = new Caderno { Nome = nome };
        _cadernos.Add(caderno);
        return caderno;
    }

    public void RenomearCaderno(Caderno caderno, string novoNome)
    {
        caderno.Nome = novoNome;
        caderno.AtualizadoEm = DateTime.Now;
    }

    public void ExcluirCaderno(Caderno caderno)
    {
        // Move as notas do caderno para o principal antes de excluir
        foreach (var nota in _notas.Where(n => n.CadernoId == caderno.Id))
            nota.CadernoId = _cadernoPrincipal.Id;

        _cadernos.Remove(caderno);
    }

    public int ContarNotasNoCaderno(Guid cadernoId) =>
        _notas.Count(n => n.CadernoId == cadernoId && !n.Excluida);

    // ── Notas ────────────────────────────────────────────────────────────────

    public List<Nota> NotasAtivas => _notas.Where(n => !n.Excluida).ToList();

    public List<Nota> NotasExcluidas => _notas.Where(n => n.Excluida).ToList();

    public Guid IdCadernoPrincipal => _cadernoPrincipal.Id;

    public void Adicionar(Nota nota) => _notas.Add(nota);

    public void MoverParaLixeira(Nota nota) => nota.Excluida = true;

    public void Restaurar(Nota nota) => nota.Excluida = false;

    public void ExcluirDefinitivamente(Nota nota) => _notas.Remove(nota);

    public void EsvaziarLixeira()
    {
        _notas.RemoveAll(n => n.Excluida);
    }
}
