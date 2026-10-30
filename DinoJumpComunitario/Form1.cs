namespace DinoJumpComunitario;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;

public partial class Form1 : Form
{
    // ==========================================
    // 1. ESTADO VOLÁTIL EM RAM (LGPD COMPLIANCE)
    // ==========================================
    public static int SessionHighScore = 0; // Recorde da sessão, resetado ao fechar
    private int _score = 0;
    private int _jumpsThisRound = 0;
    private float _gameSpeed = 7.0f;
    private const float BaseSpeed = 7.0f;
    private const float MaxSpeed = 16.0f;

    private enum GameState
    {
        StartScreen,
        Playing,
        GameOver
    }

    private GameState _state = GameState.StartScreen;

    // ==========================================
    // 2. FÍSICA E ENTIDADES DO JOGO
    // ==========================================
    private System.Windows.Forms.Timer? _gameTimer;
    private float _groundY;
    private const float DinoWidth = 48f;
    private const float DinoHeight = 54f;
    private float _dinoX = 90f;
    private float _dinoY;
    private float _dinoVelocityY = 0f;
    private const float Gravity = 1.05f;
    private const float JumpVelocity = -15.5f;
    private bool _isGrounded = true;
    private int _runAnimFrame = 0;
    private int _frameCount = 0;

    private readonly List<Obstacle> _obstacles = new();
    private readonly List<Cloud> _clouds = new();
    private readonly List<GroundDetail> _groundDetails = new();
    private readonly List<DustParticle> _dustParticles = new();
    private readonly Random _rand = new();

    // ==========================================
    // 3. VISÃO COMPUTACIONAL (CELULAR VIA WI-FI)
    // ==========================================
    private string _cameraUrl = string.Empty; // Inicia vazio até o usuário informar o IP
    private VideoCapture? _capture;
    private CancellationTokenSource? _cameraCts;
    private Task? _cameraTask;
    private Bitmap? _webcamBitmap;
    private readonly object _bitmapLock = new();

    private bool _cameraActive = false;
    private string _cameraStatus = "Pressione [D] para vincular\no DroidCam do celular.";
    private double _sensitivity = 0.10; // 10% da área do ROI em movimento
    private double _currentMotionRatio = 0.0;
    private DateTime _lastJumpTrigger = DateTime.MinValue;
    private DateTime _lastMotionFlash = DateTime.MinValue;
    private const int JumpCooldownMs = 450; // Previne múltiplos pulos acidentais

    // Painel da câmera em tela
    private Rectangle _cameraBoxRect = new(720, 25, 250, 160);
    private bool _showCameraOverlay = true;

    // Modo tela cheia
    private bool _isFullscreen = false;
    private FormWindowState _prevWindowState;
    private FormBorderStyle _prevBorderStyle;

    public Form1()
    {
        InitializeComponent();

        DoubleBuffered = true;
        KeyPreview = true;
        BackColor = Color.FromArgb(247, 247, 247); // Estilo clássico Chrome Dino
        MinimumSize = new System.Drawing.Size(800, 480);

        InitGameEngine();
    }

    // ==========================================
    // INICIALIZAÇÃO DA ENGINE DO JOGO
    // ==========================================
    private void InitGameEngine()
    {
        _groundY = ClientSize.Height - 110f;
        _dinoY = _groundY - DinoHeight;

        // Inicializa nuvens e detalhes do chão
        _clouds.Clear();
        for (int i = 0; i < 4; i++)
        {
            _clouds.Add(new Cloud
            {
                X = _rand.Next(0, ClientSize.Width),
                Y = _rand.Next(40, 160),
                Scale = 0.8f + (float)_rand.NextDouble() * 0.5f,
                Speed = 0.6f + (float)_rand.NextDouble() * 0.8f
            });
        }

        _groundDetails.Clear();
        for (int i = 0; i < 20; i++)
        {
            _groundDetails.Add(new GroundDetail
            {
                X = _rand.Next(0, ClientSize.Width),
                Width = _rand.Next(4, 16)
            });
        }

        _gameTimer = new System.Windows.Forms.Timer();
        _gameTimer.Interval = 16; // ~60 FPS
        _gameTimer.Tick += GameLoopTick;
        _gameTimer.Start();
    }

