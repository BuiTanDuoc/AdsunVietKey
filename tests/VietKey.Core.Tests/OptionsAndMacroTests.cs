using VietKey.Core.Conversion;
using VietKey.Core.Engine;
using VietKey.Core.Macros;
using Xunit;

namespace VietKey.Core.Tests;

public class OptionsAndMacroTests
{
    private static string Type(VietEngine engine, string keys)
    {
        foreach (char k in keys)
            engine.Type(k);
        return engine.Render();
    }

    [Fact]
    public void FreeMarkingOff_RequiresMarkRightAfterVowel()
    {
        Assert.Equal("toans", Type(new VietEngine { FreeMarking = false }, "toans"));
        Assert.Equal("toán", Type(new VietEngine { FreeMarking = false }, "toasn"));
        Assert.Equal("toán", Type(new VietEngine { FreeMarking = true }, "toans"));
    }

    [Fact]
    public void SpellCheckOff_TransformsAnyWord()
    {
        Assert.Equal("fix", Type(new VietEngine { SpellCheck = true }, "fix"));
        Assert.Equal("fĩ", Type(new VietEngine { SpellCheck = false }, "fix"));
    }

    [Fact]
    public void Session_OutputsInTcvn3()
    {
        var session = new TypingSession(new VietEngine()) { OutputCharset = VietCharset.Tcvn3 };
        foreach (char c in "vie")
            Assert.False(session.OnChar(c).Swallow);

        Assert.Equal(new KeyResult(true, 1, "ª"), session.OnChar('e'));   // ê = 0xAA
        Assert.Equal(new KeyResult(true, 1, "Ö"), session.OnChar('j'));   // ệ = 0xD6
        Assert.False(session.OnChar('t').Swallow);
        Assert.Equal("viÖt", session.CurrentOutput);
    }

    [Fact]
    public void Session_OutputsInVni_CountsMultiCharLetters()
    {
        var session = new TypingSession(new VietEngine()) { OutputCharset = VietCharset.VniWindows };
        foreach (char c in "vieej")
            session.OnChar(c);
        Assert.Equal("vieä", session.CurrentOutput);

        // thêm 't' rồi Backspace: chỉ xoá 't'
        session.OnChar('t');
        Assert.False(session.OnBackspace().Swallow);

        // Backspace tiếp: xoá "ệ" = 2 ký tự VNI ("eä") rồi không gõ lại gì
        Assert.Equal(new KeyResult(true, 2, ""), session.OnBackspace());
        Assert.Equal("vi", session.CurrentOutput);
    }

    [Theory]
    [InlineData("vn", "Việt Nam")]
    [InlineData("Vn", "Việt Nam")]
    [InlineData("VN", "VIỆT NAM")]
    [InlineData("ko", "không")]
    [InlineData("Ko", "Không")]
    public void Macro_ExpandsWithCaseRules(string typed, string expected)
    {
        var macros = new MacroTable();
        macros.Set("vn", "Việt Nam");
        macros.Set("ko", "không");

        Assert.True(macros.TryExpand(typed, out string result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Macro_SaveAndLoadRoundTrip()
    {
        var macros = new MacroTable();
        macros.Set("vn", "Việt Nam");
        macros.Set("tphcm", "Thành phố Hồ Chí Minh");

        var writer = new StringWriter();
        macros.Save(writer);

        var loaded = new MacroTable();
        loaded.Load(new StringReader(writer.ToString()));
        Assert.Equal(2, loaded.Count);
        Assert.True(loaded.TryExpand("tphcm", out string value));
        Assert.Equal("Thành phố Hồ Chí Minh", value);
        Assert.False(loaded.TryExpand("xyz", out _));
    }
}
