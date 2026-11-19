using System.Diagnostics;
using Sunny.UI;
using VietKey.Core.Conversion;
using VietKey.Core.Engine;

namespace VietKey.App;

/// <summary>
/// Bảng điều khiển (SunnyUI) theo bố cục UniKey: phần "Điều khiển" luôn hiện,
/// nút "Mở rộng"/"Thu nhỏ" bật/tắt phần tuỳ chọn. Mọi thay đổi có hiệu lực ngay.
/// </summary>
internal sealed class ControlPanelForm : UIForm
{
    /// <summary>Thứ tự bảng mã trong ô "Bảng mã".</summary>
    public static readonly VietCharset[] Charsets =
        { VietCharset.Unicode, VietCharset.Tcvn3, VietCharset.VniWindows, VietCharset.UnicodeComposite };

    private const int LogicalWidth = 680, TopHeight = 165, ExtHeight = 426, BottomHeight = 72;

    private readonly AppSettings _settings;
    private readonly float _k;
    private bool _loading;
    private bool _expanded;

    private readonly Panel _top = new(), _ext = new(), _bottom = new();

    // Điều khiển
    private readonly UIGroupBox _gbControl;
    private readonly UILabel _lblCharset, _lblMethod, _lblHotkey;
    private readonly UIComboBox _cbCharset, _cbMethod;
    private readonly UIButton _btnCustomMethod;
    private readonly UIRadioButton _rbCtrlShift, _rbAltZ;
    private readonly UISymbolButton _btnClose, _btnExit, _btnExpand;

    // Tuỳ chọn khác
    private readonly UIGroupBox _gbOther;
    private readonly UICheckBox _cbFree, _cbModern, _cbClipboard, _cbSpell, _cbRestore, _cbFeedback, _cbBeep;

    // Gõ tắt
    private readonly UIGroupBox _gbMacro;
    private readonly UICheckBox _cbMacro, _cbMacroOff;
    private readonly UISymbolButton _btnMacroTable;

    // Hệ thống
    private readonly UIGroupBox _gbSystem;
    private readonly UICheckBox _cbShowOnStart, _cbStartup, _cbVnUi;

    // Giao diện
    private readonly UIGroupBox _gbTheme;
    private readonly UILabel _lblTheme, _lblColor;
    private readonly UIComboBox _cbTheme;
    private readonly UIColorPicker _colorPicker;

    // Hàng nút dưới
    private readonly UISymbolButton _btnHelp, _btnAbout, _btnDefault;
    private readonly UILinkLabel _lnkData;

    public event EventHandler? SettingsChanged;
    public event EventHandler? ExitRequested;
    public event EventHandler? EditMacrosRequested;

