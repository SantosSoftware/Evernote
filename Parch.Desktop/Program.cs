using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;

namespace Parch.Desktop;

static class Program
{
    internal static IHost? WebHost { get; private set; }
    internal static int Port { get; private set; }

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, e) =>
        {
            MessageBox.Show(
                $"Erro inesperado: {e.Exception.Message}",
                "Parch — Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Environment.Exit(1);
        };

        // 1. Seleciona porta disponível
        Port = SelectAvailablePort();

        // 2. Configura variáveis de ambiente ANTES de inicializar o servidor
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var parchDir = Path.Combine(appData, "Parch");
        Directory.CreateDirectory(parchDir);
        Directory.CreateDirectory(Path.Combine(parchDir, "uploads"));
        Environment.SetEnvironmentVariable("DATABASE_PATH", Path.Combine(parchDir, "parch.db"));
        Environment.SetEnvironmentVariable("UPLOADS_PATH", Path.Combine(parchDir, "uploads"));

        // 3. Exibe splash
        var splash = new SplashForm();
        splash.Show();
        Application.DoEvents();

        // 4. Inicia o servidor web em background
        Task.Run(async () =>
        {
            try
            {
                var args = new[] { $"--urls=http://localhost:{Port}" };
                var webApp = Parch.ParchApp.CreateApp(args);
                WebHost = webApp;
                await webApp.StartAsync();
            }
            catch (Exception ex)
            {
                // Erro no startup — será detectado pelo timeout do WaitForServer
                Console.Error.WriteLine($"Erro ao iniciar servidor: {ex.Message}");
            }
        });

        // 5. Aguarda servidor ficar disponível (timeout 30s)
        bool ready = WaitForServer($"http://localhost:{Port}/", TimeSpan.FromSeconds(30));

        splash.Close();
        splash.Dispose();

        if (!ready)
        {
            MessageBox.Show(
                "O servidor não iniciou dentro do tempo esperado (30s).\n" +
                "Verifique se há conflitos de porta ou problemas de permissão.",
                "Parch — Erro de Inicialização",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Environment.Exit(1);
            return;
        }

        // 6. Abre janela principal
        var mainForm = new MainForm();
        Application.Run(mainForm);
    }

    /// <summary>
    /// Seleciona uma porta TCP disponível pedindo ao SO uma porta efêmera.
    /// </summary>
    internal static int SelectAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>
    /// Faz polling no endpoint até receber resposta ou atingir o timeout.
    /// </summary>
    internal static bool WaitForServer(string url, TimeSpan timeout)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = http.GetAsync(url).GetAwaiter().GetResult();
                if ((int)response.StatusCode < 500)
                    return true;
            }
            catch { /* servidor ainda não disponível */ }
            Thread.Sleep(200);
        }
        return false;
    }
}
