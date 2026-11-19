using System.Text;
using static AdsunVietKey.Core.Engine.VietChars;

namespace AdsunVietKey.Core.Engine;

public enum InputMethod
{
    Telex = 0,
    Vni = 1,
}

/// <summary>
/// Engine ghép chữ tiếng Việt cho MỘT từ đang gõ.
/// Từ được lưu dưới dạng danh sách chữ cái (chưa dấu thanh) + một thanh điệu,
/// nên vị trí đặt dấu luôn được tính lại mỗi khi từ thay đổi ("hoà" + n → "hoàn").
/// Engine không biết gì về bàn phím hay Windows: chỉ nhận ký tự và trả về chuỗi hiển thị.
/// </summary>
public sealed class VietEngine
{
    private struct Letter
    {
        public char Ch;      // dạng thường: a–z, ă â ê ô ơ ư đ, hoặc chữ số
        public bool Upper;
        public bool FromW;   // chữ 'ư' sinh ra từ phím 'w' đứng riêng (Telex)

        public Letter(char ch, bool upper, bool fromW = false)
        {
            Ch = ch;
            Upper = upper;
            FromW = fromW;
        }
    }

    private static readonly HashSet<string> Initials = new(StringComparer.Ordinal)
    {
        "", "b", "c", "ch", "d", "đ", "g", "gh", "gi", "h", "k", "kh", "l", "m", "n",
        "ng", "ngh", "nh", "p", "ph", "q", "qu", "r", "s", "t", "th", "tr", "v", "x",
    };

    private static readonly HashSet<string> Finals = new(StringComparer.Ordinal)
    {
        "", "c", "ch", "m", "n", "ng", "nh", "p", "t",
    };

    private List<Letter> _letters = new();
    private int _tone;
    private bool _literal; // từ này không còn được biến đổi (sau khi "gõ lại để huỷ" hoặc có chữ số)
    private readonly StringBuilder _raw = new(); // các phím đã gõ cho từ này
    private bool _rawValid = true;               // _raw còn khớp với từ (sau Backspace thì không)

    public InputMethod Method { get; set; } = InputMethod.Telex;

    /// <summary>true: "hoà, thuỷ, khoẻ" (kiểu mới); false: "hòa, thủy, khỏe" (kiểu cũ).</summary>
    public bool ModernToneStyle { get; set; } = true;

    /// <summary>Khi từ không còn là âm tiết tiếng Việt hợp lệ, trả lại đúng các phím đã gõ ("Windows", "good").</summary>
    public bool RestoreInvalidWords { get; set; } = true;

    /// <summary>Kiểm tra chính tả: chỉ biến đổi khi từ là âm tiết tiếng Việt hợp lệ.</summary>
    public bool SpellCheck { get; set; } = true;

    /// <summary>Gõ tự do: cho phép gõ dấu sau cả từ ("toans"). Tắt: phải gõ dấu ngay sau nguyên âm ("toasn").</summary>
    public bool FreeMarking { get; set; } = true;

    /// <summary>Các phím đã gõ cho từ hiện tại (dùng cho gõ tắt).</summary>
    public string RawKeys => _raw.ToString();

    public bool IsEmpty => _letters.Count == 0;

    public void Reset()
    {
        _letters.Clear();
        _tone = None;
        _literal = false;
        _raw.Clear();
        _rawValid = true;
    }

    /// <summary>Chuỗi hiện tại của từ đang gõ, đã đặt dấu.</summary>
    public string Render()
    {
        int pos = _tone != None ? TonePosition() : -1;
        var sb = new StringBuilder(_letters.Count);
        for (int i = 0; i < _letters.Count; i++)
        {
            var l = _letters[i];
            sb.Append(Compose(l.Ch, i == pos ? _tone : None, l.Upper));
        }
        return sb.ToString();
    }

