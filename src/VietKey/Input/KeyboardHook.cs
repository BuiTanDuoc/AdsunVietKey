using System.Runtime.InteropServices;
using System.Text;
using AdsunVietKey.App;
using AdsunVietKey.Core;
using AdsunVietKey.Core.Conversion;
using AdsunVietKey.Core.Macros;
using static AdsunVietKey.Native.NativeMethods;

namespace AdsunVietKey.Input;

/// <summary>Các phím tắt Ctrl+Shift+Fx giống UniKey.</summary>
internal enum AppHotkey
{
    ControlPanel, // CS+F5
    Toolkit,      // CS+F6
    QuickConvert, // CS+F9
}

/// <summary>
/// Hook bàn phím/chuột cấp thấp (WH_KEYBOARD_LL, WH_MOUSE_LL) cho toàn hệ thống.
/// Phải được cài trên luồng có vòng lặp message (luồng UI).
/// </summary>
internal sealed class KeyboardHook : IDisposable
{
    private const int VK_TAB = 0x09;
    private const int VK_RETURN = 0x0D;
    private const int VK_SPACE = 0x20;
    private const int VK_F5 = 0x74;
    private const int VK_F6 = 0x75;
    private const int VK_F9 = 0x78;

    private readonly TypingSession _session;
    private readonly AppSettings _settings;
    private readonly MacroTable _macros;

    // giữ tham chiếu delegate để GC không thu hồi
    private readonly HookProc _keyboardProc;
    private readonly HookProc _mouseProc;
    private IntPtr _keyboardHook;
    private IntPtr _mouseHook;

    private IntPtr _lastWindow;
    private bool _comboArmed; // đang nhấn Ctrl+Shift mà chưa có phím nào khác
    private readonly StringBuilder _plainWord = new(); // từ đang gõ khi tắt tiếng Việt (cho gõ tắt)

    public event EventHandler? ToggleRequested;
    public event EventHandler<AppHotkey>? HotkeyPressed;

    public KeyboardHook(TypingSession session, AppSettings settings, MacroTable macros)
    {
        _session = session;
        _settings = settings;
        _macros = macros;
        _keyboardProc = KeyboardProc;
        _mouseProc = MouseProc;
    }

    public void Install()
    {
        IntPtr module = GetModuleHandle(null);
        _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, module, 0);
        if (_keyboardHook == IntPtr.Zero)
            throw new InvalidOperationException($"Không cài được hook bàn phím (lỗi {Marshal.GetLastWin32Error()}).");

