using AdsunVietKey.Core.Conversion;
using Xunit;

namespace AdsunVietKey.Core.Tests;

public class ConverterTests
{
    private const string AllLower =
        "aáàảãạăắằẳẵặâấầẩẫậeéèẻẽẹêếềểễệiíìỉĩịoóòỏõọôốồổỗộơớờởỡợuúùủũụưứừửữựyýỳỷỹỵđ";

    private static readonly string AllUpper = AllLower.ToUpperInvariant();

    [Theory]
    [InlineData("Việt Nam", "ViÖt Nam")]
    [InlineData("Đại học", "§¹i häc")]
    [InlineData("Cộng hòa xã hội chủ nghĩa", "Céng hßa x· héi chñ nghÜa")]
    [InlineData("Thông tin", "Th«ng tin")]
    public void Tcvn3_KnownSamples(string unicode, string tcvn3)
    {
        Assert.Equal(tcvn3, VietConverter.Convert(unicode, VietCharset.Unicode, VietCharset.Tcvn3));
        Assert.Equal(unicode, VietConverter.Convert(tcvn3, VietCharset.Tcvn3, VietCharset.Unicode));
    }

    [Theory]
    [InlineData("Việt Nam", "Vieät Nam")]
    [InlineData("Đại học", "Ñaïi hoïc")]
    [InlineData("Thông tin", "Thoâng tin")]
    [InlineData("Trường", "Tröôøng")]
    [InlineData("KHUYẾN MÃI ĐẶC BIỆT", "KHUYEÁN MAÕI ÑAËC BIEÄT")]
    public void Vni_KnownSamples(string unicode, string vni)
    {
        Assert.Equal(vni, VietConverter.Convert(unicode, VietCharset.Unicode, VietCharset.VniWindows));
        Assert.Equal(unicode, VietConverter.Convert(vni, VietCharset.VniWindows, VietCharset.Unicode));
    }

    [Fact]
    public void Vni_RoundTripsEveryLetter()
    {
        string text = AllLower + AllUpper + " ASCII 123";
        string vni = VietConverter.Encode(text, VietCharset.VniWindows);
        Assert.Equal(text, VietConverter.Decode(vni, VietCharset.VniWindows));
    }

    [Fact]
    public void Tcvn3_RoundTripsLowercaseAndShapedCapitals()
    {
        string text = AllLower + "ĂÂÊÔƠƯĐ";
        string tcvn = VietConverter.Encode(text, VietCharset.Tcvn3);
        Assert.Equal(text, VietConverter.Decode(tcvn, VietCharset.Tcvn3));
    }

    [Fact]
    public void Tcvn3_HasDistinctCodeForEveryLowercaseLetter()
    {
        string tcvn = VietConverter.Encode(AllLower, VietCharset.Tcvn3);
        Assert.Equal(AllLower.Length, tcvn.Distinct().Count());
    }

    [Fact]
    public void Composite_DecomposesToneOnly_AndNormalizesBack()
    {
        string composite = VietConverter.Convert("Việt", VietCharset.Unicode, VietCharset.UnicodeComposite);
        Assert.Equal("Việt", composite);
        Assert.Equal("Việt", VietConverter.Convert(composite, VietCharset.UnicodeComposite, VietCharset.Unicode));
    }

    [Fact]
    public void NoAccent_StripsAllMarks()
    {
        Assert.Equal("Tieng Viet co dau - DUONG", VietConverter.Convert("Tiếng Việt có dấu - ĐƯỜNG", VietCharset.Unicode, VietCharset.NoAccent));
    }

    [Fact]
    public void Tcvn3_ToVni_Directly()
    {
        Assert.Equal("Vieät Nam", VietConverter.Convert("ViÖt Nam", VietCharset.Tcvn3, VietCharset.VniWindows));
    }
}
