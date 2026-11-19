using VietKey.App;

namespace VietKey;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, @"Local\VietKey.SingleInstance", out bool isFirst);
        if (!isFirst)
        {
            MessageBox.Show("VietKey đang chạy (xem icon ở khay hệ thống).", "VietKey",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApp());
    }
}
