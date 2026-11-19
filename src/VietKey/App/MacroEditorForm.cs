using VietKey.Core.Macros;

namespace VietKey.App;

/// <summary>Soạn bảng gõ tắt: lưới hai cột "Gõ tắt" / "Thay thế".</summary>
internal sealed class MacroEditorForm : Form
{
    private readonly MacroTable _table;
    private readonly string _filePath;
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = true,
        AllowUserToDeleteRows = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersWidth = 28,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
    };

    public event EventHandler? Saved;

    public MacroEditorForm(MacroTable table, string filePath, Icon? icon)
    {
        _table = table;
        _filePath = filePath;

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = Lang.T("VietKey – Bảng gõ tắt", "VietKey – Macro table");
        Font = new Font("Segoe UI", 10f);
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(560, 460);
        MinimumSize = new Size(420, 300);
        if (icon is not null) Icon = icon;

        var colKey = new DataGridViewTextBoxColumn { HeaderText = Lang.T("Gõ tắt", "Abbreviation"), FillWeight = 30 };
        var colValue = new DataGridViewTextBoxColumn { HeaderText = Lang.T("Thay thế", "Replacement"), FillWeight = 70 };
        _grid.Columns.AddRange(colKey, colValue);
        foreach (var (key, value) in table.Entries)
            _grid.Rows.Add(key, value);

        var hint = new Label
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(6, 6, 6, 0),
            Text = Lang.T(
                "Gõ từ tắt rồi nhấn Space/Enter để thay. Gõ \"Vn\" → viết hoa chữ đầu, \"VN\" → viết hoa toàn bộ.",
                "Type an abbreviation then Space/Enter. \"Vn\" capitalises the first letter, \"VN\" the whole text."),
        };

        var save = new Button { Text = Lang.T("Lưu", "Save"), AutoSize = true };
        var close = new Button { Text = Lang.T("Đóng", "Close"), AutoSize = true, DialogResult = DialogResult.Cancel };
        save.Click += (_, _) => SaveTable();
        CancelButton = close;

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(6),
        };
        buttons.Controls.AddRange(new Control[] { close, save });

        Controls.Add(_grid);
        Controls.Add(hint);
        Controls.Add(buttons);
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
                MessageBox.Show(this, Lang.T($"Từ gõ tắt \"{key}\" không được chứa khoảng trắng.", $"Abbreviation \"{key}\" must not contain spaces."),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            Saved?.Invoke(this, EventArgs.Empty);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