    /// <summary>Xử lý một ký tự gõ vào (chữ cái hoặc chữ số).</summary>
    public void Type(char typed)
    {
        char k = char.ToLowerInvariant(typed);
        bool upper = char.IsUpper(typed);
        _raw.Append(typed);

        if (!_literal && TryTransform(k, upper))
            return;
        Append(k, upper);

        if (!_literal && SpellCheck && RestoreInvalidWords && _rawValid && !IsValidWord())
            RestoreRawKeys();
    }

    private void RestoreRawKeys()
    {
        _letters.Clear();
        foreach (char c in _raw.ToString())
            _letters.Add(new Letter(char.ToLowerInvariant(c), char.IsUpper(c)));
        _tone = None;
        _literal = true;
    }

    /// <summary>Xoá chữ cái cuối cùng của từ.</summary>
    public void Backspace()
    {
        if (_letters.Count == 0)
            return;

        int last = _letters.Count - 1;
        if (_tone != None && TonePosition() == last)
            _tone = None; // xoá luôn chữ mang dấu thì dấu cũng mất

        _letters.RemoveAt(last);
        _rawValid = false;
        if (!HasVowel())
            _tone = None;
        _literal = _letters.Exists(l => !char.IsLetter(l.Ch));
    }

    // ------------------------------------------------------------------ biến đổi

    private bool TryTransform(char k, bool upper)
    {
        if (Method == InputMethod.Telex)
        {
            return k switch
            {
                's' => ApplyTone(Sac, k, upper),
                'f' => ApplyTone(Huyen, k, upper),
                'r' => ApplyTone(Hoi, k, upper),
                'x' => ApplyTone(Nga, k, upper),
                'j' => ApplyTone(Nang, k, upper),
                'z' => RemoveTone(),
                'a' or 'e' or 'o' => ApplyCircumflex(k, k, upper),
                'w' => ApplyTelexW(upper),
                'd' => ApplyDStroke(k, upper),
                _ => false,
            };
        }

        return k switch
        {
            '1' => ApplyTone(Sac, k, upper),
            '2' => ApplyTone(Huyen, k, upper),
            '3' => ApplyTone(Hoi, k, upper),
            '4' => ApplyTone(Nga, k, upper),
            '5' => ApplyTone(Nang, k, upper),
            '0' => RemoveTone(),
            '6' => ApplyCircumflex(null, k, upper),
            '7' => ApplyHorn(k, upper),
            '8' => ApplyBreve(k, upper),
            '9' => ApplyDStroke(k, upper),
            _ => false,
        };
    }

    private bool ApplyTone(int tone, char k, bool upper)
    {
        if (!HasVowel() || !Valid())
            return false;
        if (!FreeMarking && !IsVowel(_letters[^1].Ch))
            return false;

        if (_tone == tone)
        {
            // gõ lại phím dấu → huỷ dấu, giữ phím gõ dạng chữ thường ("ass" → "as")
            _tone = None;
            AppendLiteral(k, upper);
            return true;
        }

        _tone = tone;
        return true;
    }

    private bool RemoveTone()
    {
        if (_tone == None)
            return false;
        _tone = None;
        return true;
    }

    /// <summary>Thêm dấu mũ: a→â, e→ê, o→ô. <paramref name="family"/> = null nghĩa là bất kỳ (VNI 6).</summary>
    private bool ApplyCircumflex(char? family, char k, bool upper)
    {
        var (s, e) = VowelCluster();
        if (s < 0)
            return false;

        for (int i = e - 1; i >= s; i--)
        {
            char c = _letters[i].Ch;
            char fam = Family(c);
            if (fam is not ('a' or 'e' or 'o'))
                continue;
            if (family is not null && fam != family)
                continue;

            if (!FreeMarking && i != _letters.Count - 1)
                return false;

            char target = fam switch { 'a' => 'â', 'e' => 'ê', _ => 'ô' };
            if (c == target)
                return Revert(i, fam, k, upper);

            return TrySetLetters(i, target);
        }
        return false;
    }

