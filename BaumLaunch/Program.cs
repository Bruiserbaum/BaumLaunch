using System.Runtime.InteropServices;

namespace BaumLaunch;

static class Program
{
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        bool startMinimized = args.Contains("--startup");

        using var mutex = new Mutex(true, "BaumLaunch_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            // Already running — only bring window to front if this wasn't a silent startup launch
            if (!startMinimized)
            {
                var existing = System.Diagnostics.Process.GetProcessesByName("BaumLaunch")
                    .FirstOrDefault(p => p.Id != Environment.ProcessId);
                if (existing != null)
                {
                    ShowWindow(existing.MainWindowHandle, 9); // SW_RESTORE
                    SetForegroundWindow(existing.MainWindowHandle);
                }
            }
            return;
        }

        Application.Run(new MainForm(startMinimized));
    }
}
