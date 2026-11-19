using System.Text;
using AdsunVietKey.Core.Engine;

namespace AdsunVietKey.Core.Conversion;

public enum VietCharset
{
    /// <summary>Unicode dựng sẵn (NFC) – chuẩn hiện nay.</summary>
    Unicode = 0,

    /// <summary>Unicode tổ hợp: chữ gốc + dấu thanh kết hợp (U+0300, U+0301, U+0303, U+0309, U+0323).</summary>
    UnicodeComposite = 1,

    /// <summary>TCVN3 (ABC, font .VnTime) – 1 byte/ký tự.</summary>
    Tcvn3 = 2,

    /// <summary>VNI-Windows (font VNI-Times) – chữ gốc + 1 byte dấu.</summary>
    VniWindows = 3,

    /// <summary>Tiếng Việt không dấu (chỉ dùng làm bảng mã đích).</summary>
    NoAccent = 4,
}

/// <summary>
/// Chuyển mã văn bản tiếng Việt giữa các bảng mã.
/// Văn bản TCVN3/VNI được biểu diễn như cách Windows đặt vào clipboard:
/// mỗi byte 0xA0–0xFF thành ký tự Unicode U+00A0–U+00FF (Latin-1).
/// </summary>
public static class VietConverter
{
    // Hàng: thứ tự VietChars.Vowels (a ă â e ê i o ô ơ u ư y); cột: không dấu, sắc, huyền, hỏi, ngã, nặng.
    private static readonly int[][] Tcvn3Lower =
    {
        new[] { 'a', 0xB8, 0xB5, 0xB6, 0xB7, 0xB9 },
        new[] { 0xA8, 0xBE, 0xBB, 0xBC, 0xBD, 0xC6 },
        new[] { 0xA9, 0xCA, 0xC7, 0xC8, 0xC9, 0xCB },
        new[] { 'e', 0xD0, 0xCC, 0xCE, 0xCF, 0xD1 },
        new[] { 0xAA, 0xD5, 0xD2, 0xD3, 0xD4, 0xD6 },
        new[] { 'i', 0xDD, 0xD7, 0xD8, 0xDC, 0xDE },
        new[] { 'o', 0xE3, 0xDF, 0xE1, 0xE2, 0xE4 },
        new[] { 0xAB, 0xE8, 0xE5, 0xE6, 0xE7, 0xE9 },
        new[] { 0xAC, 0xED, 0xEA, 0xEB, 0xEC, 0xEE },
        new[] { 'u', 0xF3, 0xEF, 0xF1, 0xF2, 0xF4 },
        new[] { 0xAD, 0xF8, 0xF5, 0xF6, 0xF7, 0xF9 },
        new[] { 'y', 0xFD, 0xFA, 0xFB, 0xFC, 0xFE },
    };

    // TCVN3 chỉ có chữ hoa cho các chữ có dấu phụ (không có chữ hoa mang dấu thanh).
    private static readonly Dictionary<char, char> Tcvn3UpperShaped = new()
    {
        ['ă'] = '¡', ['â'] = '¢', ['ê'] = '£',
        ['ô'] = '¤', ['ơ'] = '¥', ['ư'] = '¦',
    };

    // VNI-Windows (chữ thường; chữ hoa = ToUpper từng ký tự)
    private const string VniTone = "\0ùøûõï";         // -, sắc, huyền, hỏi, ngã, nặng
    private const string VniCircumflex = "âáàåãä";    // â ê ô + thanh
    private const string VniBreve = "êéèúüë";         // ă + thanh
    private const string VniIWithTone = "íìæóò";      // i có thanh (1 ký tự)

    private static readonly string CombiningTones = "\0̣́̀̉̃";

    private static readonly Dictionary<char, char> UniToTcvn3 = new();
    private static readonly Dictionary<char, char> Tcvn3ToUni = new();
    private static readonly Dictionary<char, string> UniToVni = new();
    private static readonly Dictionary<string, char> VniToUni = new(StringComparer.Ordinal);
    private static readonly Dictionary<char, string> UniToComposite = new();
    private static readonly Dictionary<char, char> UniToNoAccent = new();

    static VietConverter()
    {
        for (int row = 0; row < VietChars.Vowels.Length; row++)
        {
            char baseLower = VietChars.Vowels[row];
            for (int tone = 0; tone <= VietChars.Nang; tone++)
            {
                char lower = VietChars.Compose(baseLower, tone, upper: false);
                char upper = VietChars.Compose(baseLower, tone, upper: true);

                // TCVN3
                char tcvn = (char)Tcvn3Lower[row][tone];
                AddPair(UniToTcvn3, Tcvn3ToUni, lower, tcvn);
                if (tone == 0 && Tcvn3UpperShaped.TryGetValue(baseLower, out char tcvnUpper))
                    AddPair(UniToTcvn3, Tcvn3ToUni, upper, tcvnUpper);
                else if (tone != 0)
                    UniToTcvn3[upper] = tcvn; // không có mã chữ hoa → dùng mã chữ thường (font .VnTimeH hiển thị hoa)

                // VNI-Windows
                string vni = VniOf(baseLower, tone);
                AddVni(lower, vni);
                AddVni(upper, vni.ToUpperInvariant());

                // Unicode tổ hợp
                if (tone != 0)
                {
                    char mark = CombiningTones[tone];
                    UniToComposite[lower] = string.Concat(VietChars.Compose(baseLower, 0, false), mark);
                    UniToComposite[upper] = string.Concat(VietChars.Compose(baseLower, 0, true), mark);
                }

                // Không dấu
                char plain = VietChars.Family(baseLower);
                if (plain != lower) UniToNoAccent[lower] = plain;
                if (char.ToUpperInvariant(plain) != upper) UniToNoAccent[upper] = char.ToUpperInvariant(plain);
            }
        }

        AddPair(UniToTcvn3, Tcvn3ToUni, 'đ', '®');
        AddPair(UniToTcvn3, Tcvn3ToUni, 'Đ', '§');
        AddVni('đ', "ñ");
        AddVni('Đ', "Ñ");
        UniToNoAccent['đ'] = 'd';
        UniToNoAccent['Đ'] = 'D';
    }

