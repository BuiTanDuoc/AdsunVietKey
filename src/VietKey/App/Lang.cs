namespace VietKey.App;

/// <summary>Chọn chuỗi giao diện tiếng Việt hoặc tiếng Anh ("Vietnamese interface").</summary>
internal static class Lang
{
    public static bool Vietnamese { get; set; } = true;

    public static string T(string vietnamese, string english) => Vietnamese ? vietnamese : english;
}
