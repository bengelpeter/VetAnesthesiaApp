using VetAnesthesiaApp.Services.Voice;

namespace VetAnesthesiaApp.Tests;

public class VoiceTranscriptSegmenterTests
{
    [Fact]
    public void Split_ReturnsSingleCommand_WhenOnlyOneCommandIsPresent()
    {
        var commands = VoiceTranscriptSegmenter.Split("heart rate 90");

        Assert.Equal(new[] { "heart rate 90" }, commands);
    }

    [Fact]
    public void Split_BreaksCombinedDictationIntoMultipleCommands()
    {
        var commands = VoiceTranscriptSegmenter.Split("heart rate 90 respiratory rate 12 oxygen saturation 98");

        Assert.Equal(
            new[]
            {
                "heart rate 90",
                "respiratory rate 12",
                "oxygen saturation 98"
            },
            commands);
    }
}
