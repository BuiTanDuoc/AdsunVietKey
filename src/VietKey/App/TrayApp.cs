using System.Media;
using Sunny.UI;
using System.Text;
using VietKey.Core;
using VietKey.Core.Conversion;
using VietKey.Core.Engine;
using VietKey.Core.Macros;
using VietKey.Input;

namespace VietKey.App;

/// <summary>Ứng dụng chạy ở khay hệ thống: icon V/E, menu chuột phải giống UniKey, bấm trái để chuyển Việt/Anh.</summary>
internal sealed class TrayApp : ApplicationContext
{
    private readonly AppSettings _settings = AppSettings.Load();
    private readonly VietEngine _engine = new();
    private readonly TypingSession _session;
    private readonly MacroTable _macros = new();
    private readonly KeyboardHook _hook;
    private readonly NotifyIcon _tray;
    private readonly UIContextMenuStrip _menu = new();
    private readonly Control _invoker = new(); // chạy việc trên luồng UI sau khi hook trả về

    private Icon? _iconVi;
    private Icon? _iconEn;
    private ControlPanelForm? _panel;
    private ConverterForm? _converter;
    private MacroEditorForm? _macroEditor;
    private UIStyle? _appliedTheme;
    private int _appliedColor;

    private static string MacroFile => Path.Combine(AppSettings.DataFolder, "macros.txt");

    public TrayApp()
    {
        Lang.Vietnamese = _settings.VietnameseInterface;
        _session = new TypingSession(_engine);
        LoadMacros();

        _iconVi = IconFactory.Create(vietnamese: true);
        _iconEn = IconFactory.Create(vietnamese: false);

        ApplyTheme();
        _menu.Font = UiKit.NewFont(10f);
        UiKit.RegisterMenu(_menu);

        _tray = new NotifyIcon { ContextMenuStrip = _menu, Visible = true };
        _tray.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                Toggle();
        };
        _tray.DoubleClick += (_, _) => ShowControlPanel();

        _invoker.CreateControl();
        _hook = new KeyboardHook(_session, _settings, _macros);
        _hook.ToggleRequested += (_, _) => Toggle();
        _hook.HotkeyPressed += (_, key) => _invoker.BeginInvoke(new Action(() => OnHotkey(key)));

        ApplySettings();

        try
        {
            _hook.Install();
        }
        catch (Exception ex)
        {
            UiKit.Error(ex.Message);
        }