    private void ResetGame()
    {
        _score = 0;
        _jumpsThisRound = 0;
        _gameSpeed = BaseSpeed;
        _dinoY = _groundY - DinoHeight;
        _dinoVelocityY = 0f;
        _isGrounded = true;
        _obstacles.Clear();
        _dustParticles.Clear();
        _state = GameState.Playing;
    }

    // ==========================================
    // LOOP PRINCIPAL DO JOGO (~60 FPS)
    // ==========================================
    private void GameLoopTick(object? sender, EventArgs e)
    {
        _frameCount++;
        _groundY = ClientSize.Height - 110f;
        _cameraBoxRect = new Rectangle(ClientSize.Width - 265, 20, 245, 160);

        if (_state == GameState.Playing)
        {
            UpdatePhysics();
            UpdateObstacles();
            UpdateScoreAndSpeed();
        }

        UpdateEnvironment();
        Invalidate(); // Redesenha a tela
    }

    private void UpdatePhysics()
    {
        // Gravidade e Pulo
        _dinoVelocityY += Gravity;
        _dinoY += _dinoVelocityY;

        float floorLevel = _groundY - DinoHeight;
        if (_dinoY >= floorLevel)
        {
            if (!_isGrounded && _dinoVelocityY > 5f)
            {
                // Partículas de poeira ao aterrissar
                SpawnDust(_dinoX + 10, _groundY);
                SpawnDust(_dinoX + DinoWidth - 10, _groundY);
            }
            _dinoY = floorLevel;
            _dinoVelocityY = 0f;
            _isGrounded = true;
        }
        else
        {
            _isGrounded = false;
        }

        // Animação das pernas a cada 6 frames quando no chão
        if (_isGrounded && _frameCount % 6 == 0)
        {
            _runAnimFrame = (_runAnimFrame + 1) % 2;
        }

        // Atualiza partículas
        for (int i = _dustParticles.Count - 1; i >= 0; i--)
        {
            _dustParticles[i].X -= _gameSpeed * 0.6f;
            _dustParticles[i].Life -= 0.08f;
            if (_dustParticles[i].Life <= 0)
            {
                _dustParticles.RemoveAt(i);
            }
        }
    }

    private void UpdateObstacles()
    {
        // Move obstáculos
        for (int i = _obstacles.Count - 1; i >= 0; i--)
        {
            _obstacles[i].X -= _gameSpeed;

            if (_obstacles[i].X + _obstacles[i].Width < 0)
            {
                _obstacles.RemoveAt(i);
            }
        }

        // Gera novos obstáculos com espaçamento seguro proporcional à velocidade
        float minSpacing = 280f + (_gameSpeed * 18f);
        float lastObstacleX = _obstacles.Count > 0 ? _obstacles[^1].X : 0;

        if (_obstacles.Count == 0 || (ClientSize.Width - lastObstacleX > minSpacing && _rand.Next(0, 100) < 5))
        {
            int cactusType = _rand.Next(0, 4);
            float width = cactusType switch
            {
                0 => 22f, // Pequeno
                1 => 44f, // Duplo
                2 => 65f, // Triplo
                _ => 28f  // Alto
            };
            float height = cactusType == 3 ? 62f : 45f;

            _obstacles.Add(new Obstacle
            {
                X = ClientSize.Width + _rand.Next(20, 100),
                Y = _groundY - height,
                Width = width,
                Height = height,
                Type = cactusType
            });
        }

        // Detecção de Colisão com margem de tolerância (Hitbox justo e amigável)
        var dinoHitbox = new RectangleF(
            _dinoX + 8,
            _dinoY + 6,
            DinoWidth - 14,
            DinoHeight - 10
        );

        foreach (var obs in _obstacles)
        {
            var obsHitbox = new RectangleF(
                obs.X + 4,
                obs.Y + 4,
                obs.Width - 8,
                obs.Height - 6
            );

            if (dinoHitbox.IntersectsWith(obsHitbox))
            {
                // Game Over
                _state = GameState.GameOver;
                if (_score > SessionHighScore)
                {
                    SessionHighScore = _score;
                }
                break;
            }
        }
    }