    /// <summary>Telex 'w': ươ, ă, ơ, ư hoặc 'w' đứng riêng → 'ư'.</summary>
    private bool ApplyTelexW(bool upper)
    {
        var (s, _) = VowelCluster();
        if (s < 0)
        {
            // chưa có nguyên âm: "w" → "ư", "tw" → "tư"
            var saved = Snapshot();
            _letters.Add(new Letter('ư', upper, fromW: true));
            if (Valid())
                return true;
            Restore(saved);
            return false;
        }
        return ApplyHornOrBreve('w', upper, allowBreve: true, allowHorn: true);
    }

    /// <summary>VNI 7: dấu móc (ơ, ư, ươ).</summary>
    private bool ApplyHorn(char k, bool upper) => ApplyHornOrBreve(k, upper, allowBreve: false, allowHorn: true);

    /// <summary>VNI 8: dấu trăng (ă).</summary>
    private bool ApplyBreve(char k, bool upper) => ApplyHornOrBreve(k, upper, allowBreve: true, allowHorn: false);

    private bool ApplyHornOrBreve(char k, bool upper, bool allowBreve, bool allowHorn)
    {
        var (s, e) = VowelCluster();
        if (s < 0)
            return false;

        if (allowHorn)
        {
            // cặp u + o → ươ
            for (int i = s; i < e - 1; i++)
            {
                if (Family(_letters[i].Ch) == 'u' && Family(_letters[i + 1].Ch) == 'o')
                {
                    if (!FreeMarking && i + 1 != _letters.Count - 1)
                        return false;
                    if (_letters[i].Ch == 'ư' && _letters[i + 1].Ch == 'ơ')
                    {
                        SetLetter(i, 'u');
                        return Revert(i + 1, 'o', k, upper);
                    }
                    return TrySetLetters(i, 'ư', i + 1, 'ơ');
                }
            }
        }

        for (int i = e - 1; i >= s; i--)
        {
            char c = _letters[i].Ch;
            char family = Family(c);
            bool applies = (family == 'a' && allowBreve) || (family is 'o' or 'u' && allowHorn);
            if (applies && !FreeMarking && i != _letters.Count - 1)
                return false;

            switch (family)
            {
                case 'a' when allowBreve:
                    return c == 'ă' ? Revert(i, 'a', k, upper) : TrySetLetters(i, 'ă');

                case 'o' when allowHorn:
                    return c == 'ơ' ? Revert(i, 'o', k, upper) : TrySetLetters(i, 'ơ');

                case 'u' when allowHorn:
                    if (c != 'ư')
                        return TrySetLetters(i, 'ư');
                    if (_letters[i].FromW && i == _letters.Count - 1)
                    {
                        // "ww" → "w": thay chữ 'ư' tự sinh bằng chữ 'w' thật
                        _letters[i] = new Letter('w', _letters[i].Upper);
                        _literal = true;
                        return true;
                    }
                    return Revert(i, 'u', k, upper);
            }
        }
        return false;
    }

    /// <summary>d → đ (Telex "dd", VNI "d9"); chỉ áp dụng cho chữ đầu từ.</summary>
    private bool ApplyDStroke(char k, bool upper)
    {
        if (_letters.Count == 0)
            return false;

        if (!FreeMarking && _letters.Count != 1)
            return false;

        char c = _letters[0].Ch;
        if (c == 'đ')
            return Revert(0, 'd', k, upper);
        if (c == 'd')
            return TrySetLetters(0, 'đ');
        return false;
    }

    // ------------------------------------------------------------------ trợ giúp

    /// <summary>Huỷ biến đổi: đưa chữ về dạng gốc và thêm phím vừa gõ như ký tự thường.</summary>
    private bool Revert(int index, char baseCh, char k, bool upper)
    {
        SetLetter(index, baseCh);
        AppendLiteral(k, upper);
        return true;
    }

    private bool TrySetLetters(int i1, char c1, int i2 = -1, char c2 = '\0')
    {
        var saved = Snapshot();
        SetLetter(i1, c1);
        if (i2 >= 0)
            SetLetter(i2, c2);
        if (Valid())
            return true;
        Restore(saved);
        return false;
    }

    private void SetLetter(int index, char ch)
    {
        var l = _letters[index];
        _letters[index] = new Letter(ch, l.Upper);
    }

