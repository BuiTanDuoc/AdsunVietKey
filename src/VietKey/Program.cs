using AdsunVietKey.App;

namespace AdsunVietKey;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        UiKit.Initialize();

        using var mutex = new Mutex(initiallyOwned: true, @"Local\AdsunVietKey.SingleInstance", out bool isFirst);
        if (!isFirst)
        {
            UiKit.Info("AdsunVietKey đang chạy (xem icon ở khay hệ thống).");
            return;
        }

        Application.Run(new TrayApp());
    }
}
