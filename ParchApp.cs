using Parch.Components;
using Parch.Data;
using Parch.Services;
using Microsoft.EntityFrameworkCore;

namespace Parch;

/// <summary>
/// Ponto central de configuração do host ASP.NET Core do Parch.
/// Usado tanto pelo Program.cs (modo web) quanto pelo Parch.Desktop (modo desktop).
/// </summary>
public static class ParchApp
{
    public static WebApplication CreateApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Garante que a URL passada via args seja respeitada (sobrescreve ASPNETCORE_URLS)
        var urlArg = args.FirstOrDefault(a => a.StartsWith("--urls="));
        if (urlArg is not null)
        {
            var url = urlArg["--urls=".Length..];
            builder.WebHost.UseUrls(url);
        }

        // Em modo desktop (single file), o wwwroot fica junto ao .exe
        var exeDir = AppContext.BaseDirectory;
        var wwwrootPath = Path.Combine(exeDir, "wwwroot");
        if (Directory.Exists(wwwrootPath))
        {
            builder.Environment.WebRootPath = wwwrootPath;
        }

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddHubOptions(options => options.MaximumReceiveMessageSize = 100 * 1024 * 1024); // 100 MB

        // Banco de dados SQLite — caminho configurável via DATABASE_PATH
        var dbPath = Environment.GetEnvironmentVariable("DATABASE_PATH")
            ?? Path.Combine(builder.Environment.ContentRootPath, "parch.db");

        // Garante que o diretório do banco existe
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Scoped = uma instância por conexão SignalR (por aba do browser)
        builder.Services.AddScoped<NotaService>();
        builder.Services.AddScoped<TarefaService>();
        builder.Services.AddScoped<CalendarioService>();
        builder.Services.AddScoped<GravacaoTelaService>();
        builder.Services.AddHostedService<LembreteTarefaService>();

        builder.Services.AddControllers();

        var app = builder.Build();

        // Aplica migrations e semeia dados iniciais na primeira execução
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
            var pendingMigrations = db.Database.GetPendingMigrations().ToList();
            if (pendingMigrations.Count > 0)
            {
                logger.LogInformation("Aplicando {Count} migration(s): {Migrations}",
                    pendingMigrations.Count, string.Join(", ", pendingMigrations));
            }
            db.Database.Migrate();

            if (!db.Cadernos.Any())
            {
                db.Cadernos.Add(new Caderno { Nome = "Caderno Principal" });
                db.SaveChanges();
            }

            if (!db.Calendarios.Any())
            {
                db.Calendarios.Add(new CalendarioLocal { Nome = "Alexandre Mello", Cor = "#f4a54a" });
                db.SaveChanges();
            }
        }

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        // HTTPS é gerenciado pelo proxy do Fly.io — UseHttpsRedirection não é necessário no container
        app.UseStaticFiles();

        app.UseAntiforgery();

        // Endpoint dedicado para servir arquivos de upload
        app.MapGet("/uploads/{*filename}", (string filename, IWebHostEnvironment env) =>
        {
            var uploadsBase = Environment.GetEnvironmentVariable("UPLOADS_PATH")
                ?? Path.Combine(
                    env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"),
                    "uploads");
            var filePath = Path.Combine(uploadsBase, Uri.UnescapeDataString(filename));
            if (!File.Exists(filePath)) return Results.NotFound();
            var contentType = Path.GetExtension(filePath).ToLower() switch
            {
                ".pdf"              => "application/pdf",
                ".jpg" or ".jpeg"   => "image/jpeg",
                ".png"              => "image/png",
                ".gif"              => "image/gif",
                ".webp"             => "image/webp",
                ".webm"             => "video/webm",
                _                   => "application/octet-stream"
            };
            return Results.File(filePath, contentType, enableRangeProcessing: true);
        });

        app.MapStaticAssets();
        app.MapControllers();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        return app;
    }
}