    private void Append(char k, bool upper)
    {
        _letters.Add(new Letter(k, upper));
        if (!char.IsLetter(k))
            _literal = true;
    }

    private void AppendLiteral(char k, bool upper)
    {
        _letters.Add(new Letter(k, upper));
        _literal = true;
    }

    private (List<Letter> Letters, int Tone) Snapshot() => (new List<Letter>(_letters), _tone);

    private void Restore((List<Letter> Letters, int Tone) s)
    {
        _letters = s.Letters;
        _tone = s.Tone;
    }

    private bool Valid() => !SpellCheck || IsValidWord();

    private bool HasVowel() => _letters.Exists(l => IsVowel(l.Ch));

    /// <summary>Vị trí [start, end) của cụm nguyên âm; "qu" và "gi" được tính là phụ âm đầu.</summary>
    private (int Start, int End) VowelCluster()
    {
        int n = _letters.Count, i = 0;
        while (i < n && !IsVowel(_letters[i].Ch))
            i++;
        if (i >= n)
            return (-1, -1);

        if (i > 0 && i + 1 < n && IsVowel(_letters[i + 1].Ch))
        {
            char prev = _letters[i - 1].Ch, cur = _letters[i].Ch;
            if ((prev == 'q' && cur == 'u') || (prev == 'g' && cur == 'i'))
                i++;
        }

        int start = i;
        while (i < n && IsVowel(_letters[i].Ch))
            i++;
        return (start, i);
    }

    /// <summary>Chọn nguyên âm mang dấu thanh.</summary>
    private int TonePosition()
    {
        var (s, e) = VowelCluster();
        if (s < 0)
            return -1;

        // 1. Ưu tiên nguyên âm có dấu phụ; với "ươ" thì chọn "ơ" (chữ sau)
        for (int i = e - 1; i >= s; i--)
            if (IsShaped(_letters[i].Ch))
                return i;

        int len = e - s;
        if (len == 1)
            return s;
        if (len >= 3)
            return s + 1; // oai, oay, uyu, oeo…

        // 2. Cụm hai nguyên âm
        bool hasFinal = e < _letters.Count;
        if (hasFinal)
            return s + 1; // toán, hoạch, tuýt

        string pair = string.Concat(_letters[s].Ch, _letters[s + 1].Ch);
        if (ModernToneStyle && pair is "oa" or "oe" or "uy")
            return s + 1; // hoà, khoẻ, thuỷ

        return s; // mía, múa, cái, hòa (kiểu cũ)
    }

    /// <summary>Kiểm tra cấu trúc âm tiết: phụ âm đầu + ≤3 nguyên âm + phụ âm cuối hợp lệ.</summary>
    private bool IsValidWord()
    {
        int n = _letters.Count, i = 0;
        foreach (var l in _letters)
            if (!char.IsLetter(l.Ch))
                return false;

        var initial = new StringBuilder();
        while (i < n && !IsVowel(_letters[i].Ch))
            initial.Append(_letters[i++].Ch);

        if (i < n)
        {
            string ini = initial.ToString();
            if (ini == "q" && _letters[i].Ch == 'u')
            {
                initial.Append('u');
                i++;
            }
            else if (ini == "g" && _letters[i].Ch == 'i' && i + 1 < n && IsVowel(_letters[i + 1].Ch))
            {
                initial.Append('i');
                i++;
            }
        }

        if (!Initials.Contains(initial.ToString()))
            return false;

        int vowelStart = i;
        while (i < n && IsVowel(_letters[i].Ch))
            i++;
        int vowelCount = i - vowelStart;
        if (vowelCount > 3)
            return false;

        var final = new StringBuilder();
        for (; i < n; i++)
        {
            if (IsVowel(_letters[i].Ch))
                return false; // nguyên âm sau phụ âm cuối
            final.Append(_letters[i].Ch);
        }

        if (vowelCount == 0)
            return final.Length == 0;
        return Finals.Contains(final.ToString());
    }
}