    private void UpdateScoreAndSpeed()
    {
        _score++;
        if (_score > SessionHighScore)
        {
            SessionHighScore = _score;
        }

        // Aumento suave de velocidade progressiva
        if (_gameSpeed < MaxSpeed)
        {
            _gameSpeed = BaseSpeed + (_score * 0.0035f);
            if (_gameSpeed > MaxSpeed) _gameSpeed = MaxSpeed;
        }
    }

    private void UpdateEnvironment()
    {
        // Nuvens
        foreach (var cloud in _clouds)
        {
            cloud.X -= cloud.Speed;
            if (cloud.X < -100)
            {
                cloud.X = ClientSize.Width + _rand.Next(30, 120);
                cloud.Y = _rand.Next(40, 160);
            }
        }

        // Chão móvel
        float speed = _state == GameState.Playing ? _gameSpeed : 1.5f;
        foreach (var dot in _groundDetails)
        {
            dot.X -= speed;
            if (dot.X < -20)
            {
                dot.X = ClientSize.Width + _rand.Next(5, 50);
            }
        }
    }

    private void SpawnDust(float x, float y)
    {
        for (int i = 0; i < 4; i++)
        {
            _dustParticles.Add(new DustParticle
            {
                X = x + _rand.Next(-4, 5),
                Y = y - _rand.Next(1, 6),
                Size = _rand.Next(2, 5),
                Life = 1.0f
            });
        }
    }

