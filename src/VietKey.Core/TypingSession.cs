using AdsunVietKey.Core.Conversion;
using AdsunVietKey.Core.Engine;

namespace AdsunVietKey.Core;

/// <summary>Kết quả xử lý một phím.</summary>
/// <param name="Swallow">true: chặn phím gốc và gửi <see cref="Backspaces"/> + <see cref="Text"/> thay thế.</param>
public readonly record struct KeyResult(bool Swallow, int Backspaces, string Text)
{
    public static readonly KeyResult PassThrough = new(false, 0, string.Empty);
}

/// <summary>
/// So sánh chuỗi trước/sau khi engine xử lý phím để quyết định:
/// cho phím đi qua bình thường, hay chặn lại rồi gửi số Backspace + chuỗi cần thay.
/// Chuỗi được so sánh ở bảng mã đầu ra (Unicode, TCVN3, VNI…), nên số Backspace
/// luôn đúng với số ký tự thực sự nằm trong ứng dụng.
/// </summary>
public sealed class TypingSession
{
    public TypingSession(VietEngine engine) => Engine = engine;

    public VietEngine Engine { get; }

    /// <summary>Bảng mã của văn bản gửi vào ứng dụng.</summary>
    public VietCharset OutputCharset { get; set; } = VietCharset.Unicode;

    /// <summary>Từ đang gõ, ở bảng mã đầu ra (đúng như đang hiển thị trong ứng dụng).</summary>
    public string CurrentOutput => Output(Engine.Render());

    public void Reset() => Engine.Reset();

    public KeyResult OnChar(char c)
    {
        string before = CurrentOutput;
        Engine.Type(c);
        string after = CurrentOutput;

        if (after.Length == before.Length + 1 && after[^1] == c && after.StartsWith(before, StringComparison.Ordinal))
            return KeyResult.PassThrough;

        return Diff(before, after);
    }

    public KeyResult OnBackspace()
    {
        if (Engine.IsEmpty)
            return KeyResult.PassThrough;

        string before = CurrentOutput;
        Engine.Backspace();
        string after = CurrentOutput;

        if (after.Length == before.Length - 1 && before.StartsWith(after, StringComparison.Ordinal))
            return KeyResult.PassThrough;

        return Diff(before, after);
    }

    /// <summary>Chuyển một chuỗi Unicode sang bảng mã đầu ra.</summary>
    public string Output(string unicode) =>
        OutputCharset == VietCharset.Unicode ? unicode : VietConverter.Encode(unicode, OutputCharset);

    private static KeyResult Diff(string before, string after)
    {
        int p = 0;
        while (p < before.Length && p < after.Length && before[p] == after[p])
            p++;
        return new KeyResult(true, before.Length - p, after[p..]);
    }
}
