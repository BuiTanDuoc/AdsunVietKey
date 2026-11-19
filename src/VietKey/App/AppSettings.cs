using System.Reflection;
using Microsoft.Win32;
using Sunny.UI;
using AdsunVietKey.Core.Conversion;
using AdsunVietKey.Core.Engine;

namespace AdsunVietKey.App;

public enum ToggleHotkey
{
    CtrlShift = 0,
    AltZ = 1,
}

/// <summary>Cấu hình, lưu tại HKCU\Software\AdsunVietKey. Giá trị mặc định giống UniKey.</summary>
internal sealed class AppSettings
{
    private const string KeyPath = @"Software\AdsunVietKey";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "AdsunVietKey";

    // Điều khiển
    public bool Enabled { get; set; } = true;
    public VietCharset OutputCharset { get; set; } = VietCharset.Unicode;
    public InputMethod Method { get; set; } = InputMethod.Telex;
    public ToggleHotkey Hotkey { get; set; } = ToggleHotkey.CtrlShift;

    // Tuỳ chọn khác
    public bool FreeMarking { get; set; } = true;
    public bool ModernToneStyle { get; set; }
    public bool UseClipboardForUnicode { get; set; }
    public bool SpellCheck { get; set; } = true;
    public bool RestoreInvalidWords { get; set; } = true;
    public bool ShowFeedback { get; set; }
    public bool BeepOnToggle { get; set; } = true;

    // Gõ tắt
    public bool MacroEnabled { get; set; }
    public bool MacroWhenOff { get; set; }

    // Hệ thống
    public bool ShowPanelOnStartup { get; set; }
    public bool VietnameseInterface { get; set; } = true;

    // Giao diện (SunnyUI)
    public UIStyle Theme { get; set; } = UIStyle.Blue;
    public int ThemeColor { get; set; } = unchecked((int)0xFF00BEAC); // màu cho theme "Tuỳ chọn màu"

    // Công cụ chuyển mã
    public VietCharset ConvertFrom { get; set; } = VietCharset.Tcvn3;
    public VietCharset ConvertTo { get; set; } = VietCharset.Unicode;

    private static readonly PropertyInfo[] InstanceProperties =
        typeof(AppSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    /// <summary>Đưa mọi tuỳ chọn (trừ khởi động cùng Windows) về mặc định.</summary>
    public void ResetToDefaults()
    {
        var d = new AppSettings();
        foreach (var p in InstanceProperties)
            if (p.CanWrite && p.CanRead)
                p.SetValue(this, p.GetValue(d));
    }

    public static AppSettings Load()
    {
        var s = new AppSettings();
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            if (key is null)
                return s;

            foreach (var p in InstanceProperties)
            {
                if (!p.CanWrite || key.GetValue(p.Name) is not int raw)
                    continue;

                if (p.PropertyType == typeof(bool))
                    p.SetValue(s, raw != 0);
                else if (p.PropertyType == typeof(int))
                    p.SetValue(s, raw);
                else if (p.PropertyType.IsEnum && Enum.IsDefined(p.PropertyType, raw))
                    p.SetValue(s, Enum.ToObject(p.PropertyType, raw));
            }

            if (s.ConvertFrom == VietCharset.NoAccent)
                s.ConvertFrom = VietCharset.Tcvn3;
            if (s.OutputCharset == VietCharset.NoAccent)
                s.OutputCharset = VietCharset.Unicode;
        }
        catch (Exception)
        {
            // Registry lỗi thì dùng mặc định
        }
        return s;
    }

    public void Save()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
            foreach (var p in InstanceProperties)
            {
                object? value = p.GetValue(this);
                if (value is int iv)
                {
                    key.SetValue(p.Name, iv, RegistryValueKind.DWord);
                    continue;
                }
                int raw = value switch
                {
                    bool b => b ? 1 : 0,
                    int i => i,
                    Enum e => Convert.ToInt32(e),
                    _ => int.MinValue,
                };
                if (raw != int.MinValue)
                    key.SetValue(p.Name, raw, RegistryValueKind.DWord);
            }
        }
        catch (Exception)
        {
            // bỏ qua: không lưu được cấu hình không làm hỏng việc gõ
        }
    }

    /// <summary>Khởi động cùng Windows (HKCU\...\Run).</summary>
    public static bool RunAtStartup
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(RunValueName) is string;
        }
        set
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (value)
                key.SetValue(RunValueName, $"\"{Application.ExecutablePath}\"");
            else
                key.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
    }

    /// <summary>Thư mục dữ liệu: %AppData%\AdsunVietKey.</summary>
    public static string DataFolder
    {
        get
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AdsunVietKey");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}
