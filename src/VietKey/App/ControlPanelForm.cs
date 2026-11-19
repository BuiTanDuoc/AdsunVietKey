using System.Diagnostics;
using VietKey.Core.Conversion;
using VietKey.Core.Engine;

namespace VietKey.App;

/// <summary>
/// Bảng điều khiển theo bố cục UniKey: phần "Điều khiển" luôn hiện,
/// nút "Mở rộng"/"Thu nhỏ" bật/tắt phần tuỳ chọn. Mọi thay đổi có hiệu lực ngay.
/// </summary>
internal sealed class ControlPanelForm : Form
{
    /// <summary>Thứ tự bảng mã trong ô "Bảng mã".</summary>
    public static readonly VietCharset[] Charsets =
        { VietCharset.Unicode, VietCharset.Tcvn3, VietCharset.VniWindows, VietCharset.UnicodeComposite };

    private readonly AppSettings _settings;
    private bool _loading;
    private bool _expanded;

    // Điều khiển
    private readonly GroupBox _gbControl = Group(20, 8, 460, 145);
    private readonly Label _lblCharset = Lbl(14, 30);
    private readonly ComboBox _cbCharset = Combo(138, 26, 280);
    private readonly Label _lblMethod = Lbl(14, 70);
    private readonly ComboBox _cbMethod = Combo(138, 66, 220);
    private readonly Button _btnCustomMethod = new() { Text = "···", Location = new Point(372, 65), Size = new Size(44, 27), Enabled = false };
    private readonly Label _lblHotkey = Lbl(14, 108);
    private readonly RadioButton _rbCtrlShift = new() { Text = "CTRL + SHIFT", Location = new Point(138, 106), AutoSize = true };
    private readonly RadioButton _rbAltZ = new() { Text = "ALT + Z", Location = new Point(296, 106), AutoSize = true };

    private readonly Button _btnClose = Big(500, 14);
    private readonly Button _btnExit = Big(500, 72);
    private readonly Button _btnExpand = Big(500, 130);

    // Tuỳ chọn khác
    private readonly GroupBox _gbOther = Group(20, 4, 640, 160);
    private readonly CheckBox _cbFree = Check(14, 26);
    private readonly CheckBox _cbModern = Check(14, 58);
    private readonly CheckBox _cbClipboard = Check(14, 90);
    private readonly CheckBox _cbSpell = Check(340, 26);
    private readonly CheckBox _cbRestore = Check(340, 58);
    private readonly CheckBox _cbFeedback = Check(340, 90);
    private readonly CheckBox _cbBeep = Check(340, 122);

    // Gõ tắt
    private readonly GroupBox _gbMacro = Group(20, 174, 320, 140);
    private readonly CheckBox _cbMacro = Check(14, 26);
    private readonly CheckBox _cbMacroOff = Check(14, 56);
    private readonly Button _btnMacroTable = new() { Location = new Point(16, 92), Size = new Size(160, 34) };

    // Hệ thống
    private readonly GroupBox _gbSystem = Group(350, 174, 310, 140);
    private readonly CheckBox _cbShowOnStart = Check(14, 26);
    private readonly CheckBox _cbStartup = Check(14, 56);
    private readonly CheckBox _cbVnUi = new() { Text = "Vietnamese interface", Location = new Point(14, 86), AutoSize = true };

    // Hàng nút dưới
    private readonly Button _btnHelp = BottomButton(26);
    private readonly Button _btnAbout = BottomButton(184);
    private readonly Button _btnDefault = BottomButton(342);
    private readonly LinkLabel _lnkData = new() { Location = new Point(496, 27), AutoSize = true };

    private readonly Panel _extPanel;

    public event EventHandler? SettingsChanged;
    public event EventHandler? ExitRequested;
    public event EventHandler? EditMacrosRequested;

    public ControlPanelForm(AppSettings settings, Icon? icon)
    {
        _settings = settings;

        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10f);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ShowInTaskbar = true;
        if (icon is not null) Icon = icon;