    public ControlPanelForm(AppSettings settings, Icon? icon)
    {
        _settings = settings;
        _k = UiKit.Scale(this);

        SuspendLayout();
        MaximizeBox = false;
        MinimizeBox = false;
        ShowTitleIcon = icon is not null;
        if (icon is not null) Icon = icon;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        EscClose = true;
        TitleHeight = S(36);

        // ---- phần trên
        _gbControl = Group(_top, 20, 8, 460, 148);
        _lblCharset = Lbl(_gbControl, 14, 40);
        _cbCharset = Combo(_gbControl, 138, 36, 290);
        _lblMethod = Lbl(_gbControl, 14, 78);
        _cbMethod = Combo(_gbControl, 138, 74, 228);
        _btnCustomMethod = new UIButton { Text = "···", Enabled = false };
        Place(_gbControl, _btnCustomMethod, 376, 74, 52, 29);
        _lblHotkey = Lbl(_gbControl, 14, 114);
        _rbCtrlShift = Radio(_gbControl, 138, 112, 150);
        _rbAltZ = Radio(_gbControl, 300, 112, 120);
        _rbCtrlShift.Text = "CTRL + SHIFT";
        _rbAltZ.Text = "ALT + Z";

        _btnClose = SymButton(_top, 500, 14, 150, 42, FontAwesomeIcons.fa_check);
        _btnExit = SymButton(_top, 500, 66, 150, 42, FontAwesomeIcons.fa_power_off);
        _btnExpand = SymButton(_top, 500, 118, 150, 42, FontAwesomeIcons.fa_angle_double_down);

        // ---- phần mở rộng
        _gbOther = Group(_ext, 20, 4, 640, 166);
        _cbFree = Check(_gbOther, 14, 34);
        _cbModern = Check(_gbOther, 14, 66);
        _cbClipboard = Check(_gbOther, 14, 98);
        _cbSpell = Check(_gbOther, 330, 34);
        _cbRestore = Check(_gbOther, 330, 66);
        _cbFeedback = Check(_gbOther, 330, 98);
        _cbBeep = Check(_gbOther, 330, 130);

        _gbMacro = Group(_ext, 20, 178, 320, 146);
        _cbMacro = Check(_gbMacro, 14, 34);
        _cbMacroOff = Check(_gbMacro, 14, 66);
        _btnMacroTable = SymButton(_gbMacro, 16, 100, 180, 34, FontAwesomeIcons.fa_list);

        _gbSystem = Group(_ext, 350, 178, 310, 146);
        _cbShowOnStart = Check(_gbSystem, 14, 34);
        _cbStartup = Check(_gbSystem, 14, 66);
        _cbVnUi = Check(_gbSystem, 14, 98);
        _cbVnUi.Text = "Vietnamese interface";

        _gbTheme = Group(_ext, 20, 332, 640, 86);
        _lblTheme = Lbl(_gbTheme, 14, 44);
        _cbTheme = Combo(_gbTheme, 110, 40, 250);
        _lblColor = Lbl(_gbTheme, 384, 44);
        _colorPicker = new UIColorPicker();
        Place(_gbTheme, _colorPicker, 460, 40, 160, 29);

        // ---- hàng nút dưới
        _btnHelp = SymButton(_bottom, 26, 14, 140, 44, FontAwesomeIcons.fa_book);
        _btnAbout = SymButton(_bottom, 182, 14, 140, 44, FontAwesomeIcons.fa_info_circle);
        _btnDefault = SymButton(_bottom, 338, 14, 140, 44, FontAwesomeIcons.fa_refresh);
        _lnkData = new UILinkLabel { AutoSize = true };
        Place(_bottom, _lnkData, 500, 28, 150, 24);

        Controls.Add(_top);
        Controls.Add(_ext);
        Controls.Add(_bottom);
        _ext.Visible = false;

        UiKit.ApplyFont(this);
        TitleFont = UiKit.NewFont(11f);
        foreach (var b in new[] { _btnClose, _btnExit, _btnExpand, _btnHelp, _btnAbout, _btnDefault, _btnMacroTable })
            b.SymbolSize = S(20);

        WireEvents();
        ApplyTexts();
        LoadValues();
        Relayout();
        ResumeLayout(true);
    }

    // ------------------------------------------------------------ dựng control

    private int S(int logical) => (int)Math.Round(logical * _k);

    private void Place(Control parent, Control c, int x, int y, int w, int h)
    {
        c.SetBounds(S(x), S(y), S(w), S(h));
        parent.Controls.Add(c);
    }

    private UIGroupBox Group(Control parent, int x, int y, int w, int h)
    {
        var g = new UIGroupBox();
        Place(parent, g, x, y, w, h);
        return g;
    }

    private UILabel Lbl(Control parent, int x, int y)
    {
        var l = new UILabel { AutoSize = true, BackColor = Color.Transparent };
        Place(parent, l, x, y, 110, 24);
        return l;
    }

    private UIComboBox Combo(Control parent, int x, int y, int w)
    {
        var c = new UIComboBox { DropDownStyle = UIDropDownStyle.DropDownList };
        Place(parent, c, x, y, w, 29);
        return c;
    }

