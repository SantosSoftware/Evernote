using Evenote.Components;
using Evenote.Data;
using Evenote.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Banco de dados SQLite
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "evenote.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Scoped = uma instância por conexão SignalR (por aba do browser)
builder.Services.AddScoped<NotaService>();
builder.Services.AddScoped<TarefaService>();

var app = builder.Build();

// Aplica migrations e semeia o Caderno Principal na primeira execução
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    if (!db.Cadernos.Any())
    {
        db.Cadernos.Add(new Caderno { Nome = "Caderno Principal" });
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
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
