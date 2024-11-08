using Engine.Utils;
using Xunit;

namespace Engine.Tests.Utils;

public class TextDiffHelperTests
{
    [Fact]
    public void Test_Adding_Line_Among_Three()
    {
        var original = "Linje 1\nLinje 2\nLinje 3";
        var updated = "Linje 1\nLinje 2\nNy linje added\nLinje 3";

        var result = TextDiffHelper.HighlightChanges(original, updated);

        Assert.Equal("Linje 1\nLinje 2\n<span class='added'>Ny linje added</span>\nLinje 3", result);
    }

    [Fact]
    public void Test_Removing_Line_Among_Three()
    {
        var original = "Linje 1\nLinje 2\nLinje 3";
        var updated = "Linje 1\nLinje 3";

        var result = TextDiffHelper.HighlightChanges(original, updated);

        Assert.Equal("Linje 1\n<span class='removed'>Linje 2</span>\nLinje 3", result);
    }

    [Fact]
    public void Test_Changing_Two_Words_In_Second_Line()
    {
        var original = "Linje 1\nLinje med noget og andet";
        var updated = "Linje 1\nLinje med lol og pep";

        var expected =
            "Linje 1\n<span class='added'>Linje med lol og pep</span>\n<span class='removed'>Linje med noget og andet</span>";
        var actual = TextDiffHelper.HighlightChanges(original, updated);

        Assert.Equal(expected, actual);
    }
}