        _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, module, 0);
    }

    public void Dispose()
    {
        if (_keyboardHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }
        if (_mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }
    }

    /// <summary>Bắt đầu từ mới (đổi cửa sổ, bấm chuột, đổi cấu hình…).</summary>
    public void ResetWord()
    {
        _session.Reset();
        _plainWord.Clear();
    }

    private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var info = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            if (info.dwExtraInfo != InputSender.Marker)
            {
                int msg = (int)wParam;
                bool swallow = msg switch
                {
                    WM_KEYDOWN or WM_SYSKEYDOWN => OnKeyDown((int)info.vkCode),
                    WM_KEYUP or WM_SYSKEYUP => OnKeyUp((int)info.vkCode),
                    _ => false,
                };
                if (swallow)
                    return 1;
            }
        }
        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (int)wParam is WM_LBUTTONDOWN or WM_RBUTTONDOWN or WM_MBUTTONDOWN)
            ResetWord(); // bấm chuột có thể đổi vị trí con trỏ soạn thảo
        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private bool OnKeyDown(int vk)
    {
        bool isCtrl = vk is VK_CONTROL or VK_LCONTROL or VK_RCONTROL;
        bool isShift = vk is VK_SHIFT or VK_LSHIFT or VK_RSHIFT;
        bool isAlt = vk is VK_MENU or VK_LMENU or VK_RMENU;
        bool isModifier = isCtrl || isShift || isAlt || vk is VK_LWIN or VK_RWIN or VK_CAPITAL;

        // ---- phím chuyển Việt/Anh
        if (_settings.Hotkey == ToggleHotkey.CtrlShift)
        {
            if ((isCtrl && IsKeyDown(VK_SHIFT)) || (isShift && IsKeyDown(VK_CONTROL)))
                _comboArmed = !IsKeyDown(VK_MENU);
            else if (!isModifier || isAlt)
                _comboArmed = false;
        }
        else if (vk == 'Z' && IsKeyDown(VK_MENU) && !IsKeyDown(VK_CONTROL))
        {
            RequestToggle();
            return true;
        }

        if (isModifier)
            return false;

        bool ctrl = IsKeyDown(VK_CONTROL), alt = IsKeyDown(VK_MENU);

        // ---- Ctrl+Shift+F5/F6/F9
        if (ctrl && IsKeyDown(VK_SHIFT) && !alt && vk is VK_F5 or VK_F6 or VK_F9)
        {
            ResetWord();
            HotkeyPressed?.Invoke(this, vk switch
            {
                VK_F5 => AppHotkey.ControlPanel,
                VK_F6 => AppHotkey.Toolkit,
                _ => AppHotkey.QuickConvert,
            });
            return true;
        }

        // ---- đổi cửa sổ thì bắt đầu từ mới
        IntPtr fg = GetForegroundWindow();
        if (fg != _lastWindow)
        {
            _lastWindow = fg;
            ResetWord();
        }

        if (ctrl || alt || IsKeyDown(VK_LWIN) || IsKeyDown(VK_RWIN))
        {
            ResetWord(); // phím tắt (Ctrl+C, Alt+Tab…) không phải là gõ chữ
            return false;
        }

        bool shift = IsKeyDown(VK_SHIFT);
        char? ch = null;
        if (vk is >= 0x41 and <= 0x5A) // A–Z
            ch = (char)(shift ^ IsCapsLockOn() ? vk : vk + 32);
        else if (vk is >= 0x30 and <= 0x39 && !shift) // 0–9
            ch = (char)vk;

        return _settings.Enabled ? OnKeyVietnamese(vk, ch) : OnKeyPlain(vk, ch);
    }

    /// <summary>Đang bật tiếng Việt.</summary>
    private bool OnKeyVietnamese(int vk, char? ch)
    {
        KeyResult result;
        if (ch is char c)
        {
            result = _session.OnChar(c);
        }
        else if (vk == VK_BACK)
        {
            result = _session.OnBackspace();
        }
        else
        {
            // dấu cách, Enter, dấu câu, phím mũi tên… → kết thúc từ
            bool expanded = (vk is VK_SPACE or VK_RETURN or VK_TAB)
                && _settings.MacroEnabled
                && TryExpandMacro(vk, _session.Engine.Render(), _session.Engine.RawKeys, _session.CurrentOutput.Length);
            ResetWord();
            return expanded;
        }

        if (!result.Swallow)
            return false;

        InputSender.Replace(result.Backspaces, result.Text, UseClipboard);
        return true;
    }

    /// <summary>Đang tắt tiếng Việt: chỉ theo dõi từ để gõ tắt (nếu cho phép).</summary>
    private bool OnKeyPlain(int vk, char? ch)
    {
        if (ch is char c)
        {
            _plainWord.Append(c);
            return false;
        }
        if (vk == VK_BACK)
        {
            if (_plainWord.Length > 0)
                _plainWord.Length--;
            return false;
        }

        string word = _plainWord.ToString();
        _plainWord.Clear();
        _session.Reset();

        return (vk is VK_SPACE or VK_RETURN or VK_TAB)
            && _settings.MacroEnabled && _settings.MacroWhenOff
            && TryExpandMacro(vk, word, word, word.Length);
    }

    /// <summary>Thay từ gõ tắt bằng nội dung đầy đủ, rồi gửi lại phím kết thúc từ.</summary>
    private bool TryExpandMacro(int endKey, string word, string rawKeys, int charsOnScreen)
    {
        if (!_macros.TryExpand(word, out string expansion) && !_macros.TryExpand(rawKeys, out expansion))
            return false;

        string text = _session.Output(expansion);
        if (endKey == VK_SPACE)
        {
            InputSender.Replace(charsOnScreen, text + " ", UseClipboard);
        }
        else
        {
            InputSender.Replace(charsOnScreen, text, UseClipboard);
            InputSender.SendKey(endKey);
        }
        return true;
    }

    private bool UseClipboard =>
        _settings.UseClipboardForUnicode &&
        _settings.OutputCharset is VietCharset.Unicode or VietCharset.UnicodeComposite;

    private bool OnKeyUp(int vk)
    {
        bool isCtrlOrShift = vk is VK_CONTROL or VK_LCONTROL or VK_RCONTROL or VK_SHIFT or VK_LSHIFT or VK_RSHIFT;
        if (_settings.Hotkey == ToggleHotkey.CtrlShift && _comboArmed && isCtrlOrShift)
        {
            _comboArmed = false;
            RequestToggle();
        }
        return false;
    }

    private void RequestToggle()
    {
        ResetWord();
        ToggleRequested?.Invoke(this, EventArgs.Empty);
    }
}
