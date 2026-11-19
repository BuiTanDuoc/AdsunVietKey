using AdsunVietKey.Core;
using AdsunVietKey.Core.Engine;
using Xunit;

namespace AdsunVietKey.Core.Tests;

public class EngineTests
{
    /// <summary>Gõ lần lượt từng phím; '&lt;' nghĩa là Backspace.</summary>
    private static string Type(string keys, InputMethod method = InputMethod.Telex, bool modern = true)
    {
        var engine = new VietEngine { Method = method, ModernToneStyle = modern };
        foreach (char k in keys)
        {
            if (k == '<') engine.Backspace();
            else engine.Type(k);
        }
        return engine.Render();
    }

    [Theory]
    [InlineData("tieesng", "tiếng")]
    [InlineData("tieengs", "tiếng")]
    [InlineData("vieejt", "việt")]
    [InlineData("Vieejt", "Việt")]
    [InlineData("VIEEJT", "VIỆT")]
    [InlineData("dduwowcj", "được")]
    [InlineData("nguwowif", "người")]
    [InlineData("nguoiwf", "người")]
    [InlineData("truwowngf", "trường")]
    [InlineData("ruowuj", "rượu")]
    [InlineData("quyeets", "quyết")]
    [InlineData("gif", "gì")]
    [InlineData("giaf", "già")]
    [InlineData("quaf", "quà")]
    [InlineData("cuar", "của")]
    [InlineData("muaf", "mùa")]
    [InlineData("toans", "toán")]
    [InlineData("chuyeenr", "chuyển")]
    [InlineData("khoong", "không")]
    [InlineData("Ddaay", "Đây")]
    [InlineData("tw", "tư")]
    [InlineData("w", "ư")]
    public void Telex_ComposesWords(string keys, string expected) => Assert.Equal(expected, Type(keys));

    [Theory]
    [InlineData("ass", "as")]
    [InlineData("aaa", "aa")]
    [InlineData("ddd", "dd")]
    [InlineData("ww", "w")]
    [InlineData("www", "ww")]
    public void Telex_RepeatedKeyUndoes(string keys, string expected) => Assert.Equal(expected, Type(keys));

    [Theory]
    [InlineData("Windows", "Windows")]
    [InlineData("good", "good")]
    [InlineData("fix", "fix")]
    public void Telex_RestoresNonVietnameseWords(string keys, string expected) => Assert.Equal(expected, Type(keys));

    [Theory]
    [InlineData("hoaf", "hoà", "hòa")]
    [InlineData("thuyr", "thuỷ", "thủy")]
    [InlineData("khoer", "khoẻ", "khỏe")]
    [InlineData("hoafn", "hoàn", "hoàn")]
    public void TonePlacement_ModernAndOld(string keys, string modern, string old)
    {
        Assert.Equal(modern, Type(keys, modern: true));
        Assert.Equal(old, Type(keys, modern: false));
    }

    [Theory]
    [InlineData("hoaf<", "ho")]
    [InlineData("hoans<", "hoá")]
    [InlineData("dduowcj<<", "đư")]
    public void Backspace_RemovesLastLetter(string keys, string expected) => Assert.Equal(expected, Type(keys));

    [Theory]
    [InlineData("vie65t", "việt")]
    [InlineData("Vie65t", "Việt")]
    [InlineData("d9u7o7c5", "được")]
    [InlineData("duo7c59", "được")]
    [InlineData("tru7o7ng2", "trường")]
    [InlineData("hoa2", "hoà")]
    [InlineData("d9a6y", "đây")]
    [InlineData("a11", "a1")]
    [InlineData("a66", "a6")]
    public void Vni_ComposesWords(string keys, string expected) => Assert.Equal(expected, Type(keys, InputMethod.Vni));

    [Fact]
    public void Session_PassesPlainKeysAndReplacesTransformed()
    {
        var session = new TypingSession(new VietEngine());

        Assert.False(session.OnChar('v').Swallow);
        Assert.False(session.OnChar('i').Swallow);
        Assert.False(session.OnChar('e').Swallow);

        // "vie" + 'e' → "viê": xoá 1 ký tự, gõ "ê"
        Assert.Equal(new KeyResult(true, 1, "ê"), session.OnChar('e'));

        // "viê" + 'j' → "việ"
        Assert.Equal(new KeyResult(true, 1, "ệ"), session.OnChar('j'));

        Assert.False(session.OnChar('t').Swallow);
        Assert.Equal("việt", session.Engine.Render());
    }

    [Fact]
    public void Session_MovesToneWhenWordGrows_OldStyle()
    {
        var session = new TypingSession(new VietEngine { ModernToneStyle = false });
        foreach (char c in "hoaf")
            session.OnChar(c);
        Assert.Equal("hòa", session.Engine.Render());

        // thêm 'n' → dấu chuyển sang 'a': xoá "òa", gõ "oàn"
        Assert.Equal(new KeyResult(true, 2, "oàn"), session.OnChar('n'));
    }
}
