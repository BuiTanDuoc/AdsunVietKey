using System.Globalization;

namespace AdsunVietKey.Core.Macros;

/// <summary>
/// Bảng gõ tắt: "vn" → "Việt Nam". Tra không phân biệt hoa/thường;
/// gõ "Vn" → "Việt Nam" (viết hoa chữ đầu), gõ "VN" → "VIỆT NAM".
/// File lưu dạng UTF-8, mỗi dòng: <c>từ_tắt&lt;TAB&gt;nội_dung</c>; dòng bắt đầu bằng '#' là chú thích.
/// </summary>
public sealed class MacroTable
{
    private readonly Dictionary<string, string> _items = new(StringComparer.OrdinalIgnoreCase);

    public int Count => _items.Count;

    public IEnumerable<KeyValuePair<string, string>> Entries =>
        _items.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase);

    public void Clear() => _items.Clear();

    public void Set(string key, string value)
    {
        key = key.Trim();
        if (key.Length == 0 || key.Any(char.IsWhiteSpace))
            throw new ArgumentException("Từ gõ tắt không được rỗng hoặc chứa khoảng trắng.", nameof(key));
        _items[key] = value;
    }

    public bool TryExpand(string word, out string expansion)
    {
        expansion = string.Empty;
        if (word.Length == 0 || !_items.TryGetValue(word, out string? value))
            return false;

        bool hasLetters = word.Any(char.IsLetter);
        bool allUpper = hasLetters && word.Length > 1 && word.Where(char.IsLetter).All(char.IsUpper);
        bool firstUpper = char.IsUpper(word[0]);

        if (allUpper)
            expansion = value.ToUpper(CultureInfo.InvariantCulture);
        else if (firstUpper && value.Length > 0)
            expansion = char.ToUpper(value[0], CultureInfo.InvariantCulture) + value[1..];
        else
            expansion = value;
        return true;
    }

    public void Load(TextReader reader)
    {
        _items.Clear();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0 || line[0] == '#')
                continue;
            int tab = line.IndexOf('\t');
            if (tab <= 0)
                continue;
            string key = line[..tab].Trim();
            if (key.Length > 0 && !key.Any(char.IsWhiteSpace))
                _items[key] = line[(tab + 1)..];
        }
    }

    public void Save(TextWriter writer)
    {
        writer.WriteLine("# AdsunVietKey – bảng gõ tắt: từ_tắt<TAB>nội_dung");
        foreach (var (key, value) in Entries)
            writer.WriteLine($"{key}\t{value}");
    }
}
