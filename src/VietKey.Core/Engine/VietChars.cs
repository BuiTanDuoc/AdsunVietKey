namespace VietKey.Core.Engine;

/// <summary>Bảng nguyên âm tiếng Việt dựng sẵn (precomposed Unicode).</summary>
internal static class VietChars
{
    public const int None = 0, Sac = 1, Huyen = 2, Hoi = 3, Nga = 4, Nang = 5;

    /// <summary>Các nguyên âm gốc (chữ thường), cùng thứ tự với <see cref="ToneTable"/>.</summary>
    public const string Vowels = "aăâeêioôơuưy";

    // Mỗi hàng: nguyên âm gốc; cột: không dấu, sắc, huyền, hỏi, ngã, nặng.
    private static readonly string[] ToneTable =
    {
        "aáàảãạ", "ăắằẳẵặ", "âấầẩẫậ",
        "eéèẻẽẹ", "êếềểễệ",
        "iíìỉĩị",
        "oóòỏõọ", "ôốồổỗộ", "ơớờởỡợ",
        "uúùủũụ", "ưứừửữự",
        "yýỳỷỹỵ",
    };

    /// <summary>Nguyên âm đã có dấu phụ (mũ, móc, trăng).</summary>
    public const string ShapedVowels = "ăâêôơư";

    public static bool IsVowel(char lower) => Vowels.IndexOf(lower) >= 0;

    public static bool IsShaped(char lower) => ShapedVowels.IndexOf(lower) >= 0;

    /// <summary>Nhóm gốc của một nguyên âm: a/ă/â → a, e/ê → e, o/ô/ơ → o, u/ư → u.</summary>
    public static char Family(char lower) => lower switch
    {
        'a' or 'ă' or 'â' => 'a',
        'e' or 'ê' => 'e',
        'o' or 'ô' or 'ơ' => 'o',
        'u' or 'ư' => 'u',
        _ => lower,
    };

    /// <summary>Ghép chữ cái (dạng thường) với thanh điệu và chữ hoa.</summary>
    public static char Compose(char lower, int tone, bool upper)
    {
        int row = Vowels.IndexOf(lower);
        char c = row >= 0 ? ToneTable[row][tone] : lower;
        return upper ? char.ToUpperInvariant(c) : c;
    }
}
