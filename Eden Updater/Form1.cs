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
            sb.Append("""
                param(
                    [Parameter(Mandatory = $true)]
                    [string]$FolderPath
                )

                $ErrorActionPreference = 'Stop'

                # API de Gitea para la última release
                $apiUrl = "https://git.eden-emu.dev/api/v1/repos/eden-ci/nightly/releases/latest"
                $headers = @{
                    "User-Agent" = "PowerShell-Nightly-Downloader"
                    "Accept"     = "application/json"
                }

                Write-Host "Obteniendo última release de eden-ci/nightly..."
                $release = Invoke-RestMethod -Uri $apiUrl -Headers $headers

                # Buscar asset .zip que contenga msvc y amd64
                $asset = $release.assets | Where-Object {
                    $_.name -match '(?i)msvc' -and
                    $_.name -match '(?i)amd64' -and
                    $_.name -match '\.zip$'
                } | Select-Object -First 1

                if (-not $asset) {
                    $names = ($release.assets | ForEach-Object { $_.name }) -join ", "
                    throw "No se encontró un asset .zip MSVC amd64. Assets disponibles: $names"
                }

                # Preparar rutas temporales
                $tempRoot = Join-Path $env:TEMP ("nightly_" + [Guid]::NewGuid().ToString("N"))
                $zipPath = Join-Path $tempRoot $asset.name
                $extractPath = Join-Path $tempRoot "extract"

                New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
                New-Item -ItemType Directory -Path $extractPath -Force | Out-Null

                if (-not (Test-Path $FolderPath)) {
                    New-Item -ItemType Directory -Path $FolderPath -Force | Out-Null
                }

                Write-Host "Descargando: $($asset.name)"
                Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zipPath -Headers $headers

                Write-Host "Descomprimiendo..."
                Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force

                Write-Host "Copiando y reemplazando en: $FolderPath"
                Get-ChildItem -Path $extractPath -Force | ForEach-Object {
                    Copy-Item -Path $_.FullName -Destination $FolderPath -Recurse -Force
                }

                # Limpieza
                Remove-Item -Path $tempRoot -Recurse -Force

                Write-Host "Proceso completado."
                """);

            File.WriteAllText(ps1FilePath, sb.ToString());

            // Ejecutar el archivo .ps1 sin mostrar la ventana de PowerShell
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{ps1FilePath}\" -FolderPath \"{selectedFolderPath}\"",
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
