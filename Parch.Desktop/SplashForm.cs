namespace Parch.Desktop;

public class SplashForm : Form
{
    public SplashForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(400, 200);
        BackColor = Color.FromArgb(30, 30, 30);

        var lblNome = new Label
        {
            Text = "Parch",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 28, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(140, 50)
        };

        var lblStatus = new Label
        {
            Text = "Iniciando...",
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = new Font("Segoe UI", 11),
            AutoSize = true,
            Location = new Point(155, 110)
        };

        Controls.Add(lblNome);
        Controls.Add(lblStatus);
    }
}
