using System.Runtime.InteropServices;
using System.Text;
using VietKey.Core.Conversion;

namespace VietKey.App;

/// <summary>Chuyển mã nội dung clipboard và file văn bản.</summary>
internal static class ClipboardConverter
{
    /// <summary>Chuyển mã văn bản trong clipboard. Trả về số ký tự kết quả, hoặc null nếu clipboard không có văn bản.</summary>
    public static int? ConvertClipboard(VietCharset from, VietCharset to)
    {
        string? text = Retry(() => Clipboard.ContainsText(TextDataFormat.UnicodeText)
            ? Clipboard.GetText(TextDataFormat.UnicodeText)
            : null);
        if (string.IsNullOrEmpty(text))
            return null;

        string result = VietConverter.Convert(text, from, to);
        Retry(() =>
        {
            Clipboard.SetText(result, TextDataFormat.UnicodeText);
            return true;
        });
        return result.Length;
    }

    /// <summary>
    /// Chuyển mã file văn bản thuần. File TCVN3/VNI được đọc/ghi theo byte (Latin-1),
    /// file Unicode theo UTF-8 (có BOM khi ghi).
    /// </summary>
    public static void ConvertFile(string inputPath, string outputPath, VietCharset from, VietCharset to)
    {
        string text = IsLegacy(from)
            ? File.ReadAllText(inputPath, Encoding.Latin1)
            : File.ReadAllText(inputPath, Encoding.UTF8); // tự nhận BOM UTF-16/UTF-8

        string result = VietConverter.Convert(text, from, to);

        Encoding outEncoding = IsLegacy(to) ? Encoding.Latin1 : new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        File.WriteAllText(outputPath, result, outEncoding);
    }

    private static bool IsLegacy(VietCharset c) => c is VietCharset.Tcvn3 or VietCharset.VniWindows;

    /// <summary>Clipboard có thể đang bị ứng dụng khác giữ: thử lại vài lần.</summary>
    private static T Retry<T>(Func<T> action)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return action();
            }
            catch (ExternalException) when (attempt < 4)
            {
                Thread.Sleep(50);
            }
        }
    }
}