        // ---- phần trên
        _gbControl.Controls.AddRange(new Control[]
        {
            _lblCharset, _cbCharset, _lblMethod, _cbMethod, _btnCustomMethod, _lblHotkey, _rbCtrlShift, _rbAltZ,
        });
        var topPanel = PanelOf(165, _gbControl, _btnClose, _btnExit, _btnExpand);

        // ---- phần mở rộng
        _gbOther.Controls.AddRange(new Control[] { _cbFree, _cbModern, _cbClipboard, _cbSpell, _cbRestore, _cbFeedback, _cbBeep });
        _gbMacro.Controls.AddRange(new Control[] { _cbMacro, _cbMacroOff, _btnMacroTable });
        _gbSystem.Controls.AddRange(new Control[] { _cbShowOnStart, _cbStartup, _cbVnUi });
        _extPanel = PanelOf(324, _gbOther, _gbMacro, _gbSystem);
        _extPanel.Visible = false;

        // ---- hàng nút dưới
        var bottomPanel = PanelOf(72, _btnHelp, _btnAbout, _btnDefault, _lnkData);

        var stack = new TableLayoutPanel
        {
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        stack.Controls.Add(topPanel);
        stack.Controls.Add(_extPanel);
        stack.Controls.Add(bottomPanel);
        Controls.Add(stack);

        WireEvents();
        ApplyTexts();
        LoadValues();
        ResumeLayout(true);
    }

    // ------------------------------------------------------------ dựng control

    private static GroupBox Group(int x, int y, int w, int h) => new() { Location = new Point(x, y), Size = new Size(w, h) };
    private static Label Lbl(int x, int y) => new() { Location = new Point(x, y), AutoSize = true };
    private static ComboBox Combo(int x, int y, int w) => new() { Location = new Point(x, y), Width = w, DropDownStyle = ComboBoxStyle.DropDownList };
    private static CheckBox Check(int x, int y) => new() { Location = new Point(x, y), AutoSize = true };
    private static Button Big(int x, int y) => new() { Location = new Point(x, y), Size = new Size(122, 42) };
    private static Button BottomButton(int x) => new() { Location = new Point(x, 12), Size = new Size(138, 46) };

    private static Panel PanelOf(int height, params Control[] children)
    {
        var panel = new Panel { Size = new Size(680, height), Margin = Padding.Empty };
        panel.Controls.AddRange(children);
        return panel;
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
        _btnClose.Text = Lang.T("✔  Đóng", "✔  Close");
        _btnExit.Text = Lang.T("⏻  Kết thúc", "⏻  Exit");
        UpdateExpandText();

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

        _btnHelp.Text = Lang.T("📖  Hướng dẫn", "📖  Help");
        _btnAbout.Text = Lang.T("Thông tin", "About");
        _btnDefault.Text = Lang.T("Mặc định", "Default");
        _lnkData.Text = Lang.T("Thư mục dữ liệu", "Data folder");

        int selectedCharset = _cbCharset.SelectedIndex, selectedMethod = _cbMethod.SelectedIndex;
        bool wasLoading = _loading;
        _loading = true;
        _cbCharset.Items.Clear();
        foreach (var c in Charsets)
            _cbCharset.Items.Add(CharsetName(c));
        _cbMethod.Items.Clear();
        _cbMethod.Items.AddRange(new object[] { "Telex", "VNI" });
        _cbCharset.SelectedIndex = selectedCharset;
        _cbMethod.SelectedIndex = selectedMethod;
        _loading = wasLoading;
    }

    public static string CharsetName(VietCharset c) => c switch
    {
        VietCharset.Unicode => Lang.T("Unicode dựng sẵn", "Unicode (precomposed)"),
        VietCharset.UnicodeComposite => Lang.T("Unicode tổ hợp", "Unicode (composite)"),
        VietCharset.NoAccent => Lang.T("Không dấu", "No accents"),
        _ => VietConverter.DisplayName(c),
    };

    private void UpdateExpandText() =>
        _btnExpand.Text = _expanded ? Lang.T("⇧  Thu nhỏ", "⇧  Less") : Lang.T("⇩  Mở rộng", "⇩  More");

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

