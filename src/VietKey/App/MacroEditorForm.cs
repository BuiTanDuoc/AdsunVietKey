using Sunny.UI;
using VietKey.Core.Macros;

namespace VietKey.App;

/// <summary>Soạn bảng gõ tắt (SunnyUI): lưới hai cột "Gõ tắt" / "Thay thế".</summary>
internal sealed class MacroEditorForm : UIForm
{
    private readonly MacroTable _table;
    private readonly string _filePath;
    private readonly UIDataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = true,
        AllowUserToDeleteRows = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.CellSelect,
        RowHeadersVisible = false,
    };

    public MacroEditorForm(MacroTable table, string filePath, Icon? icon)
    {
        _table = table;
        _filePath = filePath;
        float k = UiKit.Scale(this);
        int S(int v) => (int)Math.Round(v * k);

        Text = Lang.T("VietKey – Bảng gõ tắt", "VietKey – Macro table");
        StartPosition = FormStartPosition.CenterScreen;
        ShowTitleIcon = icon is not null;
        if (icon is not null) Icon = icon;
        EscClose = true;
        TitleHeight = S(36);
        MinimumSize = new Size(S(460), S(340));
        ClientSize = new Size(S(600), S(500));

        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Lang.T("Gõ tắt", "Abbreviation"), FillWeight = 30 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Lang.T("Thay thế", "Replacement"), FillWeight = 70 });
        foreach (var (key, value) in table.Entries)
            _grid.Rows.Add(key, value);

        var hint = new UILabel
        {
            Dock = DockStyle.Top,
            Height = S(52),
            BackColor = Color.Transparent,
            Text = Lang.T(
                "Gõ từ tắt rồi nhấn Space/Enter để thay. \"Vn\" → viết hoa chữ đầu, \"VN\" → viết hoa toàn bộ. Dòng mới ở cuối bảng.",
                "Type an abbreviation then Space/Enter. \"Vn\" capitalises the first letter, \"VN\" the whole text. Add rows at the bottom."),
        };

        var save = new UISymbolButton { Text = Lang.T("Lưu", "Save"), Symbol = FontAwesomeIcons.fa_check, Size = new Size(S(120), S(38)), SymbolSize = S(18) };
        var close = new UISymbolButton { Text = Lang.T("Đóng", "Close"), Symbol = FontAwesomeIcons.fa_times, Size = new Size(S(120), S(38)), SymbolSize = S(18) };
        save.Click += (_, _) => SaveTable();
        close.Click += (_, _) => Close();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = S(56),
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(S(6)),
        };
        buttons.Controls.AddRange(new Control[] { close, save });

        var host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(S(10), S(4), S(10), 0) };
        host.Controls.Add(_grid);
        host.Controls.Add(hint);

        Controls.Add(host);
        Controls.Add(buttons);

        UiKit.ApplyFont(this);
        TitleFont = UiKit.NewFont(11f);
    }

    private void SaveTable()
    {
        _grid.EndEdit();
        var fresh = new MacroTable();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow)
                continue;
            string key = (row.Cells[0].Value as string ?? string.Empty).Trim();
            string value = row.Cells[1].Value as string ?? string.Empty;
            if (key.Length == 0)
                continue;
            if (key.Any(char.IsWhiteSpace))
            {
                UiKit.Error(Lang.T($"Từ gõ tắt \"{key}\" không được chứa khoảng trắng.", $"Abbreviation \"{key}\" must not contain spaces."), Text);
                return;
            }
            fresh.Set(key, value);
        }

        try
        {
            using (var writer = new StreamWriter(_filePath, append: false, new System.Text.UTF8Encoding(true)))
                fresh.Save(writer);

            _table.Clear();
            foreach (var (key, value) in fresh.Entries)
                _table.Set(key, value);

            Close();
        }
        catch (Exception ex)
        {
            UiKit.Error(ex.Message, Text);
        }
    }
}
