using Evenote.Services;
using Microsoft.AspNetCore.Mvc;

namespace Evenote.Controllers;

[ApiController]
[Route("api/gravacoes")]
public class GravacaoesController : ControllerBase
{
    private readonly GravacaoTelaService _service;

    public GravacaoesController(GravacaoTelaService service)
    {
        _service = service;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(500 * 1024 * 1024)] // 500 MB
    public async Task<IActionResult> Upload([FromForm] UploadGravacaoRequest request)
    {
        if (request.Arquivo == null || request.Arquivo.Length == 0)
            return BadRequest(new { erro = "Arquivo não fornecido." });

        if (request.NotaId == Guid.Empty)
            return BadRequest(new { erro = "NotaId inválido." });

        var mimeValido = request.Arquivo.ContentType is "video/webm" or "application/octet-stream";
        if (!mimeValido)
            return BadRequest(new { erro = $"Tipo de arquivo não suportado: {request.Arquivo.ContentType}. Use WebM." });

        var nomeOriginal = string.IsNullOrWhiteSpace(request.NomeOriginal)
            ? request.Arquivo.FileName
            : request.NomeOriginal;

        var gravacao = await _service.SalvarGravacaoAsync(request.NotaId, request.Arquivo, nomeOriginal);

        return Ok(new GravacaoTelaDto(
            gravacao.Id,
            gravacao.NotaId,
            gravacao.CaminhoArquivo,
            gravacao.NomeOriginal,
            gravacao.TamanhoBytes,
            gravacao.CriadoEm
        ));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        await _service.ExcluirGravacaoAsync(id);
        return NoContent();
    }
}

public class UploadGravacaoRequest
{
    public Guid NotaId { get; set; }
    public IFormFile? Arquivo { get; set; }
    public string? NomeOriginal { get; set; }
}

public record GravacaoTelaDto(
    Guid Id,
    Guid NotaId,
    string CaminhoArquivo,
    string NomeOriginal,
    long TamanhoBytes,
    DateTime CriadoEm
);