    private UICheckBox Check(Control parent, int x, int y)
    {
        var c = new UICheckBox { AutoSize = true };
        Place(parent, c, x, y, 290, 29);
        return c;
    }

    private UIRadioButton Radio(Control parent, int x, int y, int w)
    {
        var r = new UIRadioButton();
        Place(parent, r, x, y, w, 29);
        return r;
    }

    private UISymbolButton SymButton(Control parent, int x, int y, int w, int h, int symbol)
    {
        var b = new UISymbolButton { Symbol = symbol };
        Place(parent, b, x, y, w, h);
        return b;
    }

    /// <summary>Xếp ba phần theo chiều dọc và đặt lại kích thước form (thu nhỏ / mở rộng).</summary>
    private void Relayout()
    {
        int y = TitleHeight;
        _top.SetBounds(0, y, S(LogicalWidth), S(TopHeight));
        y += S(TopHeight);
        _ext.Visible = _expanded;
        if (_expanded)
        {
            _ext.SetBounds(0, y, S(LogicalWidth), S(ExtHeight));
            y += S(ExtHeight);
        }
        _bottom.SetBounds(0, y, S(LogicalWidth), S(BottomHeight));
        y += S(BottomHeight);
        ClientSize = new Size(S(LogicalWidth), y);
    }

    // ------------------------------------------------------------ chữ hiển thị

    /// <summary>Gán chữ theo ngôn ngữ giao diện (gọi lại khi đổi "Vietnamese interface").</summary>
    public void ApplyTexts()
    {
        Text = $"VietKey {Application.ProductVersion.Split('+')[0]}";
        _gbControl.Text = Lang.T("Điều khiển", "Control");
        _lblCharset.Text = Lang.T("Bảng mã:", "Charset:");
        _lblMethod.Text = Lang.T("Kiểu gõ:", "Input method:");
        _lblHotkey.Text = Lang.T("Phím chuyển:", "Switch key:");
        _btnClose.Text = Lang.T("Đóng", "Close");
        _btnExit.Text = Lang.T("Kết thúc", "Exit");
        UpdateExpandButton();

        _gbOther.Text = Lang.T("Tùy chọn khác", "Other options");
        _cbFree.Text = Lang.T("Cho phép gõ tự do", "Free marking");
        _cbModern.Text = Lang.T("Đặt dấu oà, uý (thay vì òa, úy)", "Use oà, uý (instead of òa, úy)");
        _cbClipboard.Text = Lang.T("Luôn sử dụng clipboard cho unicode", "Always use clipboard for Unicode");
        _cbSpell.Text = Lang.T("Bật kiểm tra chính tả", "Enable spell checking");
        _cbRestore.Text = Lang.T("Tự động khôi phục phím với từ sai", "Auto restore keys for invalid words");
        _cbFeedback.Text = Lang.T("Hiện thông báo phản hồi", "Show feedback messages");
        _cbBeep.Text = Lang.T("Bật chuông khi chuyển ngôn ngữ", "Beep when switching language");

        _gbMacro.Text = Lang.T("Tùy chọn gõ tắt", "Macro options");
        _cbMacro.Text = Lang.T("Cho phép gõ tắt", "Enable macros");
        _cbMacroOff.Text = Lang.T("Cho phép gõ tắt cả khi tắt tiếng Việt", "Macros even in English mode");
        _btnMacroTable.Text = Lang.T("Bảng gõ tắt...", "Macro table...");

        _gbSystem.Text = Lang.T("Hệ thống", "System");
        _cbShowOnStart.Text = Lang.T("Bật hội thoại này khi khởi động", "Show this dialog at startup");
        _cbStartup.Text = Lang.T("Khởi động cùng Windows", "Run at Windows startup");

        _gbTheme.Text = Lang.T("Giao diện", "Appearance");
        _lblTheme.Text = Lang.T("Theme:", "Theme:");
        _lblColor.Text = Lang.T("Màu:", "Color:");

        _btnHelp.Text = Lang.T("Hướng dẫn", "Help");
        _btnAbout.Text = Lang.T("Thông tin", "About");
        _btnDefault.Text = Lang.T("Mặc định", "Default");
        _lnkData.Text = Lang.T("Thư mục dữ liệu", "Data folder");

        bool wasLoading = _loading;
        _loading = true;
        int charset = _cbCharset.SelectedIndex, method = _cbMethod.SelectedIndex, theme = _cbTheme.SelectedIndex;
        _cbCharset.Items.Clear();
        foreach (var c in Charsets)
            _cbCharset.Items.Add(CharsetName(c));
        _cbMethod.Items.Clear();
        _cbMethod.Items.AddRange(new object[] { "Telex", "VNI" });
        _cbTheme.Items.Clear();
        foreach (var t in UiKit.Themes)
            _cbTheme.Items.Add(UiKit.ThemeName(t));
        _cbCharset.SelectedIndex = charset;
        _cbMethod.SelectedIndex = method;
        _cbTheme.SelectedIndex = theme;
        _loading = wasLoading;
    }

