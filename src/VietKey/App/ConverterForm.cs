using VietKey.Core.Conversion;

namespace VietKey.App;

/// <summary>Cửa sổ chuyển mã (tương tự UniKey Toolkit): clipboard, ô văn bản và file.</summary>
internal sealed class ConverterForm : Form
{
    private static readonly VietCharset[] SourceCharsets =
        { VietCharset.Unicode, VietCharset.UnicodeComposite, VietCharset.Tcvn3, VietCharset.VniWindows };

    private static readonly VietCharset[] TargetCharsets =
        { VietCharset.Unicode, VietCharset.UnicodeComposite, VietCharset.Tcvn3, VietCharset.VniWindows, VietCharset.NoAccent };

    private readonly AppSettings _settings;
    private readonly ComboBox _cbFrom = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
    private readonly ComboBox _cbTo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
    private readonly TextBox _txtSource = NewTextBox();
    private readonly TextBox _txtResult = NewTextBox();
    private readonly Label _status = new() { AutoSize = true, ForeColor = SystemColors.GrayText, Margin = new Padding(3, 8, 3, 3) };

    public event EventHandler? CharsetsChanged;

    public ConverterForm(AppSettings settings)
    {
        _settings = settings;

        Text = Lang.T("VietKey – Công cụ chuyển mã", "VietKey – Converter toolkit");
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);
        MinimumSize = new Size(620, 460);
        Size = new Size(680, 520);
        Padding = new Padding(10);

        foreach (var c in SourceCharsets) _cbFrom.Items.Add(ControlPanelForm.CharsetName(c));
        foreach (var c in TargetCharsets) _cbTo.Items.Add(ControlPanelForm.CharsetName(c));
        _cbFrom.SelectedIndex = Math.Max(0, Array.IndexOf(SourceCharsets, settings.ConvertFrom));
        _cbTo.SelectedIndex = Math.Max(0, Array.IndexOf(TargetCharsets, settings.ConvertTo));
        _cbFrom.SelectedIndexChanged += (_, _) => SaveCharsets();
        _cbTo.SelectedIndexChanged += (_, _) => SaveCharsets();

        var swap = new Button { Text = Lang.T("⇄ Đảo", "⇄ Swap"), AutoSize = true };
        swap.Click += (_, _) => Swap();

        var charsetRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
        charsetRow.Controls.Add(new Label { Text = Lang.T("Nguồn:", "Source:"), AutoSize = true, Margin = new Padding(3, 7, 3, 3) });
        charsetRow.Controls.Add(_cbFrom);
        charsetRow.Controls.Add(swap);
        charsetRow.Controls.Add(new Label { Text = Lang.T("Đích:", "Target:"), AutoSize = true, Margin = new Padding(9, 7, 3, 3) });
        charsetRow.Controls.Add(_cbTo);

        var btnClipboard = new Button { Text = Lang.T("Chuyển mã clipboard  (CS+F9)", "Convert clipboard  (CS+F9)"), AutoSize = true };
        btnClipboard.Click += (_, _) => ConvertClipboard();
        var btnText = new Button { Text = Lang.T("Chuyển ô văn bản ↓", "Convert text box ↓"), AutoSize = true };
        btnText.Click += (_, _) => ConvertText();
        var btnFile = new Button { Text = Lang.T("Chuyển mã file...", "Convert file..."), AutoSize = true };
        btnFile.Click += (_, _) => ConvertFile();
        var btnClose = new Button { Text = Lang.T("Đóng", "Close"), AutoSize = true, DialogResult = DialogResult.Cancel };
        CancelButton = btnClose;