    private static void AddPair(Dictionary<char, char> encode, Dictionary<char, char> decode, char uni, char legacy)
    {
        if (uni == legacy)
            return; // chữ ASCII giữ nguyên
        encode[uni] = legacy;
        decode[legacy] = uni;
    }

    private static void AddVni(char uni, string vni)
    {
        if (vni.Length == 1 && vni[0] == uni)
            return;
        UniToVni[uni] = vni;
        VniToUni[vni] = uni;
    }

    private static string VniOf(char baseLower, int tone)
    {
        switch (baseLower)
        {
            case 'i':
                return tone == 0 ? "i" : VniIWithTone[tone - 1].ToString();
            case 'ơ':
                return tone == 0 ? "ô" : string.Concat('ô', VniTone[tone]);
            case 'ư':
                return tone == 0 ? "ö" : string.Concat('ö', VniTone[tone]);
            case 'â':
                return string.Concat('a', VniCircumflex[tone]);
            case 'ê':
                return string.Concat('e', VniCircumflex[tone]);
            case 'ô':
                return string.Concat('o', VniCircumflex[tone]);
            case 'ă':
                return string.Concat('a', VniBreve[tone]);
            default:
                if (tone == 0) return baseLower.ToString();
                if (baseLower == 'y' && tone == VietChars.Nang) return "yî"; // ỵ dùng dấu nặng riêng
                return string.Concat(baseLower, VniTone[tone]);
        }
    }

    /// <summary>Chuyển <paramref name="text"/> từ bảng mã <paramref name="from"/> sang <paramref name="to"/>.</summary>
    public static string Convert(string text, VietCharset from, VietCharset to)
    {
        if (from == VietCharset.NoAccent)
            throw new ArgumentException("Không dấu chỉ dùng làm bảng mã đích.", nameof(from));
        return Encode(Decode(text, from), to);
    }

    /// <summary>Đọc văn bản ở bảng mã bất kỳ thành Unicode dựng sẵn.</summary>
    public static string Decode(string text, VietCharset from) => from switch
    {
        VietCharset.Unicode or VietCharset.UnicodeComposite => text.Normalize(NormalizationForm.FormC),
        VietCharset.Tcvn3 => MapChars(text, Tcvn3ToUni),
        VietCharset.VniWindows => DecodeVni(text),
        _ => text,
    };

    /// <summary>Ghi văn bản Unicode dựng sẵn ra bảng mã đích.</summary>
    public static string Encode(string unicode, VietCharset to)
    {
        if (to == VietCharset.Unicode)
            return unicode.Normalize(NormalizationForm.FormC);

        var sb = new StringBuilder(unicode.Length * 2);
        foreach (char c in unicode)
        {
            switch (to)
            {
                case VietCharset.Tcvn3:
                    sb.Append(UniToTcvn3.TryGetValue(c, out char t) ? t : c);
                    break;
                case VietCharset.VniWindows:
                    if (UniToVni.TryGetValue(c, out string? v)) sb.Append(v); else sb.Append(c);
                    break;
                case VietCharset.UnicodeComposite:
                    if (UniToComposite.TryGetValue(c, out string? comp)) sb.Append(comp); else sb.Append(c);
                    break;
                case VietCharset.NoAccent:
                    sb.Append(UniToNoAccent.TryGetValue(c, out char n) ? n : c);
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    private static string MapChars(string text, Dictionary<char, char> map)
    {
        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
            sb.Append(map.TryGetValue(c, out char m) ? m : c);
        return sb.ToString();
    }

    private static string DecodeVni(string text)
    {
        var sb = new StringBuilder(text.Length);
        int i = 0;
        while (i < text.Length)
        {
            if (i + 1 < text.Length && VniToUni.TryGetValue(text.Substring(i, 2), out char two))
            {
                sb.Append(two);
                i += 2;
            }
            else if (VniToUni.TryGetValue(text[i].ToString(), out char one))
            {
                sb.Append(one);
                i++;
            }
            else
            {
                sb.Append(text[i]);
                i++;
            }
        }
        return sb.ToString();
    }

    /// <summary>Tên hiển thị của bảng mã.</summary>
    public static string DisplayName(VietCharset charset) => charset switch
    {
        VietCharset.Unicode => "Unicode (dựng sẵn)",
        VietCharset.UnicodeComposite => "Unicode tổ hợp",
        VietCharset.Tcvn3 => "TCVN3 (ABC)",
        VietCharset.VniWindows => "VNI Windows",
        VietCharset.NoAccent => "Không dấu",
        _ => charset.ToString(),
    };
}
