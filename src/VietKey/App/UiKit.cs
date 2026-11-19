using Sunny.UI;

namespace AdsunVietKey.App;

/// <summary>Theme SunnyUI, font chung và các hộp thoại dùng trong AdsunVietKey.</summary>
internal static class UiKit
{
    /// <summary>Các theme cho người dùng chọn (Colorful = tự chọn màu chủ đạo).</summary>
    public static readonly UIStyle[] Themes =
    {
        UIStyle.Blue, UIStyle.Green, UIStyle.Orange, UIStyle.Red, UIStyle.Gray, UIStyle.Purple,
        UIStyle.LayuiGreen, UIStyle.LayuiRed, UIStyle.LayuiOrange,
        UIStyle.DarkBlue, UIStyle.Black, UIStyle.Dark,
        UIStyle.Colorful,
    };

    private static readonly List<UIContextMenuStrip> Menus = new();

    /// <summary>Gọi một lần lúc khởi động, trước khi tạo form.</summary>
    public static void Initialize()
    {
        // SunnyUI mặc định dùng chữ tiếng Trung cho nút hộp thoại (确定/取消) → chuyển sang tiếng Anh
        UIStyles.CultureInfo = CultureInfos.en_US;
    }

    public static string ThemeName(UIStyle style) => style switch
    {
        UIStyle.Blue => Lang.T("Xanh dương", "Blue"),
        UIStyle.Green => Lang.T("Xanh lá", "Green"),
        UIStyle.Orange => Lang.T("Cam", "Orange"),
        UIStyle.Red => Lang.T("Đỏ", "Red"),
        UIStyle.Gray => Lang.T("Xám", "Gray"),
        UIStyle.Purple => Lang.T("Tím", "Purple"),
        UIStyle.LayuiGreen => Lang.T("Ngọc lục bảo", "Emerald"),
        UIStyle.LayuiRed => Lang.T("Đỏ san hô", "Coral red"),
        UIStyle.LayuiOrange => Lang.T("Hổ phách", "Amber"),
        UIStyle.DarkBlue => Lang.T("Xanh đậm (tối)", "Dark blue"),
        UIStyle.Black => Lang.T("Đen (tối)", "Black"),
        UIStyle.Dark => Lang.T("Tối", "Dark"),
        UIStyle.Colorful => Lang.T("Tự chọn màu", "Custom color"),
        _ => style.ToString(),
    };

    /// <summary>Áp theme cho toàn bộ form SunnyUI đang mở và các menu đã đăng ký.</summary>
    public static void ApplyTheme(UIStyle theme, Color customColor)
    {
        if (theme == UIStyle.Colorful)
            UIStyles.InitColorful(customColor, ReadableForeColor(customColor));
        else
            UIStyles.SetStyle(theme);

        foreach (var menu in Menus)
            menu.SetInheritedStyle(CurrentStyle);
    }

    /// <summary>Menu không nằm trên form nên phải đăng ký để đổi màu theo theme.</summary>
    public static void RegisterMenu(UIContextMenuStrip menu)
    {
        Menus.Add(menu);
        menu.SetInheritedStyle(CurrentStyle);
    }

    public static UIStyle CurrentStyle => UIStyles.Style == UIStyle.Inherited ? UIStyle.Blue : UIStyles.Style;

    /// <summary>Chữ trắng trên nền đậm, chữ đen trên nền sáng.</summary>
    public static Color ReadableForeColor(Color back) =>
        (0.299 * back.R + 0.587 * back.G + 0.114 * back.B) > 170 ? Color.FromArgb(48, 48, 48) : Color.White;

    // ------------------------------------------------------------------ font & tỉ lệ

    /// <summary>Font hiển thị tốt tiếng Việt (SunnyUI mặc định dùng font Trung Quốc).</summary>
    public static Font NewFont(float size = 10.5f, FontStyle style = FontStyle.Regular) =>
        new("Segoe UI", size, style, GraphicsUnit.Point);

    /// <summary>Đặt font cho control và mọi control con.</summary>
    public static void ApplyFont(Control root, float size = 10.5f)
    {
        root.Font = NewFont(size);
        foreach (Control child in root.Controls)
            ApplyFont(child, size);
    }

    /// <summary>Hệ số DPI (100% = 1.0) để nhân toạ độ khi dựng giao diện bằng code.</summary>
    public static float Scale(Control c) => c.DeviceDpi / 96f;

    // ------------------------------------------------------------------ hộp thoại

    public static void Info(string message, string? title = null) =>
        UIMessageBox.Show(message, title ?? "AdsunVietKey", CurrentStyle, UIMessageBoxButtons.OK);

    public static void Error(string message, string? title = null) =>
        UIMessageBox.Show(message, title ?? "AdsunVietKey", UIStyle.Red, UIMessageBoxButtons.OK);
}