    public static string CharsetName(VietCharset c) => c switch
    {
        VietCharset.Unicode => Lang.T("Unicode dựng sẵn", "Unicode (precomposed)"),
        VietCharset.UnicodeComposite => Lang.T("Unicode tổ hợp", "Unicode (composite)"),
        VietCharset.NoAccent => Lang.T("Không dấu", "No accents"),
        _ => VietConverter.DisplayName(c),
    };

    private void UpdateExpandButton()
    {
        _btnExpand.Text = _expanded ? Lang.T("Thu nhỏ", "Less") : Lang.T("Mở rộng", "More");
        _btnExpand.Symbol = _expanded ? FontAwesomeIcons.fa_angle_double_up : FontAwesomeIcons.fa_angle_double_down;
    }

    // ------------------------------------------------------------ giá trị

    private void LoadValues()
    {
        _loading = true;
        _cbCharset.SelectedIndex = Math.Max(0, Array.IndexOf(Charsets, _settings.OutputCharset));
        _cbMethod.SelectedIndex = (int)_settings.Method;
        _rbCtrlShift.Checked = _settings.Hotkey == ToggleHotkey.CtrlShift;
        _rbAltZ.Checked = _settings.Hotkey == ToggleHotkey.AltZ;

        _cbFree.Checked = _settings.FreeMarking;
        _cbModern.Checked = _settings.ModernToneStyle;
        _cbClipboard.Checked = _settings.UseClipboardForUnicode;
        _cbSpell.Checked = _settings.SpellCheck;
        _cbRestore.Checked = _settings.RestoreInvalidWords;
        _cbRestore.Enabled = _settings.SpellCheck;
        _cbFeedback.Checked = _settings.ShowFeedback;
        _cbBeep.Checked = _settings.BeepOnToggle;

        _cbMacro.Checked = _settings.MacroEnabled;
        _cbMacroOff.Checked = _settings.MacroWhenOff;
        _cbMacroOff.Enabled = _settings.MacroEnabled;

        _cbShowOnStart.Checked = _settings.ShowPanelOnStartup;
        _cbStartup.Checked = SafeRunAtStartup();
        _cbVnUi.Checked = _settings.VietnameseInterface;

        _cbTheme.SelectedIndex = Math.Max(0, Array.IndexOf(UiKit.Themes, _settings.Theme));
        _colorPicker.Value = Color.FromArgb(_settings.ThemeColor);
        _colorPicker.Enabled = _settings.Theme == UIStyle.Colorful;
        _loading = false;
    }

    /// <summary>Đọc lại cấu hình (khi thay đổi từ menu khay hệ thống).</summary>
    public void RefreshValues() => LoadValues();

