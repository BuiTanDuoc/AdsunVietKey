using VietKey.Core.Engine;

namespace VietKey.App;

/// <summary>Bảng điều khiển (dựng bằng code, không cần Designer).</summary>
internal sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly RadioButton _rbTelex = new() { Text = "Telex", AutoSize = true };
    private readonly RadioButton _rbVni = new() { Text = "VNI", AutoSize = true };
    private readonly CheckBox _cbModern = new() { Text = "Đặt dấu kiểu mới (hoà, thuỷ, khoẻ)", AutoSize = true };
    private readonly CheckBox _cbRestore = new() { Text = "Khôi phục phím với từ không phải tiếng Việt", AutoSize = true };
    private readonly CheckBox _cbStartup = new() { Text = "Khởi động cùng Windows", AutoSize = true };
    private readonly ComboBox _cbHotkey = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;

        Text = "VietKey – Bảng điều khiển";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Font = new Font("Segoe UI", 9f);
        Padding = new Padding(12);
        ShowInTaskbar = true;

        _cbHotkey.Items.AddRange(new object[] { "Ctrl + Shift", "Alt + Z" });

        var methodBox = new GroupBox { Text = "Kiểu gõ", AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(8) };
        var methodRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        methodRow.Controls.AddRange(new Control[] { _rbTelex, _rbVni });
        methodBox.Controls.Add(methodRow);

        var hotkeyRow = new FlowLayoutPanel { AutoSize = true };
        hotkeyRow.Controls.Add(new Label { Text = "Phím chuyển Việt/Anh:", AutoSize = true, Margin = new Padding(3, 7, 3, 3) });
        hotkeyRow.Controls.Add(_cbHotkey);

        var testBox = new TextBox
        {
            Width = 360,
            PlaceholderText = "Gõ thử ở đây, ví dụ: Tieesng Vieejt",
        };

        var ok = new Button { Text = "Đồng ý", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Huỷ", DialogResult = DialogResult.Cancel, AutoSize = true };
        var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        buttons.Controls.AddRange(new Control[] { cancel, ok });
        AcceptButton = ok;
        CancelButton = cancel;

        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Dock = DockStyle.Fill };
        layout.Controls.Add(methodBox);
        layout.Controls.Add(_cbModern);
        layout.Controls.Add(_cbRestore);
        layout.Controls.Add(hotkeyRow);
        layout.Controls.Add(_cbStartup);
        layout.Controls.Add(new Label { Text = "Gõ thử:", AutoSize = true, Margin = new Padding(3, 10, 3, 0) });
        layout.Controls.Add(testBox);
        layout.Controls.Add(buttons);
        Controls.Add(layout);

        LoadValues();
        ok.Click += (_, _) => SaveValues();
    }

    private void LoadValues()
    {
        _rbTelex.Checked = _settings.Method == InputMethod.Telex;
        _rbVni.Checked = _settings.Method == InputMethod.Vni;
        _cbModern.Checked = _settings.ModernToneStyle;
        _cbRestore.Checked = _settings.RestoreInvalidWords;
        _cbHotkey.SelectedIndex = (int)_settings.Hotkey;
        _cbStartup.Checked = AppSettings.RunAtStartup;
    }

    private void SaveValues()
    {
        _settings.Method = _rbVni.Checked ? InputMethod.Vni : InputMethod.Telex;
        _settings.ModernToneStyle = _cbModern.Checked;
        _settings.RestoreInvalidWords = _cbRestore.Checked;
        _settings.Hotkey = (ToggleHotkey)Math.Max(0, _cbHotkey.SelectedIndex);
        try
        {
            AppSettings.RunAtStartup = _cbStartup.Checked;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Không đặt được khởi động cùng Windows: " + ex.Message, Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
