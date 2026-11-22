using System;
using System.Windows.Forms;

namespace AppleNotesPortable
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Render natively at the display's scaling instead of being drawn at
            // 100% and stretched (which is what made text look fuzzy). Must run
            // before any window is created.
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WebViewForm());
        }
    }
}
