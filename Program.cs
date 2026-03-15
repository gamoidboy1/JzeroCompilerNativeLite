using System;
using System.IO;
using System.Windows.Forms;

namespace JzeroCompilerNativeLite
{
    internal static class Program
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JzeroCompilerNativeLite");
        private static readonly string CrashLogPath = Path.Combine(LogDirectory, "startup-error.log");

        [STAThread]
        private static void Main()
        {
            Application.ThreadException += ApplicationThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                WriteCrashLog(ex);
                MessageBox.Show(
                    "JzeroCompilerNativeLite failed to start.\r\n\r\n" + ex,
                    "Startup error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void ApplicationThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            WriteCrashLog(e.Exception);
            MessageBox.Show(
                "A UI error occurred.\r\n\r\n" + e.Exception,
                "Application error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            WriteCrashLog(exception ?? new Exception("Unknown unhandled exception"));
        }

        private static void WriteCrashLog(Exception ex)
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
                File.AppendAllText(
                    CrashLogPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                    ex + Environment.NewLine +
                    "------------------------" + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
