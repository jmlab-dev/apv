using System.Text;
using AdrenalinProfileViewer.Services;
using AdrenalinProfileViewer.UI;

namespace AdrenalinProfileViewer;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        NativeThemeHelper.InitializeApplication();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, eventArgs) => ReportFatalError(eventArgs.Exception, "UI thread");

        try
        {
            PortablePaths.Initialize();
            Directory.SetCurrentDirectory(PortablePaths.ApplicationDirectory);
            Application.Run(new MainForm(args));
        }
        catch (Exception ex)
        {
            ReportFatalError(ex, "application startup");
        }
    }

    private static void ReportFatalError(Exception exception, string context)
    {
        string? logPath = null;

        try
        {
            Directory.CreateDirectory(PortablePaths.LogsDirectory);
            logPath = PortablePaths.CrashLogPath;

            var report = new StringBuilder()
                .AppendLine(new string('=', 72))
                .AppendLine($"Time: {DateTimeOffset.Now:O}")
                .AppendLine($"Context: {context}")
                .AppendLine($"Application: {Application.ProductName} {Application.ProductVersion}")
                .AppendLine($"Executable directory: {PortablePaths.ApplicationDirectory}")
                .AppendLine($"OS: {Environment.OSVersion}")
                .AppendLine($"Runtime: {Environment.Version}")
                .AppendLine()
                .AppendLine(exception.ToString())
                .ToString();

            File.AppendAllText(logPath, report, Encoding.UTF8);
        }
        catch
        {
            // Error reporting must never hide the original exception.
        }

        var logMessage = logPath is null
            ? "\n\nThe portable application folder is not writable, so no crash log could be created."
            : $"\n\nA diagnostic log was written beside the executable at:\n{logPath}";

        MessageBox.Show(
            $"AMD Adrenalin Profile Viewer encountered an error during {context}.\n\n{exception.Message}{logMessage}",
            "AMD Adrenalin Profile Viewer",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
