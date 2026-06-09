using VetAnesthesiaApp.Constants;
using VetAnesthesiaApp.Services.Voice;

namespace VetAnesthesiaApp.Tests;

public class VoiceParserServiceTests
{
    private readonly VoiceParserService _parser = new(new SpokenNumberParser());

    [Fact]
    public void Parse_MapsSpo2WithoutBeingStolenByOxygenAlias()
    {
        var result = _parser.Parse("oxygen saturation 98", DateTime.Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(VoiceCommandType.FieldValue, result.CommandType);
        Assert.Equal(AnesthesiaFieldKeys.Spo2, result.FieldKey);
        Assert.Equal(98, result.ParsedNumericValue);
    }

    [Fact]
    public void Parse_NormalizesPhoneticIsoInput()
    {
        var result = _parser.Parse("i sell two point five", DateTime.Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(AnesthesiaFieldKeys.IsoPercent, result.FieldKey);
        Assert.Equal(2.5m, result.ParsedNumericValue);
        Assert.Equal("isoflurane two point five", result.NormalizedTranscript);
    }

    [Fact]
    public void Parse_RecognizesNotes()
    {
        var result = _parser.Parse("note patient swallowing lightly", DateTime.Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(VoiceCommandType.Note, result.CommandType);
        Assert.Equal("patient swallowing lightly", result.NoteText);
    }

    [Theory]
    [InlineData("next bucket")]
    [InlineData("next time")]
    [InlineData("new time")]
    public void Parse_RecognizesBucketAdvanceCommands(string transcript)
    {
        var result = _parser.Parse(transcript, DateTime.Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(VoiceCommandType.NextBucket, result.CommandType);
    }

    [Fact]
    public void Parse_FailsCleanly_ForUnknownTranscript()
    {
        var result = _parser.Parse("hello there", DateTime.Now);

        Assert.False(result.IsSuccess);
        Assert.Equal("No command match.", result.StatusMessage);
    }
}
