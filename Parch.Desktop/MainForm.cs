using Microsoft.Web.WebView2.WinForms;

namespace Parch.Desktop;

public class MainForm : Form
{
    private readonly WebView2 _webView;

    public MainForm()
    {
        Text = "Parch";
        WindowState = FormWindowState.Maximized;

        var icoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "parch.ico");
        if (File.Exists(icoPath))
            Icon = new Icon(icoPath);

        _webView = new WebView2
        {
            Dock = DockStyle.Fill
        };
        Controls.Add(_webView);

        Load += OnLoad;
        FormClosed += OnFormClosed;
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        try
        {
            await _webView.EnsureCoreWebView2Async();
            _webView.CoreWebView2.Navigate($"http://localhost:{Program.Port}/");
        }
        catch (Exception ex) when (
            ex.Message.Contains("WebView2", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("Edge", StringComparison.OrdinalIgnoreCase) ||
            ex is System.Runtime.InteropServices.COMException)
        {
            MessageBox.Show(
                "O Microsoft WebView2 Runtime não está instalado nesta máquina.\n\n" +
                "Baixe e instale em:\nhttps://developer.microsoft.com/microsoft-edge/webview2/\n\n" +
                "Após a instalação, reinicie o Parch.",
                "Parch — WebView2 Necessário",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            Application.Exit();
        }
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        // Shutdown limpo do servidor com timeout de 10 segundos
        if (Program.WebHost is not null)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                Program.WebHost.StopAsync(cts.Token).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // Timeout atingido — encerramento forçado
            }
            finally
            {
                Program.WebHost.Dispose();
            }
        }
        Environment.Exit(0);
    }
}
