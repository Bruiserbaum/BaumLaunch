using System.Runtime.InteropServices;

namespace BaumLaunch;

static class Program
{
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        using var mutex = new Mutex(true, "BaumLaunch_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            var existing = System.Diagnostics.Process.GetProcessesByName("BaumLaunch")
                .FirstOrDefault(p => p.Id != Environment.ProcessId);
            if (existing != null)
            {
                ShowWindow(existing.MainWindowHandle, 9); // SW_RESTORE
                SetForegroundWindow(existing.MainWindowHandle);
            }
            return;
        }

        Application.Run(new MainForm());
    }
}