        if (_settings.ShowPanelOnStartup)
            ShowControlPanel();
    }

    // ------------------------------------------------------------------ menu

    /// <summary>Dựng lại menu chuột phải theo cấu hình và ngôn ngữ hiện tại (bố cục UniKey).</summary>
    private void BuildMenu()
    {
        var old = _menu.Items.Cast<ToolStripItem>().ToList();
        _menu.Items.Clear();
        foreach (var item in old)
            item.Dispose();

        _menu.Items.Add(Item(Lang.T("Hướng dẫn", "Help"), ControlPanelForm.ShowHelp));
        _menu.Items.Add(Item(Lang.T("Công cụ...[CS+F6]", "Toolkit...[CS+F6]"), ShowConverter));
        _menu.Items.Add(Item(Lang.T("Chuyển mã nhanh - [CS+F9]", "Quick convert - [CS+F9]"), QuickConvert));
        _menu.Items.Add(new ToolStripSeparator());

        _menu.Items.Add(Item(Lang.T("Bật kiểm tra chính tả", "Enable spell checking"),
            () => Update(s => s.SpellCheck = !s.SpellCheck), _settings.SpellCheck));
        _menu.Items.Add(Item(Lang.T("Bật tính năng gõ tắt", "Enable macros"),
            () => Update(s => s.MacroEnabled = !s.MacroEnabled), _settings.MacroEnabled));
        _menu.Items.Add(Item(Lang.T("Soạn bảng gõ tắt...", "Edit macro table..."), ShowMacroEditor));

        var methods = new ToolStripMenuItem(Lang.T("Kiểu gõ", "Input method"));
        methods.DropDownItems.Add(Item("Telex", () => Update(s => s.Method = InputMethod.Telex), _settings.Method == InputMethod.Telex));
        methods.DropDownItems.Add(Item("VNI", () => Update(s => s.Method = InputMethod.Vni), _settings.Method == InputMethod.Vni));
        _menu.Items.Add(methods);
        _menu.Items.Add(new ToolStripSeparator());

        _menu.Items.Add(CharsetItem(VietCharset.Unicode));
        _menu.Items.Add(CharsetItem(VietCharset.Tcvn3));
        _menu.Items.Add(CharsetItem(VietCharset.VniWindows));
        var otherCharsets = new ToolStripMenuItem(Lang.T("Bảng mã khác", "Other charsets"))
        {
            Checked = _settings.OutputCharset == VietCharset.UnicodeComposite,
        };
        otherCharsets.DropDownItems.Add(CharsetItem(VietCharset.UnicodeComposite));
        _menu.Items.Add(otherCharsets);
        _menu.Items.Add(new ToolStripSeparator());

        var panel = Item(Lang.T("Bảng điều khiển...[CS+F5]", "Control panel...[CS+F5]"), ShowControlPanel);
        panel.Font = new Font(_menu.Font, FontStyle.Bold);
        _menu.Items.Add(panel);
        _menu.Items.Add(Item(Lang.T("Kết thúc", "Exit"), ExitThread));
    }

    private static ToolStripMenuItem Item(string text, Action onClick, bool isChecked = false) =>
        new(text, null, (_, _) => onClick()) { Checked = isChecked };

    private ToolStripMenuItem CharsetItem(VietCharset charset) =>
        Item(ControlPanelForm.CharsetName(charset), () => Update(s => s.OutputCharset = charset), _settings.OutputCharset == charset);

    private void Update(Action<AppSettings> change)
    {
        change(_settings);
        ApplySettings();
    }

    // ------------------------------------------------------------------ hành động

    private void Toggle()
    {
        _settings.Enabled = !_settings.Enabled;
        ApplySettings();

        if (_settings.BeepOnToggle)
            SystemSounds.Beep.Play();
        if (_settings.ShowFeedback)
            _tray.ShowBalloonTip(1000, "VietKey", StatusText(), ToolTipIcon.None);
    }

    private void OnHotkey(AppHotkey key)
    {
        switch (key)
        {
            case AppHotkey.ControlPanel: ShowControlPanel(); break;
            case AppHotkey.Toolkit: ShowConverter(); break;
            case AppHotkey.QuickConvert: QuickConvert(); break;
        }
    }

    private void ShowControlPanel()
    {
        if (_panel is { IsDisposed: false })
        {
            _panel.Activate();
            return;
        }

        var form = new ControlPanelForm(_settings, _iconVi);
        form.SettingsChanged += (_, _) => ApplySettings();
        form.ExitRequested += (_, _) => ExitThread();
        form.EditMacrosRequested += (_, _) => ShowMacroEditor();
        form.FormClosed += (_, _) => _panel = null;
        _panel = form;
        form.Show();
        form.Activate();
    }

    private void ShowConverter()
    {
        if (_converter is { IsDisposed: false })
        {
            _converter.Activate();
            return;
        }

        var form = new ConverterForm(_settings, _iconVi);
        form.CharsetsChanged += (_, _) => _settings.Save();
        form.FormClosed += (_, _) => _converter = null;
        _converter = form;
        form.Show();
        form.Activate();
    }

    private void ShowMacroEditor()
    {
        if (_macroEditor is { IsDisposed: false })
        {
            _macroEditor.Activate();
            return;
        }

        var form = new MacroEditorForm(_macros, MacroFile, _iconVi);
        form.FormClosed += (_, _) => _macroEditor = null;
        _macroEditor = form;
        form.Show();
        form.Activate();
    }

    /// <summary>Chuyển mã clipboard theo bảng mã nguồn/đích đã chọn trong Công cụ.</summary>
    private void QuickConvert()
    {
        string from = ControlPanelForm.CharsetName(_settings.ConvertFrom);
        string to = ControlPanelForm.CharsetName(_settings.ConvertTo);
        try
        {
            int? length = ClipboardConverter.ConvertClipboard(_settings.ConvertFrom, _settings.ConvertTo);
            if (length is null)
                _tray.ShowBalloonTip(2000, "VietKey", Lang.T("Clipboard không có văn bản để chuyển mã.", "Clipboard has no text to convert."), ToolTipIcon.Warning);
            else
                _tray.ShowBalloonTip(2000, "VietKey", Lang.T($"Đã chuyển clipboard: {from} → {to}", $"Clipboard converted: {from} → {to}"), ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            _tray.ShowBalloonTip(3000, "VietKey", Lang.T("Lỗi chuyển mã clipboard: ", "Clipboard conversion failed: ") + ex.Message, ToolTipIcon.Error);
        }
    }

    // ------------------------------------------------------------------ cấu hình

    private void LoadMacros()
    {
        try
        {
            if (!File.Exists(MacroFile))
            {
                _macros.Set("vn", "Việt Nam");
                _macros.Set("ko", "không");
                using var writer = new StreamWriter(MacroFile, append: false, new UTF8Encoding(true));
                _macros.Save(writer);
                return;
            }
            using var reader = new StreamReader(MacroFile, Encoding.UTF8);
            _macros.Load(reader);
        }
        catch (Exception)
        {
            // không đọc được bảng gõ tắt thì dùng bảng rỗng
        }
    }

    /// <summary>Đẩy cấu hình xuống engine, lưu Registry và cập nhật icon/menu/bảng điều khiển.</summary>
    private void ApplySettings()
    {
        Lang.Vietnamese = _settings.VietnameseInterface;
        ApplyTheme();

        _engine.Method = _settings.Method;
        _engine.ModernToneStyle = _settings.ModernToneStyle;
        _engine.FreeMarking = _settings.FreeMarking;
        _engine.SpellCheck = _settings.SpellCheck;
        _engine.RestoreInvalidWords = _settings.RestoreInvalidWords;
        _session.OutputCharset = _settings.OutputCharset;
        _hook?.ResetWord();
        _settings.Save();

        // dựng lại menu sau khi sự kiện Click hiện tại (nếu có) xử lý xong
        _invoker.BeginInvoke(new Action(BuildMenu));
        _tray.Icon = _settings.Enabled ? _iconVi : _iconEn;
        string text = "VietKey – " + StatusText();
        _tray.Text = text.Length > 63 ? text[..63] : text;

        _panel?.RefreshValues();
    }

    /// <summary>Đổi theme SunnyUI khi người dùng chọn theme/màu khác.</summary>
    private void ApplyTheme()
    {
        if (_appliedTheme == _settings.Theme && (_settings.Theme != UIStyle.Colorful || _appliedColor == _settings.ThemeColor))
            return;
        _appliedTheme = _settings.Theme;
        _appliedColor = _settings.ThemeColor;
        UiKit.ApplyTheme(_settings.Theme, Color.FromArgb(_settings.ThemeColor));
    }

    private string StatusText()
    {
        if (!_settings.Enabled)
            return Lang.T("Tiếng Anh", "English");
        string method = _settings.Method == InputMethod.Telex ? "Telex" : "VNI";
        return Lang.T("Tiếng Việt", "Vietnamese") + $" ({method}, {ControlPanelForm.CharsetName(_settings.OutputCharset)})";
    }

    protected override void ExitThreadCore()
    {
        _hook.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        _panel?.Close();
        _converter?.Close();
        _macroEditor?.Close();
        _invoker.Dispose();
        _menu.Dispose();
        _iconVi?.Dispose();
        _iconEn?.Dispose();
        _iconVi = _iconEn = null;
        base.ExitThreadCore();
    }
}
