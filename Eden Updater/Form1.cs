using System.Text;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Eden_Updater
{
    public partial class Form1 : Form
    {
        private string selectedFolderPath = string.Empty;

        public Form1()
        {
            InitializeComponent();
        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                selectedFolderPath = dialog.SelectedPath; // Guardar en variable global
                textBox1.Text = selectedFolderPath; // Ruta de la carpeta
            }

        }

        private async void button2_Click(object sender, EventArgs e)
        {
            // Mostrar barra de progreso
            progressBar1.Visible = true;
            progressBar1.Style = ProgressBarStyle.Marquee;
            button2.Enabled = false;

            // Crear archivo .ps1 en la misma ruta que el ejecutable
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string ps1FilePath = Path.Combine(exePath, "config.ps1");
            // Construir el contenido del script
            var sb = new StringBuilder();
            sb.AppendLine($"$FolderPath = \"{selectedFolderPath}\"");
            sb.AppendLine();
            sb.AppendLine("Invoke-WebRequest -Uri \"https://github.com/Eden-CI/Nightly/releases/download/v1773167852.8678cb06eb/Eden-Windows-8678cb06eb-amd64-msvc-standard.zip\" -OutFile \"eden.zip\"");
            sb.AppendLine("Expand-Archive -Path \"eden.zip\" -DestinationPath $FolderPath -Force");
            sb.AppendLine("Remove-Item \"eden.zip\"");

            File.WriteAllText(ps1FilePath, sb.ToString());

            // Ejecutar el archivo .ps1 sin mostrar la ventana de PowerShell
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{ps1FilePath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = exePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process? process = Process.Start(psi);
            
            if (process != null)
            {
                await process.WaitForExitAsync();
                
                // Ocultar barra de progreso
                progressBar1.Visible = false;
                button2.Enabled = true;

                // Mostrar mensaje de completado
                MessageBox.Show("Eden se ha actualizado correctamente", "Eden Updater", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
