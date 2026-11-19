using Sunny.UI;
using AdsunVietKey.Core.Conversion;

namespace AdsunVietKey.App;

/// <summary>Công cụ chuyển mã (SunnyUI, tương tự UniKey Toolkit): clipboard, ô văn bản và file.</summary>
internal sealed class ConverterForm : UIForm
{
    private static readonly VietCharset[] SourceCharsets =
        { VietCharset.Unicode, VietCharset.UnicodeComposite, VietCharset.Tcvn3, VietCharset.VniWindows };

    private static readonly VietCharset[] TargetCharsets =
        { VietCharset.Unicode, VietCharset.UnicodeComposite, VietCharset.Tcvn3, VietCharset.VniWindows, VietCharset.NoAccent };

    private readonly AppSettings _settings;
    private readonly float _k;
    private readonly UIComboBox _cbFrom = new() { DropDownStyle = UIDropDownStyle.DropDownList };
    private readonly UIComboBox _cbTo = new() { DropDownStyle = UIDropDownStyle.DropDownList };
    private readonly UITextBox _txtSource = NewTextBox();
    private readonly UITextBox _txtResult = NewTextBox();
    private readonly UILabel _status = new() { AutoSize = true, BackColor = Color.Transparent };

    public event EventHandler? CharsetsChanged;

    public ConverterForm(AppSettings settings, Icon? icon)
    {
        _settings = settings;
        _k = UiKit.Scale(this);

        Text = Lang.T("AdsunVietKey – Công cụ chuyển mã", "AdsunVietKey – Converter toolkit");
        StartPosition = FormStartPosition.CenterScreen;
        ShowTitleIcon = icon is not null;
        if (icon is not null) Icon = icon;
        MaximizeBox = true;
        EscClose = true;
        TitleHeight = S(36);
        MinimumSize = new Size(S(700), S(500));
        ClientSize = new Size(S(760), S(560));

        foreach (var c in SourceCharsets) _cbFrom.Items.Add(ControlPanelForm.CharsetName(c));
        foreach (var c in TargetCharsets) _cbTo.Items.Add(ControlPanelForm.CharsetName(c));
        _cbFrom.SelectedIndex = Math.Max(0, Array.IndexOf(SourceCharsets, settings.ConvertFrom));
        _cbTo.SelectedIndex = Math.Max(0, Array.IndexOf(TargetCharsets, settings.ConvertTo));
        _cbFrom.SelectedIndexChanged += (_, _) => SaveCharsets();
        _cbTo.SelectedIndexChanged += (_, _) => SaveCharsets();
        _cbFrom.Size = _cbTo.Size = new Size(S(200), S(29));

        var swap = MakeButton(Lang.T("Đảo", "Swap"), FontAwesomeIcons.fa_exchange, 100);
        swap.Click += (_, _) => Swap();

        var charsetRow = Row();
        charsetRow.Controls.Add(Caption(Lang.T("Nguồn:", "Source:")));
        charsetRow.Controls.Add(_cbFrom);
        charsetRow.Controls.Add(swap);
        charsetRow.Controls.Add(Caption(Lang.T("Đích:", "Target:")));
        charsetRow.Controls.Add(_cbTo);

        var btnClipboard = MakeButton(Lang.T("Chuyển mã clipboard [CS+F9]", "Convert clipboard [CS+F9]"), FontAwesomeIcons.fa_clipboard, 260);
        btnClipboard.Click += (_, _) => ConvertClipboard();
        var btnText = MakeButton(Lang.T("Chuyển ô văn bản", "Convert text box"), FontAwesomeIcons.fa_angle_double_down, 190);
        btnText.Click += (_, _) => ConvertText();
        var btnFile = MakeButton(Lang.T("Chuyển mã file...", "Convert file..."), FontAwesomeIcons.fa_file_text_o, 180);
        btnFile.Click += (_, _) => ConvertFile();

        var actionRow = Row();
        actionRow.Controls.AddRange(new Control[] { btnClipboard, btnText, btnFile });

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, Padding = new Padding(S(10)) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(charsetRow);
        layout.Controls.Add(Caption(Lang.T("Văn bản nguồn (dán vào đây hoặc dùng clipboard):", "Source text (paste here or use the clipboard):")));
        layout.Controls.Add(_txtSource);
        layout.Controls.Add(Caption(Lang.T("Kết quả:", "Result:")));
        layout.Controls.Add(_txtResult);
        layout.Controls.Add(actionRow);
        layout.Controls.Add(_status);
        Controls.Add(layout);

        UiKit.ApplyFont(this);
        TitleFont = UiKit.NewFont(11f);
    }

    private VietCharset From => SourceCharsets[Math.Max(0, _cbFrom.SelectedIndex)];
    private VietCharset To => TargetCharsets[Math.Max(0, _cbTo.SelectedIndex)];

    private int S(int logical) => (int)Math.Round(logical * _k);

    private static UITextBox NewTextBox() => new()
    {
        Multiline = true,
        ShowScrollBar = true,
        Dock = DockStyle.Fill,
    };

    private FlowLayoutPanel Row() => new() { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, S(4), 0, S(4)) };

    private UILabel Caption(string text) => new()
    {
        Text = text,
        AutoSize = true,
        BackColor = Color.Transparent,
        Margin = new Padding(S(3), S(8), S(3), S(3)),
    };

    private UISymbolButton MakeButton(string text, int symbol, int width) => new()
    {
        Text = text,
        Symbol = symbol,
        SymbolSize = S(18),
        Size = new Size(S(width), S(36)),
        Margin = new Padding(S(3), S(3), S(9), S(3)),
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
            if (length is null)
            {
                SetStatus(Lang.T("Clipboard không có văn bản.", "Clipboard has no text."));
                return;
            }
            string from = ControlPanelForm.CharsetName(From), to = ControlPanelForm.CharsetName(To);
            SetStatus(Lang.T($"Đã chuyển clipboard: {from} → {to} ({length} ký tự). Hãy dán (Ctrl+V).",
                             $"Clipboard converted: {from} → {to} ({length} chars). Paste with Ctrl+V."));
            this.ShowSuccessTip(Lang.T("Đã chuyển mã clipboard", "Clipboard converted"));
        }
        catch (Exception ex)
        {
            SetStatus(Lang.T("Lỗi clipboard: ", "Clipboard error: ") + ex.Message);
        }
    }

    private void ConvertText()
    {
        _txtResult.Text = VietConverter.Convert(_txtSource.Text, From, To);
        SetStatus(Lang.T($"Đã chuyển {_txtSource.Text.Length} ký tự.", $"Converted {_txtSource.Text.Length} characters."));
    }

    private void ConvertFile()
    {
        string filter = Lang.T("File văn bản", "Text files") + " (*.txt;*.csv;*.htm;*.html)|*.txt;*.csv;*.htm;*.html|" +
                        Lang.T("Tất cả", "All files") + " (*.*)|*.*";
        using var open = new OpenFileDialog
        {
            Title = Lang.T("Chọn file nguồn", "Choose source file") + $" ({ControlPanelForm.CharsetName(From)})",
            Filter = filter,
        };
        if (open.ShowDialog(this) != DialogResult.OK)
            return;

        using var save = new SaveFileDialog
        {
            Title = Lang.T("Lưu kết quả", "Save result") + $" ({ControlPanelForm.CharsetName(To)})",
            FileName = Path.GetFileNameWithoutExtension(open.FileName) + "_" + To + Path.GetExtension(open.FileName),
            InitialDirectory = Path.GetDirectoryName(open.FileName),
            Filter = filter,
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