    // ==========================================
    // 4. MÉTODO PÚBLICO DE PULO (GATILHO EXTERNO)
    // ==========================================
    public void AcionarPulo()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(AcionarPulo));
            return;
        }

        if (_state == GameState.StartScreen || _state == GameState.GameOver)
        {
            ResetGame();
            return;
        }

        if (_state == GameState.Playing && _isGrounded)
        {
            _dinoVelocityY = JumpVelocity;
            _isGrounded = false;
            _jumpsThisRound++;
            SpawnDust(_dinoX + 15, _groundY);
        }
    }

    // ==========================================
    // 5. MÓDULO DO CELULAR (DROIDCAM WI-FI)
    // ==========================================
    private void StartWebcamCapture()
    {
        if (string.IsNullOrWhiteSpace(_cameraUrl)) return;

        StopWebcamCapture();

        _cameraCts = new CancellationTokenSource();
        var token = _cameraCts.Token;

        _cameraTask = Task.Run(() => CameraWorker(_cameraUrl, token), token);
    }

    private void StopWebcamCapture()
    {
        try
        {
            _cameraCts?.Cancel();
        }
        catch { }

        lock (_bitmapLock)
        {
            _webcamBitmap?.Dispose();
            _webcamBitmap = null;
        }
    }

    public void AbrirSelecaoDispositivos()
    {
        using (var dlg = new CameraSelectionForm(_cameraUrl, _sensitivity))
        {
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _cameraUrl = dlg.SelectedCameraUrl;
                _sensitivity = dlg.SelectedSensitivity;
                Program.LogEvent($"[Jogo] Nova URL Wi-Fi configurada: {_cameraUrl}");

                StartWebcamCapture();
            }
        }
    }

    private void CameraWorker(string cameraUrl, CancellationToken token)
    {
        VideoCapture? capture = null;
        try
        {
            Program.LogEvent($"CameraWorker iniciando conexão: {cameraUrl}");
            _cameraStatus = "Conectando ao celular...";

            capture = new VideoCapture(cameraUrl);

            // Se falhou com /video, tenta alternativa /mjpegfeed
            if (!capture.IsOpened())
            {
                capture.Dispose();
                string altUrl = cameraUrl.EndsWith("/video", StringComparison.OrdinalIgnoreCase)
                    ? cameraUrl.Substring(0, cameraUrl.Length - 6) + "/mjpegfeed"
                    : cameraUrl.TrimEnd('/') + "/video";
                capture = new VideoCapture(altUrl);
            }

            if (!capture.IsOpened())
            {
                _cameraActive = false;
                _cameraStatus = "Não foi possível conectar.\nPressione [D] para conferir o IP.";
                Program.LogEvent($"CameraWorker falhou ao abrir: {cameraUrl}");
                return;
            }

            _capture = capture;
            _cameraActive = true;
            _cameraStatus = "Celular (Wi-Fi) Ativo";
            Program.LogEvent($"CameraWorker conectado com sucesso em {cameraUrl}");

            using var rawFrame = new Mat();
            using var resized = new Mat();
            using var flipped = new Mat();
            using var grayCurrent = new Mat();
            using var grayBlurred = new Mat();
            using var prevGrayBlurred = new Mat();
            using var diff = new Mat();
            using var thresh = new Mat();

            int consecutiveEmpty = 0;

            while (!token.IsCancellationRequested)
            {
                if (!capture.Read(rawFrame) || rawFrame.Empty())
                {
                    consecutiveEmpty++;
                    if (consecutiveEmpty > 60) // 2 segundos sem sinal
                    {
                        Program.LogEvent("CameraWorker: Sinal de vídeo interrompido.");
                        break;
                    }
                    Thread.Sleep(10);
                    continue;
                }

                consecutiveEmpty = 0;

                // Reduz resolução para 320x240 para ultra baixa latência (< 1ms)
                Mat workMat;
                if (rawFrame.Width > 360 || rawFrame.Height > 270)
                {
                    Cv2.Resize(rawFrame, resized, new OpenCvSharp.Size(320, 240), 0, 0, InterpolationFlags.Nearest);
                    workMat = resized;
                }
                else
                {
                    workMat = rawFrame;
                }

                // 1. Espelhamento horizontal
                Cv2.Flip(workMat, flipped, FlipMode.Y);

                // 2. Grayscale + Blur leve
                Cv2.CvtColor(flipped, grayCurrent, ColorConversionCodes.BGR2GRAY);
                Cv2.GaussianBlur(grayCurrent, grayBlurred, new OpenCvSharp.Size(11, 11), 0);

                // 3. Região de Interesse (ROI)
                int roiWidth = (int)(flipped.Width * 0.72);
                int roiHeight = (int)(flipped.Height * 0.38);
                int roiX = (flipped.Width - roiWidth) / 2;
                int roiY = 8;
                var roiRect = new Rect(roiX, roiY, roiWidth, roiHeight);

                double motionRatio = 0.0;
                bool triggeredThisFrame = false;

                if (!prevGrayBlurred.Empty())
                {
                    Cv2.Absdiff(grayBlurred, prevGrayBlurred, diff);

                    using var roiDiff = new Mat(diff, roiRect);
                    Cv2.Threshold(roiDiff, thresh, 25, 255, ThresholdTypes.Binary);

                    int motionPixels = Cv2.CountNonZero(thresh);
                    int totalRoiPixels = roiWidth * roiHeight;
                    motionRatio = (double)motionPixels / totalRoiPixels;
                    _currentMotionRatio = motionRatio;

                    if (motionRatio >= _sensitivity)
                    {
                        var now = DateTime.UtcNow;
                        if ((now - _lastJumpTrigger).TotalMilliseconds >= JumpCooldownMs)
                        {
                            _lastJumpTrigger = now;
                            _lastMotionFlash = now;
                            triggeredThisFrame = true;

                            AcionarPulo();
                        }
                    }
                }

                grayBlurred.CopyTo(prevGrayBlurred);

                bool isFlashing = (DateTime.UtcNow - _lastMotionFlash).TotalMilliseconds < 250;
                Scalar boxColor = isFlashing
                    ? new Scalar(0, 0, 255)
                    : (triggeredThisFrame
                        ? new Scalar(0, 255, 255)
                        : new Scalar(0, 220, 0));

                Cv2.Rectangle(flipped, roiRect, boxColor, 2);

                string roiLabel = isFlashing ? "PULO DETECTADO!" : "ZONA DE DETECCAO";
                Cv2.PutText(
                    flipped,
                    roiLabel,
                    new OpenCvSharp.Point(roiX + 6, roiY + 18),
                    HersheyFonts.HersheySimplex,
                    0.45,
                    boxColor,
                    1
                );

                var bmp = BitmapConverter.ToBitmap(flipped);
                lock (_bitmapLock)
                {
                    _webcamBitmap?.Dispose();
                    _webcamBitmap = bmp;
                }
            }
        }
        catch (Exception ex)
        {
            Program.LogCrash("Form1.CameraWorker", ex);
        }
        finally
        {
            _cameraActive = false;
            _cameraStatus = "Celular desconectado.\nPressione [D] para configurar.";
            try
            {
                capture?.Release();
                capture?.Dispose();
            }
            catch { }
            _capture = null;
        }
    }

    // ==========================================
    // 6. RENDERIZAÇÃO GRÁFICA (GDI+)
    // ==========================================
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Desenha Céu e Nuvens
        DrawClouds(g);

        // Desenha Chão
        DrawGround(g);

        // Desenha Obstáculos (Cactos)
        DrawObstacles(g);

        // Desenha Partículas de Poeira
        DrawDust(g);

        // Desenha Dinossauro
        DrawDinosaur(g);

        // Desenha Interface do Usuário (Placar, Recorde, Pulos)
        DrawHUD(g);

        // Desenha Miniatura da Webcam no canto superior direito
        if (_showCameraOverlay)
        {
            DrawWebcamPanel(g);
        }

        // Telas de Início ou Game Over
        if (_state == GameState.StartScreen)
        {
            DrawStartOverlay(g);
        }
        else if (_state == GameState.GameOver)
        {
            DrawGameOverOverlay(g);
        }
    }

    private void DrawClouds(Graphics g)
    {
        using var cloudBrush = new SolidBrush(Color.FromArgb(210, 210, 210));
        foreach (var c in _clouds)
        {
            float w = 55f * c.Scale;
            float h = 18f * c.Scale;
            g.FillEllipse(cloudBrush, c.X, c.Y, w, h);
            g.FillEllipse(cloudBrush, c.X + (12f * c.Scale), c.Y - (8f * c.Scale), 26f * c.Scale, 22f * c.Scale);
            g.FillEllipse(cloudBrush, c.X + (28f * c.Scale), c.Y - (5f * c.Scale), 20f * c.Scale, 18f * c.Scale);
        }
    }

    private void DrawGround(Graphics g)
    {
        using var groundPen = new Pen(Color.FromArgb(83, 83, 83), 2.5f);
        g.DrawLine(groundPen, 0, _groundY, ClientSize.Width, _groundY);

        // Textura do chão em movimento
        using var dotBrush = new SolidBrush(Color.FromArgb(130, 130, 130));
        foreach (var dot in _groundDetails)
        {
            g.FillRectangle(dotBrush, dot.X, _groundY + 8, dot.Width, 2);
            g.FillRectangle(dotBrush, dot.X + 20, _groundY + 16, Math.Max(2, dot.Width - 4), 2);
        }
    }

    private void DrawObstacles(Graphics g)
    {
        using var cactusBrush = new SolidBrush(Color.FromArgb(46, 125, 50)); // Verde cacto
        using var cactusBorder = new Pen(Color.FromArgb(27, 94, 32), 2f);

        foreach (var obs in _obstacles)
        {
            // Tronco principal
            var rect = new RectangleF(obs.X, obs.Y, obs.Width, obs.Height);
            g.FillRectangle(cactusBrush, rect);
            g.DrawRectangle(cactusBorder, rect.X, rect.Y, rect.Width, rect.Height);

            // Detalhes de espinhos/braços nos cactos
            if (obs.Width > 26)
            {
                float armY = obs.Y + 14;
                g.FillRectangle(cactusBrush, obs.X - 5, armY, 6, 14);
                g.FillRectangle(cactusBrush, obs.X + obs.Width - 1, armY + 4, 6, 14);
            }
        }
    }

    private void DrawDust(Graphics g)
    {
        foreach (var p in _dustParticles)
        {
            int alpha = (int)(p.Life * 180);
            using var dustBrush = new SolidBrush(Color.FromArgb(alpha, 120, 120, 120));
            g.FillEllipse(dustBrush, p.X, p.Y, p.Size, p.Size);
        }
    }

    private void DrawDinosaur(Graphics g)
    {
        using var dinoBrush = new SolidBrush(Color.FromArgb(60, 60, 60));
        using var eyeBrush = new SolidBrush(Color.White);

        float x = _dinoX;
        float y = _dinoY;

        // Corpo
        g.FillRectangle(dinoBrush, x + 12, y + 16, 26, 26);

        // Cabeça
        g.FillRectangle(dinoBrush, x + 24, y, 22, 18);

        // Focinho
        g.FillRectangle(dinoBrush, x + 40, y + 6, 8, 12);

        // Olho
        if (_state == GameState.GameOver)
        {
            // Olhos em "X" quando derrotado
            using var deadPen = new Pen(Color.White, 2f);
            g.DrawLine(deadPen, x + 30, y + 4, x + 36, y + 10);
            g.DrawLine(deadPen, x + 36, y + 4, x + 30, y + 10);
        }
        else
        {
            g.FillRectangle(eyeBrush, x + 31, y + 4, 4, 4);
        }

        // Braço
        g.FillRectangle(dinoBrush, x + 36, y + 24, 7, 4);
        g.FillRectangle(dinoBrush, x + 41, y + 26, 2, 4);

        // Cauda
        g.FillRectangle(dinoBrush, x + 4, y + 22, 10, 8);
        g.FillRectangle(dinoBrush, x, y + 18, 6, 6);

        // Pernas (Animação de corrida ou pulo)
        if (!_isGrounded)
        {
            // Pernas recolhidas no ar
            g.FillRectangle(dinoBrush, x + 16, y + 42, 6, 8);
            g.FillRectangle(dinoBrush, x + 28, y + 40, 6, 6);
        }
        else
        {
            if (_runAnimFrame == 0)
            {
                // Perna esquerda esticada, direita curvada
                g.FillRectangle(dinoBrush, x + 16, y + 42, 5, 12);
                g.FillRectangle(dinoBrush, x + 16, y + 51, 8, 3);

                g.FillRectangle(dinoBrush, x + 28, y + 42, 5, 8);
            }
            else
            {
                // Perna direita esticada, esquerda curvada
                g.FillRectangle(dinoBrush, x + 16, y + 42, 5, 8);

                g.FillRectangle(dinoBrush, x + 28, y + 42, 5, 12);
                g.FillRectangle(dinoBrush, x + 28, y + 51, 8, 3);
            }
        }
    }

    private void DrawHUD(Graphics g)
    {
        using var fontScore = new Font("Segoe UI", 16, FontStyle.Bold);
        using var fontSub = new Font("Segoe UI", 11, FontStyle.Regular);
        using var textBrush = new SolidBrush(Color.FromArgb(70, 70, 70));
        using var hiBrush = new SolidBrush(Color.FromArgb(120, 120, 120));

        // Placar e Recorde da Sessão em RAM
        string scoreText = $"SCORE: {_score:D5}";
        string hiText = $"HI: {SessionHighScore:D5}";
        g.DrawString(scoreText, fontScore, textBrush, 30, 25);
        g.DrawString(hiText, fontScore, hiBrush, 210, 25);

        // Pulos da rodada e velocidade
        string jumpsText = $"Pulos na Rodada: {_jumpsThisRound}";
        string speedText = $"Velocidade: {_gameSpeed:F1}x";
        g.DrawString(jumpsText, fontSub, textBrush, 30, 60);
        g.DrawString(speedText, fontSub, textBrush, 190, 60);

        // Barra inferior de atalhos e acessibilidade
        using var fontHelp = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        using var helpBg = new SolidBrush(Color.FromArgb(235, 235, 235));
        using var helpBorder = new Pen(Color.FromArgb(210, 210, 210));
        var helpRect = new Rectangle(20, ClientSize.Height - 42, ClientSize.Width - 40, 30);
        g.FillRectangle(helpBg, helpRect);
        g.DrawRectangle(helpBorder, helpRect);

        string controlsText = " [ESPAÇO] Pular / Reiniciar  |  [D] Configurar Celular (Wi-Fi)  |  [+] / [-] Sensibilidade: " +
                              $"{(int)(_sensitivity * 100)}%  |  [F11] Tela Cheia  |  [V] Ocultar Câmera";
        g.DrawString(controlsText, fontHelp, textBrush, helpRect.X + 10, helpRect.Y + 6);
    }

    private void DrawWebcamPanel(Graphics g)
    {
        var box = _cameraBoxRect;

        // Fundo do painel
        using var panelBrush = new SolidBrush(Color.FromArgb(35, 35, 35));
        using var borderPen = new Pen(Color.FromArgb(60, 60, 60), 2f);
        g.FillRectangle(panelBrush, box);
        g.DrawRectangle(borderPen, box);

        // Feed da webcam
        lock (_bitmapLock)
        {
            if (_webcamBitmap != null)
            {
                var videoRect = new Rectangle(box.X + 4, box.Y + 22, box.Width - 8, box.Height - 48);
                g.DrawImage(_webcamBitmap, videoRect);
            }
            else
            {
                using var msgFont = new Font("Segoe UI", 9f, FontStyle.Italic);
                using var msgBrush = new SolidBrush(Color.FromArgb(200, 200, 200));
                var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(_cameraStatus, msgFont, msgBrush, box, sf);
            }
        }

        // Título do painel da webcam
        using var titleFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var titleBrush = new SolidBrush(_cameraActive ? Color.LightGreen : Color.OrangeRed);
        string statusIcon = _cameraActive ? "● CELULAR WI-FI" : "○ CELULAR DESCONECTADO";
        g.DrawString(statusIcon, titleFont, titleBrush, box.X + 8, box.Y + 4);

        // Atalho de configuração
        using var cfgFont = new Font("Segoe UI", 7.5f, FontStyle.Underline);
        using var cfgBrush = new SolidBrush(Color.FromArgb(180, 200, 240));
        g.DrawString("⚙ [D] Configurar", cfgFont, cfgBrush, box.X + box.Width - 95, box.Y + 4);

        // Barra de Medidor de Movimento (Calibração em tempo real)
        int meterX = box.X + 8;
        int meterY = box.Y + box.Height - 20;
        int meterW = box.Width - 16;
        int meterH = 12;

        using var meterBg = new SolidBrush(Color.FromArgb(50, 50, 50));
        g.FillRectangle(meterBg, meterX, meterY, meterW, meterH);

        // Nível atual de movimento
        float currentFill = (float)Math.Min(1.0, _currentMotionRatio / 0.35) * meterW;
        bool isTriggered = _currentMotionRatio >= _sensitivity;
        using var fillBrush = new SolidBrush(isTriggered ? Color.FromArgb(244, 67, 54) : Color.FromArgb(76, 175, 80));
        g.FillRectangle(fillBrush, meterX, meterY, currentFill, meterH);

        // Linha do Limiar de Sensibilidade (Threshold Line)
        float thresholdX = meterX + (float)Math.Min(1.0, _sensitivity / 0.35) * meterW;
        using var threshPen = new Pen(Color.Yellow, 2f);
        g.DrawLine(threshPen, thresholdX, meterY - 2, thresholdX, meterY + meterH + 2);
    }

    private void DrawStartOverlay(Graphics g)
    {
        using var dimBrush = new SolidBrush(Color.FromArgb(170, 255, 255, 255));
        g.FillRectangle(dimBrush, 0, 0, ClientSize.Width, ClientSize.Height);

        using var fontTitle = new Font("Segoe UI", 32, FontStyle.Bold);
        using var fontSub = new Font("Segoe UI", 16, FontStyle.Regular);
        using var fontHint = new Font("Segoe UI", 13, FontStyle.Bold);
        using var titleBrush = new SolidBrush(Color.FromArgb(40, 40, 40));
        using var actionBrush = new SolidBrush(Color.FromArgb(33, 150, 243));

        var sf = new StringFormat { Alignment = StringAlignment.Center };
        float midX = ClientSize.Width / 2f;
        float midY = ClientSize.Height / 2f - 40f;

        g.DrawString("DINO JUMP INTERATIVO", fontTitle, titleBrush, midX, midY - 60, sf);
        g.DrawString("Projeto Comunitário de Extensão", fontSub, titleBrush, midX, midY, sf);
        g.DrawString(">> PULE em frente à câmera ou aperte ESPAÇO para começar! <<", fontHint, actionBrush, midX, midY + 50, sf);
    }

    private void DrawGameOverOverlay(Graphics g)
    {
        using var dimBrush = new SolidBrush(Color.FromArgb(160, 255, 255, 255));
        g.FillRectangle(dimBrush, 0, 0, ClientSize.Width, ClientSize.Height);

        using var fontOver = new Font("Segoe UI", 30, FontStyle.Bold);
        using var fontSub = new Font("Segoe UI", 14, FontStyle.Regular);
        using var fontAction = new Font("Segoe UI", 14, FontStyle.Bold);
        using var overBrush = new SolidBrush(Color.FromArgb(211, 47, 47));
        using var textBrush = new SolidBrush(Color.FromArgb(60, 60, 60));
        using var actionBrush = new SolidBrush(Color.FromArgb(25, 118, 210));

        var sf = new StringFormat { Alignment = StringAlignment.Center };
        float midX = ClientSize.Width / 2f;
        float midY = ClientSize.Height / 2f - 40f;

        g.DrawString("G A M E   O V E R", fontOver, overBrush, midX, midY - 45, sf);
        g.DrawString($"Pontuação final: {_score:D5}   |   Pulos realizados: {_jumpsThisRound}", fontSub, textBrush, midX, midY + 15, sf);
        g.DrawString(">> Pule ou pressione ESPAÇO para jogar novamente! <<", fontAction, actionBrush, midX, midY + 55, sf);
    }

    // ==========================================
    // 7. ENTRADAS DO TECLADO E ATALHOS
    // ==========================================
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Up)
        {
            AcionarPulo();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.R)
        {
            ResetGame();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.D)
        {
            AbrirSelecaoDispositivos();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)
        {
            // Aumenta sensibilidade (precisa de menos movimento)
            _sensitivity = Math.Max(0.03, _sensitivity - 0.015);
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
        {
            // Diminui sensibilidade (precisa de mais movimento)
            _sensitivity = Math.Min(0.30, _sensitivity + 0.015);
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.V)
        {
            // Alterna visibilidade do painel da câmera
            _showCameraOverlay = !_showCameraOverlay;
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.F11)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);

        // Se clicar no painel da câmera, abre a tela de seleção de dispositivos
        if (_showCameraOverlay && _cameraBoxRect.Contains(e.Location))
        {
            AbrirSelecaoDispositivos();
        }
    }

    private void ToggleFullscreen()
    {
        if (!_isFullscreen)
        {
            _prevWindowState = WindowState;
            _prevBorderStyle = FormBorderStyle;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            _isFullscreen = true;
        }
        else
        {
            FormBorderStyle = _prevBorderStyle;
            WindowState = _prevWindowState;
            _isFullscreen = false;
        }
    }

    // ==========================================
    // 8. FINALIZAÇÃO E LIBERAÇÃO DE RECURSOS
    // ==========================================
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _gameTimer?.Stop();
        _gameTimer?.Dispose();

        try
        {
            _cameraCts?.Cancel();
            _cameraTask?.Wait(800);
        }
        catch
        {
            // Encerramento limpo
        }

        lock (_bitmapLock)
        {
            _webcamBitmap?.Dispose();
            _webcamBitmap = null;
        }

        base.OnFormClosing(e);
    }

    // ==========================================
    // CLASSES AUXILIARES DE RENDERIZAÇÃO
    // ==========================================
    private class Obstacle
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public int Type { get; set; }
    }

    private class Cloud
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Scale { get; set; }
        public float Speed { get; set; }
    }

    private class GroundDetail
    {
        public float X { get; set; }
        public float Width { get; set; }
    }

    private class DustParticle
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Size { get; set; }
        public float Life { get; set; }
    }
}
