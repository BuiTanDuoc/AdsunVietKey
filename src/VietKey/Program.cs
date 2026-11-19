using VietKey.App;

namespace VietKey;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        UiKit.Initialize();

        using var mutex = new Mutex(initiallyOwned: true, @"Local\VietKey.SingleInstance", out bool isFirst);
        if (!isFirst)
        {
            UiKit.Info("VietKey đang chạy (xem icon ở khay hệ thống).");
            return;
        }

        Application.Run(new TrayApp());
    }
}
