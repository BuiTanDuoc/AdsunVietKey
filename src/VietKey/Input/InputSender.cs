using System.Runtime.InteropServices;
using static VietKey.Native.NativeMethods;

namespace VietKey.Input;

/// <summary>Gửi phím Backspace và ký tự vào ứng dụng đang được gõ.</summary>
internal static class InputSender
{
    /// <summary>Đánh dấu các sự kiện do VietKey tự gửi để hook bỏ qua chúng ("VIET").</summary>
    public static readonly IntPtr Marker = new(0x56494554);

    private const ushort VK_V = 0x56;
    private static readonly int InputSize = Marshal.SizeOf<INPUT>();

    /// <summary>
    /// Xoá <paramref name="backspaces"/> ký tự rồi gõ <paramref name="text"/>.
    /// <paramref name="viaClipboard"/>: dán chuỗi bằng Ctrl+V (cho ứng dụng không nhận ký tự Unicode gửi trực tiếp).
    /// </summary>
    public static void Replace(int backspaces, string text, bool viaClipboard = false)
    {
        var inputs = new List<INPUT>((backspaces + text.Length) * 2 + 4);
        for (int i = 0; i < backspaces; i++)
            AddKey(inputs, VK_BACK);

        if (viaClipboard && text.Length > 0 && TryPutOnClipboard(text, out string? previous))
        {
            inputs.Add(Key((ushort)VK_CONTROL, 0, 0));
            AddKey(inputs, VK_V);
            inputs.Add(Key((ushort)VK_CONTROL, 0, KEYEVENTF_KEYUP));
            Send(inputs);
            RestoreClipboardLater(previous);
            return;
        }

        foreach (char c in text)
        {
            inputs.Add(Key(0, c, KEYEVENTF_UNICODE));
            inputs.Add(Key(0, c, KEYEVENTF_UNICODE | KEYEVENTF_KEYUP));
        }
        Send(inputs);
    }

    /// <summary>Gửi một phím (nhấn + nhả), ví dụ Enter.</summary>
    public static void SendKey(int vk)
    {
        var inputs = new List<INPUT>(2);
        AddKey(inputs, vk);
        Send(inputs);
    }

    private static void AddKey(List<INPUT> inputs, int vk)
    {
        inputs.Add(Key((ushort)vk, 0, 0));
        inputs.Add(Key((ushort)vk, 0, KEYEVENTF_KEYUP));
    }

    private static void Send(List<INPUT> inputs)
    {
        if (inputs.Count > 0)
            SendInput((uint)inputs.Count, inputs.ToArray(), InputSize);
    }

    private static bool TryPutOnClipboard(string text, out string? previous)
    {
        previous = null;
        try
        {
            if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
                previous = Clipboard.GetText(TextDataFormat.UnicodeText);
            Clipboard.SetText(text, TextDataFormat.UnicodeText);
            return true;
        }
        catch (ExternalException)
        {
            return false; // clipboard đang bận → gửi ký tự trực tiếp
        }
    }

    /// <summary>Trả lại nội dung clipboard cũ sau khi ứng dụng đích đã dán xong.</summary>
    private static void RestoreClipboardLater(string? previous)
    {
        var timer = new System.Windows.Forms.Timer { Interval = 200 };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            timer.Dispose();
            try
            {
                if (previous is null)
                    Clipboard.Clear();
                else
                    Clipboard.SetText(previous, TextDataFormat.UnicodeText);
            }
            catch (ExternalException)
            {
            }
        };
        timer.Start();
    }

    private static INPUT Key(ushort vk, ushort scan, uint flags) => new()
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = vk,
                wScan = scan,
                dwFlags = flags,
                dwExtraInfo = Marker,
            },
        },
    };
}
