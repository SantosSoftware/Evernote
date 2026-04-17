using Parch.Data;
using Microsoft.EntityFrameworkCore;

namespace Parch.Services;

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
    public DateTime? CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; } = DateTime.Now;
    public bool Excluida { get; set; } = false;
    public Guid CadernoId { get; set; }
    public string? PdfPath { get; set; }
}

// ── Serviço ───────────────────────────────────────────────────────────────────

/// <summary>
/// Serviço Scoped: uma instância por conexão SignalR (por aba do browser).
/// Centraliza notas e cadernos via EF Core + SQLite.
/// </summary>
public class NotaService
{
    private readonly AppDbContext _db;
    private readonly Guid _cadernoPrincipalId;

    public NotaService(AppDbContext db)
    {
        _db = db;
        _cadernoPrincipalId = db.Cadernos.OrderBy(c => c.CriadoEm).First().Id;
    }

    // ── Cadernos ─────────────────────────────────────────────────────────────

    public IReadOnlyList<Caderno> Cadernos =>
        _db.Cadernos.OrderBy(c => c.CriadoEm).ToList();

    public Caderno? ObterCaderno(Guid id) =>
        _db.Cadernos.Find(id);

    public Guid IdCadernoPrincipal => _cadernoPrincipalId;

    public Caderno AdicionarCaderno(string nome)
    {
        var caderno = new Caderno { Nome = nome };
        _db.Cadernos.Add(caderno);
        _db.SaveChanges();
        return caderno;
    }

    public void RenomearCaderno(Caderno caderno, string novoNome)
    {
        caderno.Nome = novoNome;
        caderno.AtualizadoEm = DateTime.Now;
        _db.SaveChanges();
    }

    public void ExcluirCaderno(Caderno caderno)
    {
        // Move as notas do caderno para o principal antes de excluir
        var notasDoCaderno = _db.Notas.Where(n => n.CadernoId == caderno.Id).ToList();
        foreach (var nota in notasDoCaderno)
            nota.CadernoId = _cadernoPrincipalId;

        _db.Cadernos.Remove(caderno);
        _db.SaveChanges();
    }

    public int ContarNotasNoCaderno(Guid cadernoId) =>
        _db.Notas.Count(n => n.CadernoId == cadernoId && !n.Excluida);

    // ── Notas ────────────────────────────────────────────────────────────────

    public List<Nota> NotasAtivas =>
        _db.Notas.Where(n => !n.Excluida).ToList();

    public List<Nota> NotasExcluidas =>
        _db.Notas.Where(n => n.Excluida).ToList();

    public void Adicionar(Nota nota)
    {
        nota.CriadoEm ??= DateTime.Now;
        _db.Notas.Add(nota);
        _db.SaveChanges();
    }

    public void MoverParaLixeira(Nota nota)
    {
        nota.Excluida = true;
        nota.AtualizadoEm = DateTime.Now;
        _db.SaveChanges();
    }

    public void Restaurar(Nota nota)
    {
        nota.Excluida = false;
        _db.SaveChanges();
    }

    public void ExcluirDefinitivamente(Nota nota)
    {
        _db.Notas.Remove(nota);
        _db.SaveChanges();
    }

    public void EsvaziarLixeira()
    {
        _db.Notas.Where(n => n.Excluida).ExecuteDelete();
    }

    /// <summary>
    /// Persiste alterações rastreadas pelo EF Core (ex.: título e conteúdo editados na UI).
    /// </summary>
    public void SalvarAlteracoes() => _db.SaveChanges();
}

// ── Enums de ordenação ────────────────────────────────────────────────────────

public enum CriterioOrdenacao { DataCriacao, Titulo }
public enum DirecaoOrdenacao  { Crescente, Decrescente }
public enum EstadoGravacao    { Inativo, Gravando, Pausado }

// ── Gravação de Tela ──────────────────────────────────────────────────────────

public class GravacaoTela
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid NotaId { get; set; }
    public string CaminhoArquivo { get; set; } = "";
    public string NomeOriginal { get; set; } = "";
    public long TamanhoBytes { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.Now;
}