        _cbRestore.Enabled = _cbSpell.Checked;
        _cbMacroOff.Enabled = _cbMacro.Checked;

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
        foreach (var cb in new[] { _cbCharset, _cbMethod })
            cb.SelectedIndexChanged += (_, _) => SaveValues();
        foreach (var rb in new[] { _rbCtrlShift, _rbAltZ })
            rb.CheckedChanged += (_, _) => { if (((RadioButton)rb).Checked) SaveValues(); };
        foreach (var c in new[] { _cbFree, _cbModern, _cbClipboard, _cbSpell, _cbRestore, _cbFeedback, _cbBeep, _cbMacro, _cbMacroOff, _cbShowOnStart, _cbVnUi })
            c.CheckedChanged += (_, _) => SaveValues();

        _cbStartup.CheckedChanged += (_, _) =>
        {
            if (_loading) return;
            try
            {
                AppSettings.RunAtStartup = _cbStartup.Checked;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        _btnClose.Click += (_, _) => Close();
        _btnExit.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        _btnExpand.Click += (_, _) =>
        {
            _expanded = !_expanded;
            _extPanel.Visible = _expanded;
            UpdateExpandText();
        };
        _btnMacroTable.Click += (_, _) => EditMacrosRequested?.Invoke(this, EventArgs.Empty);
        _btnHelp.Click += (_, _) => ShowHelp(this);
        _btnAbout.Click += (_, _) => ShowAbout(this);
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

    public static void ShowHelp(IWin32Window? owner)
    {
        string text = Lang.T(
            "TELEX: s=sắc  f=huyền  r=hỏi  x=ngã  j=nặng  z=xoá dấu\n" +
            "       aa=â  ee=ê  oo=ô  aw=ă  ow=ơ  uw=ư  uow=ươ  w=ư  dd=đ\n\n" +
            "VNI:   1=sắc  2=huyền  3=hỏi  4=ngã  5=nặng  0=xoá dấu\n" +
            "       6=â ê ô  7=ơ ư  8=ă  9=đ\n\n" +
            "Gõ lại phím dấu để huỷ (ass → as).\n\n" +
            "Phím tắt:\n" +
            "  Ctrl+Shift (hoặc Alt+Z)   chuyển Việt/Anh\n" +
            "  Ctrl+Shift+F5             Bảng điều khiển\n" +
            "  Ctrl+Shift+F6             Công cụ chuyển mã\n" +
            "  Ctrl+Shift+F9             Chuyển mã nhanh clipboard\n\n" +
            "Bấm chuột trái vào biểu tượng V/E ở khay hệ thống để chuyển Việt/Anh.",
            "TELEX: s=acute  f=grave  r=hook  x=tilde  j=dot  z=remove tone\n" +
            "       aa=â  ee=ê  oo=ô  aw=ă  ow=ơ  uw=ư  uow=ươ  w=ư  dd=đ\n\n" +
            "VNI:   1=acute  2=grave  3=hook  4=tilde  5=dot  0=remove tone\n" +
            "       6=â ê ô  7=ơ ư  8=ă  9=đ\n\n" +
            "Type a mark key twice to undo it (ass → as).\n\n" +
            "Hotkeys:\n" +
            "  Ctrl+Shift (or Alt+Z)     switch Vietnamese/English\n" +
            "  Ctrl+Shift+F5             Control panel\n" +
            "  Ctrl+Shift+F6             Converter toolkit\n" +
            "  Ctrl+Shift+F9             Quick clipboard conversion\n\n" +
            "Left-click the V/E tray icon to switch Vietnamese/English.");
        MessageBox.Show(owner, text, Lang.T("VietKey – Hướng dẫn", "VietKey – Help"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public static void ShowAbout(IWin32Window? owner)
    {
        string version = Application.ProductVersion.Split('+')[0];
        MessageBox.Show(owner,
            Lang.T($"VietKey {version}\nBộ gõ tiếng Việt Telex/VNI viết bằng C# .NET 8 WinForms.\nBố cục giao diện theo UniKey.",
                   $"VietKey {version}\nVietnamese Telex/VNI input method written in C# .NET 8 WinForms.\nUI layout modelled on UniKey."),
            Lang.T("Thông tin", "About"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
