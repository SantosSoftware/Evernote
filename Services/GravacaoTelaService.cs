using Evenote.Data;
using Microsoft.EntityFrameworkCore;

namespace Evenote.Services;

public class GravacaoTelaService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public GravacaoTelaService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // Salva o arquivo em wwwroot/uploads/gravacoes/ e persiste o registro no banco
    public async Task<GravacaoTela> SalvarGravacaoAsync(Guid notaId, IFormFile arquivo, string nomeOriginal)
    {
        var dir = Path.Combine(_env.WebRootPath, "uploads", "gravacoes");
        Directory.CreateDirectory(dir);

        var nomeArquivo = GerarNomeArquivo(notaId);
        var caminho = Path.Combine(dir, nomeArquivo);

        await using var fs = new FileStream(caminho, FileMode.Create);
        await arquivo.CopyToAsync(fs);

        var gravacao = new GravacaoTela
        {
            NotaId = notaId,
            CaminhoArquivo = $"/uploads/gravacoes/{nomeArquivo}",
            NomeOriginal = nomeOriginal,
            TamanhoBytes = arquivo.Length,
            CriadoEm = DateTime.Now
        };

        _db.GravacoesTela.Add(gravacao);
        await _db.SaveChangesAsync();
        return gravacao;
    }

    public async Task<List<GravacaoTela>> ObterPorNotaAsync(Guid notaId) =>
        await _db.GravacoesTela
            .Where(g => g.NotaId == notaId)
            .OrderBy(g => g.CriadoEm)
            .ToListAsync();

    public async Task ExcluirGravacaoAsync(Guid id)
    {
        var gravacao = await _db.GravacoesTela.FindAsync(id);
        if (gravacao == null) return;

        ExcluirArquivoFisico(gravacao.CaminhoArquivo);

        _db.GravacoesTela.Remove(gravacao);
        await _db.SaveChangesAsync();
    }

    public async Task ExcluirPorNotaAsync(Guid notaId)
    {
        var gravacoes = await _db.GravacoesTela
            .Where(g => g.NotaId == notaId)
            .ToListAsync();

        foreach (var g in gravacoes)
            ExcluirArquivoFisico(g.CaminhoArquivo);

        _db.GravacoesTela.RemoveRange(gravacoes);
        await _db.SaveChangesAsync();
    }

    private void ExcluirArquivoFisico(string caminhoRelativo)
    {
        try
        {
            var caminho = Path.Combine(_env.WebRootPath, caminhoRelativo.TrimStart('/'));
            if (File.Exists(caminho))
                File.Delete(caminho);
        }
        catch { /* arquivo ausente ou sem permissão — ignorar */ }
    }

    public static string GerarNomeArquivo(Guid notaId)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var sufixo = Guid.NewGuid().ToString("N")[..4];
        return $"{notaId}_{timestamp}_{sufixo}.webm";
    }

    public static string FormatarTamanho(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
    }

    public static string FormatarCronometro(int segundos)
    {
        var mm = segundos / 60;
        var ss = segundos % 60;
        return $"{mm:D2}:{ss:D2}";
    }
}
