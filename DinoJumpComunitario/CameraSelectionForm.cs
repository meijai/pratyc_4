namespace DinoJumpComunitario;

using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

public class CameraSelectionForm : Form
{
    public string SelectedCameraUrl { get; private set; }
    public double SelectedSensitivity { get; private set; }

    private readonly TextBox _txtPhoneIp;
    private readonly TextBox _txtPhonePort;
    private readonly Button _btnTest;
    private readonly Label _lblTestResult;
    private readonly TrackBar _tbSensitivity;
    private readonly Label _lblSensitivityVal;
    private readonly Button _btnConfirm;
    private readonly Button _btnCancel;

    public CameraSelectionForm(string currentCameraUrl, double currentSensitivity)
    {
        SelectedCameraUrl = string.IsNullOrWhiteSpace(currentCameraUrl)
            ? "http://192.168.1.50:4747/video"
            : currentCameraUrl;
        SelectedSensitivity = currentSensitivity;

        Text = "Configurar DroidCam (Wi-Fi) - Dino Jump";
        Size = new System.Drawing.Size(500, 390);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(248, 249, 250);
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

        // 1. Cabeçalho
        var lblHeader = new Label
        {
            Text = "Câmera do Celular (DroidCam Wi-Fi)",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.FromArgb(33, 37, 41),
            Location = new System.Drawing.Point(20, 16),
            AutoSize = true
        };
        Controls.Add(lblHeader);

        // 2. Painel de Instruções
        var pnlInfo = new Panel
        {
            Location = new System.Drawing.Point(20, 48),
            Size = new System.Drawing.Size(445, 130),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblHelp = new Label
        {
            Text = "1. Abra o aplicativo DroidCam no celular (mesma rede Wi-Fi do computador).\n" +
                   "2. Digite abaixo o 'WiFi IP' e a 'Port' exibidos no celular:",
            Location = new System.Drawing.Point(12, 10),
            Size = new System.Drawing.Size(420, 36),
            ForeColor = Color.FromArgb(80, 80, 80),
            Font = new Font("Segoe UI", 8.5f)
        };
        pnlInfo.Controls.Add(lblHelp);

        var lblIp = new Label
        {
            Text = "WiFi IP:",
            Location = new System.Drawing.Point(12, 54),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        pnlInfo.Controls.Add(lblIp);

        _txtPhoneIp = new TextBox
        {
            Location = new System.Drawing.Point(74, 51),
            Width = 145,
            Font = new Font("Segoe UI", 10f)
        };
        pnlInfo.Controls.Add(_txtPhoneIp);

        var lblPort = new Label
        {
            Text = "Porta:",
            Location = new System.Drawing.Point(230, 54),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        pnlInfo.Controls.Add(lblPort);

        _txtPhonePort = new TextBox
        {
            Location = new System.Drawing.Point(280, 51),
            Width = 60,
            Text = "4747",
            Font = new Font("Segoe UI", 10f)
        };
        pnlInfo.Controls.Add(_txtPhonePort);

        _btnTest = new Button
        {
            Text = "🔍 Testar IP",
            Location = new System.Drawing.Point(348, 50),
            Width = 85,
            Height = 28,
            BackColor = Color.FromArgb(240, 240, 240),
            FlatStyle = FlatStyle.System
        };
        _btnTest.Click += async (s, e) => await TestConnectionAsync();
        pnlInfo.Controls.Add(_btnTest);

        _lblTestResult = new Label
        {
            Text = "",
            Location = new System.Drawing.Point(14, 90),
            Size = new System.Drawing.Size(415, 25),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        pnlInfo.Controls.Add(_lblTestResult);

        Controls.Add(pnlInfo);

        // Preenche com valores atuais
        ParseUrlToInputs(SelectedCameraUrl);

        // 3. Sensibilidade do Pulo
        var lblSensTitle = new Label
        {
            Text = "Sensibilidade do Pulo:",
            Location = new System.Drawing.Point(20, 196),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        Controls.Add(lblSensTitle);

        _lblSensitivityVal = new Label
        {
            Text = $"{(int)(SelectedSensitivity * 100)}%",
            Location = new System.Drawing.Point(425, 196),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        Controls.Add(_lblSensitivityVal);

        _tbSensitivity = new TrackBar
        {
            Location = new System.Drawing.Point(170, 192),
            Width = 250,
            Minimum = 3,
            Maximum = 30,
            Value = (int)(SelectedSensitivity * 100),
            TickFrequency = 3
        };
        _tbSensitivity.Scroll += (s, e) =>
        {
            SelectedSensitivity = _tbSensitivity.Value / 100.0;
            _lblSensitivityVal.Text = $"{_tbSensitivity.Value}%";
        };
        Controls.Add(_tbSensitivity);

        var lblSensHelp = new Label
        {
            Text = "Sensibilidade padrão recomendada: 10% (quanto menor, mais sensível ao movimento).",
            Location = new System.Drawing.Point(20, 230),
            Size = new System.Drawing.Size(445, 36),
            ForeColor = Color.FromArgb(108, 117, 125),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic)
        };
        Controls.Add(lblSensHelp);

        // 4. Botões Confirmar e Cancelar
        _btnConfirm = new Button
        {
            Text = "Conectar e Jogar",
            DialogResult = DialogResult.OK,
            Location = new System.Drawing.Point(155, 285),
            Width = 180,
            Height = 40,
            BackColor = Color.FromArgb(40, 167, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
        };
        _btnConfirm.FlatAppearance.BorderSize = 0;
        _btnConfirm.Click += OnConfirmClick;
        Controls.Add(_btnConfirm);

        _btnCancel = new Button
        {
            Text = "Cancelar",
            DialogResult = DialogResult.Cancel,
            Location = new System.Drawing.Point(345, 285),
            Width = 120,
            Height = 40,
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular)
        };
        _btnCancel.FlatAppearance.BorderSize = 0;
        Controls.Add(_btnCancel);

        AcceptButton = _btnConfirm;
        CancelButton = _btnCancel;
    }

    private void ParseUrlToInputs(string url)
    {
        try
        {
            var uri = new Uri(url);
            _txtPhoneIp.Text = uri.Host;
            _txtPhonePort.Text = uri.Port.ToString();
        }
        catch
        {
            _txtPhoneIp.Text = "192.168.1.50";
            _txtPhonePort.Text = "4747";
        }
    }

    private async Task TestConnectionAsync()
    {
        string ip = _txtPhoneIp.Text.Trim();
        string port = string.IsNullOrWhiteSpace(_txtPhonePort.Text) ? "4747" : _txtPhonePort.Text.Trim();

        if (string.IsNullOrEmpty(ip))
        {
            _lblTestResult.ForeColor = Color.Red;
            _lblTestResult.Text = "Digite o IP do celular primeiro.";
            return;
        }

        _btnTest.Enabled = false;
        _lblTestResult.ForeColor = Color.FromArgb(0, 102, 204);
        _lblTestResult.Text = "Testando conexão com o celular...";

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2.5) };
            var response = await http.GetAsync($"http://{ip}:{port}/");
            if (response.IsSuccessStatusCode)
            {
                _lblTestResult.ForeColor = Color.FromArgb(40, 167, 69);
                _lblTestResult.Text = "✅ DroidCam encontrado e respondendo no Wi-Fi!";
            }
            else
            {
                _lblTestResult.ForeColor = Color.OrangeRed;
                _lblTestResult.Text = $"⚠️ Celular respondeu com código {(int)response.StatusCode}.";
            }
        }
        catch
        {
            _lblTestResult.ForeColor = Color.Red;
            _lblTestResult.Text = "❌ Não foi possível alcançar o celular. Verifique o Wi-Fi e o IP.";
        }
        finally
        {
            _btnTest.Enabled = true;
        }
    }

    private void OnConfirmClick(object? sender, EventArgs e)
    {
        string ip = _txtPhoneIp.Text.Trim();
        string port = string.IsNullOrWhiteSpace(_txtPhonePort.Text) ? "4747" : _txtPhonePort.Text.Trim();

        if (string.IsNullOrEmpty(ip))
        {
            MessageBox.Show("Digite o IP do celular antes de continuar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        SelectedCameraUrl = $"http://{ip}:{port}/video";
        Program.LogEvent($"[Config] Salva nova URL DroidCam: {SelectedCameraUrl}");
        Close();
    }
}