    private void SaveValues()
    {
        if (_loading)
            return;

        _settings.OutputCharset = Charsets[Math.Max(0, _cbCharset.SelectedIndex)];
        _settings.Method = (InputMethod)Math.Max(0, _cbMethod.SelectedIndex);
        _settings.Hotkey = _rbAltZ.Checked ? ToggleHotkey.AltZ : ToggleHotkey.CtrlShift;

        _settings.FreeMarking = _cbFree.Checked;
        _settings.ModernToneStyle = _cbModern.Checked;
        _settings.UseClipboardForUnicode = _cbClipboard.Checked;
        _settings.SpellCheck = _cbSpell.Checked;
        _settings.RestoreInvalidWords = _cbRestore.Checked;
        _settings.ShowFeedback = _cbFeedback.Checked;
        _settings.BeepOnToggle = _cbBeep.Checked;

        _settings.MacroEnabled = _cbMacro.Checked;
        _settings.MacroWhenOff = _cbMacroOff.Checked;

        _settings.ShowPanelOnStartup = _cbShowOnStart.Checked;
        bool languageChanged = _settings.VietnameseInterface != _cbVnUi.Checked;
        _settings.VietnameseInterface = _cbVnUi.Checked;

        _settings.Theme = UiKit.Themes[Math.Max(0, _cbTheme.SelectedIndex)];
        _settings.ThemeColor = _colorPicker.Value.ToArgb();

        _cbRestore.Enabled = _cbSpell.Checked;
        _cbMacroOff.Enabled = _cbMacro.Checked;
        _colorPicker.Enabled = _settings.Theme == UIStyle.Colorful;

        if (languageChanged)
        {
            Lang.Vietnamese = _settings.VietnameseInterface;
            ApplyTexts();
        }
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static bool SafeRunAtStartup()
    {
        try { return AppSettings.RunAtStartup; }
        catch (Exception) { return false; }
    }

    // ------------------------------------------------------------ sự kiện

    private void WireEvents()
    {
        foreach (var cb in new[] { _cbCharset, _cbMethod, _cbTheme })
            cb.SelectedIndexChanged += (_, _) => SaveValues();
        foreach (var rb in new[] { _rbCtrlShift, _rbAltZ })
            rb.CheckedChanged += (_, _) => { if (rb.Checked) SaveValues(); };
        foreach (var c in new[] { _cbFree, _cbModern, _cbClipboard, _cbSpell, _cbRestore, _cbFeedback, _cbBeep, _cbMacro, _cbMacroOff, _cbShowOnStart, _cbVnUi })
            c.CheckedChanged += (_, _) => SaveValues();
        _colorPicker.ValueChanged += (_, _) => SaveValues();

        _cbStartup.CheckedChanged += (_, _) =>
        {
            if (_loading) return;
            try
            {
                AppSettings.RunAtStartup = _cbStartup.Checked;
            }
            catch (Exception ex)
            {
                UiKit.Error(ex.Message);
            }
        };

        _btnClose.Click += (_, _) => Close();
        _btnExit.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        _btnExpand.Click += (_, _) =>
        {
            _expanded = !_expanded;
            UpdateExpandButton();
            Relayout();
        };
        _btnMacroTable.Click += (_, _) => EditMacrosRequested?.Invoke(this, EventArgs.Empty);
        _btnHelp.Click += (_, _) => ShowHelp();
        _btnAbout.Click += (_, _) => ShowAbout();
        _btnDefault.Click += (_, _) =>
        {
            _settings.ResetToDefaults();
            Lang.Vietnamese = _settings.VietnameseInterface;
            ApplyTexts();
            LoadValues();
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        };
        _lnkData.LinkClicked += (_, _) =>
            Process.Start(new ProcessStartInfo(AppSettings.DataFolder) { UseShellExecute = true });
    }

    // ------------------------------------------------------------ hướng dẫn / thông tin

    public static void ShowHelp()
    {
        string text = Lang.T(
            "TELEX   s=sắc  f=huyền  r=hỏi  x=ngã  j=nặng  z=xoá dấu\r\n" +
            "        aa=â  ee=ê  oo=ô  aw=ă  ow=ơ  uw=ư  uow=ươ  w=ư  dd=đ\r\n\r\n" +
            "VNI     1=sắc  2=huyền  3=hỏi  4=ngã  5=nặng  0=xoá dấu\r\n" +
            "        6=â ê ô  7=ơ ư  8=ă  9=đ\r\n\r\n" +
            "Gõ lại phím dấu để huỷ (ass → as).\r\n\r\n" +
            "PHÍM TẮT\r\n" +
            "  Ctrl+Shift (hoặc Alt+Z)  chuyển Việt/Anh\r\n" +
            "  Ctrl+Shift+F5            Bảng điều khiển\r\n" +
            "  Ctrl+Shift+F6            Công cụ chuyển mã\r\n" +
            "  Ctrl+Shift+F9            Chuyển mã nhanh clipboard\r\n\r\n" +
            "Bấm chuột trái vào biểu tượng V/E ở khay hệ thống để chuyển Việt/Anh.\r\n" +
            "Theme và màu giao diện: Bảng điều khiển → Mở rộng → Giao diện.",
            "TELEX   s=acute  f=grave  r=hook  x=tilde  j=dot  z=remove tone\r\n" +
            "        aa=â  ee=ê  oo=ô  aw=ă  ow=ơ  uw=ư  uow=ươ  w=ư  dd=đ\r\n\r\n" +
            "VNI     1=acute  2=grave  3=hook  4=tilde  5=dot  0=remove tone\r\n" +
            "        6=â ê ô  7=ơ ư  8=ă  9=đ\r\n\r\n" +
            "Type a mark key twice to undo it (ass → as).\r\n\r\n" +
            "HOTKEYS\r\n" +
            "  Ctrl+Shift (or Alt+Z)    switch Vietnamese/English\r\n" +
            "  Ctrl+Shift+F5            Control panel\r\n" +
            "  Ctrl+Shift+F6            Converter toolkit\r\n" +
            "  Ctrl+Shift+F9            Quick clipboard conversion\r\n\r\n" +
            "Left-click the V/E tray icon to switch Vietnamese/English.\r\n" +
            "Theme and color: Control panel → More → Appearance.");
        new TextViewForm(Lang.T("VietKey – Hướng dẫn", "VietKey – Help"), text).Show();
    }

    public static void ShowAbout()
    {
        string version = Application.ProductVersion.Split('+')[0];
        UiKit.Info(
            Lang.T($"VietKey {version}\nBộ gõ tiếng Việt Telex/VNI viết bằng C# .NET 8 WinForms, giao diện SunnyUI.\nBố cục theo UniKey.",
                   $"VietKey {version}\nVietnamese Telex/VNI input method in C# .NET 8 WinForms with SunnyUI.\nLayout modelled on UniKey."),
            Lang.T("Thông tin", "About"));
    }
}

/// <summary>Cửa sổ hiển thị văn bản chỉ đọc (Hướng dẫn).</summary>
internal sealed class TextViewForm : UIForm
{
    public TextViewForm(string title, string text)
    {
        float k = UiKit.Scale(this);
        Text = title;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        EscClose = true;
        TitleHeight = (int)(36 * k);
        ClientSize = new Size((int)(640 * k), (int)(470 * k));

        var box = new UITextBox
        {
            Multiline = true,
            ReadOnly = true,
            ShowScrollBar = true,
            Dock = DockStyle.Fill,
            Text = text,
        };
        var host = new Panel { Dock = DockStyle.Fill, Padding = new Padding((int)(12 * k)) };
        host.Controls.Add(box);
        Controls.Add(host);

        TitleFont = UiKit.NewFont(11f);
        box.Font = new Font("Consolas", 10.5f);
    }
}