        var actionRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        actionRow.Controls.AddRange(new Control[] { btnClipboard, btnText, btnFile, btnClose });

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(charsetRow);
        layout.Controls.Add(new Label { Text = Lang.T("Văn bản nguồn (dán vào đây hoặc dùng clipboard):", "Source text (paste here or use the clipboard):"), AutoSize = true, Margin = new Padding(3, 8, 3, 2) });
        layout.Controls.Add(_txtSource);
        layout.Controls.Add(new Label { Text = Lang.T("Kết quả:", "Result:"), AutoSize = true, Margin = new Padding(3, 8, 3, 2) });
        layout.Controls.Add(_txtResult);
        layout.Controls.Add(actionRow);
        layout.Controls.Add(_status);
        Controls.Add(layout);
    }

    private VietCharset From => SourceCharsets[Math.Max(0, _cbFrom.SelectedIndex)];
    private VietCharset To => TargetCharsets[Math.Max(0, _cbTo.SelectedIndex)];

    private static TextBox NewTextBox() => new()
    {
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 10f),
    };

    private void SaveCharsets()
    {
        _settings.ConvertFrom = From;
        _settings.ConvertTo = To;
        CharsetsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Swap()
    {
        int toInSource = Array.IndexOf(SourceCharsets, To);
        if (toInSource < 0)
        {
            SetStatus(Lang.T("Không thể đảo: \"Không dấu\" chỉ dùng làm bảng mã đích.", "Cannot swap: \"No accents\" is target-only."));
            return;
        }
        int fromInTarget = Array.IndexOf(TargetCharsets, From);
        _cbFrom.SelectedIndex = toInSource;
        _cbTo.SelectedIndex = fromInTarget;
        (_txtSource.Text, _txtResult.Text) = (_txtResult.Text, _txtSource.Text);
    }

    private void ConvertClipboard()
    {
        try
        {
            int? length = ClipboardConverter.ConvertClipboard(From, To);
            SetStatus(length is null
                ? Lang.T("Clipboard không có văn bản.", "Clipboard has no text.")
                : Lang.T($"Đã chuyển clipboard: {ControlPanelForm.CharsetName(From)} → {ControlPanelForm.CharsetName(To)} ({length} ký tự). Hãy dán (Ctrl+V).", $"Clipboard converted: {ControlPanelForm.CharsetName(From)} → {ControlPanelForm.CharsetName(To)} ({length} chars). Paste with Ctrl+V."));
        }
        catch (Exception ex)
        {
            SetStatus(Lang.T("Lỗi clipboard: ", "Clipboard error: ") + ex.Message);
        }
    }

    private void ConvertText()
    {
        _txtResult.Text = VietConverter.Convert(_txtSource.Text, From, To);
        SetStatus(Lang.T($"Đã chuyển {_txtSource.TextLength} ký tự.", $"Converted {_txtSource.TextLength} characters."));
    }

    private void ConvertFile()
    {
        using var open = new OpenFileDialog
        {
            Title = Lang.T("Chọn file nguồn", "Choose source file") + $" ({ControlPanelForm.CharsetName(From)})",
            Filter = Lang.T("File văn bản", "Text files") + " (*.txt;*.csv;*.htm;*.html)|*.txt;*.csv;*.htm;*.html|" + Lang.T("Tất cả", "All files") + " (*.*)|*.*",
        };
        if (open.ShowDialog(this) != DialogResult.OK)
            return;

        using var save = new SaveFileDialog
        {
            Title = Lang.T("Lưu kết quả", "Save result") + $" ({ControlPanelForm.CharsetName(To)})",
            FileName = Path.GetFileNameWithoutExtension(open.FileName) + "_" + To + Path.GetExtension(open.FileName),
            InitialDirectory = Path.GetDirectoryName(open.FileName),
            Filter = open.Filter,
        };
        if (save.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            ClipboardConverter.ConvertFile(open.FileName, save.FileName, From, To);
            SetStatus(Lang.T("Đã lưu: ", "Saved: ") + save.FileName);
        }
        catch (Exception ex)
        {
            SetStatus(Lang.T("Lỗi chuyển file: ", "File conversion failed: ") + ex.Message);
        }
    }

    private void SetStatus(string text) => _status.Text = text;
}
