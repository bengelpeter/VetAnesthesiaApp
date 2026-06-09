using VetAnesthesiaApp.Services.Voice;

namespace VetAnesthesiaApp.Tests;

public class SpokenNumberParserTests
{
    private readonly SpokenNumberParser _parser = new();

    [Theory]
    [InlineData("98", 98)]
    [InlineData("ninety eight", 98)]
    [InlineData("one twenty", 120)]
    [InlineData("one twenty five", 125)]
    [InlineData("one oh five", 105)]
    [InlineData("two point five", 2.5)]
    [InlineData("0.75", 0.75)]
    public void Parse_ReturnsExpectedValue_ForSupportedInput(string input, decimal expected)
    {
        var result = _parser.Parse(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("oxygen saturation")]
    [InlineData("point five")]
    [InlineData("two bananas")]
    public void Parse_ReturnsNull_ForUnsupportedInput(string input)
    {
        var result = _parser.Parse(input);

        Assert.Null(result);
    }
}
