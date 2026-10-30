namespace DinoJumpComunitario;

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

static class Program
{
    // Caminho na pasta raiz do projeto
    public static readonly string LogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "crash_log.txt");

    // Caminho alternativo na pasta do executável
    public static readonly string BinaryLogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");

    [STAThread]
    static void Main()
    {
        InitLogFile();
        LogEvent("Aplicação Dino Jump iniciada.");

        // 1. Configura a captura de exceções não tratadas na interface gráfica (WinForms)
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        Application.ThreadException += (sender, args) =>
        {
            LogCrash("ThreadException (Interface)", args.Exception);
        };

        // 2. Configura a captura de exceções em threads em segundo plano / AppDomain
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                LogCrash("UnhandledException (AppDomain / Background)", ex);
            }
            else
            {
                LogEvent($"UnhandledException não gerenciada: {args.ExceptionObject}");
            }
        };

        // 3. Configura a captura de exceções não tratadas em Tasks assíncronas
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            LogCrash("UnobservedTaskException (Async Task)", args.Exception);
            args.SetObserved();
        };

        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());

        LogEvent("Aplicação encerrada normalmente.");
    }

    private static void InitLogFile()
    {
        try
        {
            if (!File.Exists(LogFilePath))
            {
                var sb = new StringBuilder();
                sb.AppendLine("================================================================================");
                sb.AppendLine("DINO JUMP INTERATIVO - REGISTRO DE EVENTOS E ERROS");
                sb.AppendLine($"Arquivo criado em: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Localização: {LogFilePath}");
                sb.AppendLine($"Sistema Operacional: {Environment.OSVersion}");
                sb.AppendLine($"Runtime .NET: {Environment.Version}");
                sb.AppendLine("================================================================================");
                sb.AppendLine();
                File.WriteAllText(LogFilePath, sb.ToString(), Encoding.UTF8);
            }
        }
        catch { }
    }

    /// <summary>
    /// Grava uma linha de evento ou diagnóstico no log em tempo real.
    /// </summary>
    public static void LogEvent(string message)
    {
        try
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
            File.AppendAllText(LogFilePath, line, Encoding.UTF8);

            if (BinaryLogFilePath != LogFilePath)
            {
                try { File.AppendAllText(BinaryLogFilePath, line, Encoding.UTF8); } catch { }
            }
        }
        catch { }
    }

    /// <summary>
    /// Grava detalhadamente a exceção em crash_log.txt e avisa o usuário na tela.
    /// </summary>
    public static void LogCrash(string source, Exception ex)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine($"[CRASH / ERRO] DATA E HORA: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"ORIGEM DA FALHA: {source}");
            sb.AppendLine($"TIPO DA EXCEÇÃO: {ex.GetType().FullName}");
            sb.AppendLine($"MENSAGEM: {ex.Message}");
            sb.AppendLine("STACK TRACE COMPLETO:");
            sb.AppendLine(ex.StackTrace ?? "(nenhum rastreamento disponível)");

            var inner = ex.InnerException;
            int depth = 1;
            while (inner != null)
            {
                sb.AppendLine($"--- INNER EXCEPTION ({depth++}) ---");
                sb.AppendLine($"TIPO: {inner.GetType().FullName}");
                sb.AppendLine($"MENSAGEM: {inner.Message}");
                sb.AppendLine(inner.StackTrace ?? "(nenhum rastreamento)");
                inner = inner.InnerException;
            }
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            string logContent = sb.ToString();
            File.AppendAllText(LogFilePath, logContent, Encoding.UTF8);

            if (BinaryLogFilePath != LogFilePath)
            {
                try { File.AppendAllText(BinaryLogFilePath, logContent, Encoding.UTF8); } catch { }
            }

            MessageBox.Show(
                $"Ocorreu um erro no Dino Jump:\n\n{ex.Message}\n\n" +
                $"Detalhes gravados em:\n{LogFilePath}",
                "Dino Jump - Erro Detectado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        catch { }
    }
}
