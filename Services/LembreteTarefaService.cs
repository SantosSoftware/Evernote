using System.Text;
using Parch.Data;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace Parch.Services;

public class LembreteTarefaService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<LembreteTarefaService> _logger;

    public LembreteTarefaService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<LembreteTarefaService> logger)
    {
        _scopeFactory = scopeFactory;
        _config       = config;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await VerificarLembretes();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task VerificarLembretes()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var agora  = DateTime.Now;
        var limite = agora.AddMinutes(10);

        var tarefas = await db.Tarefas
            .Where(t => !t.Concluida
                     && !t.Notificada
                     && t.DataVencimento.HasValue
                     && t.DataVencimento.Value >= agora
                     && t.DataVencimento.Value <= limite)
            .ToListAsync();

        foreach (var tarefa in tarefas)
        {
            try
            {
                await EnviarEmail(tarefa);
                tarefa.Notificada = true;
                _logger.LogInformation("Lembrete enviado para tarefa: {Titulo}", tarefa.Titulo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar lembrete para tarefa: {Titulo}", tarefa.Titulo);
            }
        }

        if (tarefas.Count > 0)
            await db.SaveChangesAsync();
    }

    private async Task EnviarEmail(Tarefa tarefa)
    {
        var smtp        = _config["Email:Smtp"]         ?? throw new InvalidOperationException("Email:Smtp não configurado.");
        var porta       = int.Parse(_config["Email:Porta"] ?? "587");
        var usuario     = _config["Email:Usuario"]      ?? throw new InvalidOperationException("Email:Usuario não configurado.");
        var senha       = _config["Email:Senha"]        ?? throw new InvalidOperationException("Email:Senha não configurado.");
        var destinatario= _config["Email:Destinatario"] ?? usuario;

        var vencimento = tarefa.DataVencimento!.Value;
        var hora = vencimento.TimeOfDay == TimeSpan.Zero
            ? vencimento.ToString("dd/MM/yyyy")
            : vencimento.ToString("dd/MM/yyyy 'às' HH:mm");

        var mensagem = new MimeMessage();
        mensagem.From.Add(MailboxAddress.Parse(usuario));
        mensagem.To.Add(MailboxAddress.Parse(destinatario));
        mensagem.Subject = $"⏰ Lembrete: {tarefa.Titulo}";

        var corpo = new StringBuilder();
        corpo.AppendLine($"<h2 style='margin:0 0 8px'>⏰ {tarefa.Titulo}</h2>");
        corpo.AppendLine($"<p style='color:#555;margin:0 0 4px'>Vence em: <strong>{hora}</strong></p>");

        if (!string.IsNullOrWhiteSpace(tarefa.Descricao))
            corpo.AppendLine($"<p style='color:#555;margin:8px 0 0'>{tarefa.Descricao}</p>");

        if (tarefa.Recorrencia != TipoRecorrencia.Nenhuma)
        {
            var proxima = TarefaService.ProximaOcorrencia(tarefa);
            if (proxima.HasValue)
                corpo.AppendLine($"<p style='color:#888;margin:8px 0 0;font-size:0.9em'>Próxima ocorrência: {proxima.Value:dd/MM/yyyy}</p>");
        }

        mensagem.Body = new TextPart("html") { Text = corpo.ToString() };

        using var client = new SmtpClient();
        await client.ConnectAsync(smtp, porta, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(usuario, senha);
        await client.SendAsync(mensagem);
        await client.DisconnectAsync(true);
    }
}
