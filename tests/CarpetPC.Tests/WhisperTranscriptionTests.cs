using CarpetPC.App.Audio;
using Xunit;

namespace CarpetPC.Tests;

public sealed class WhisperTranscriptionTests
{
    [Fact]
    public void Parse_RemovesKnownNoiseMarkersAndTrimsText()
    {
        var transcript = WhisperTranscriptParser.Parse("  [BLANK_AUDIO] open steam [MUSIC]  ");

        Assert.Equal("open steam", transcript);
    }

    [Fact]
    public void Build_CreatesOutputTextPathAndUsesCpuFriendlyArguments()
    {
        var command = WhisperCommandBuilder.Build("whisper-cli.exe", "model.bin", "input.wav", "out");

        Assert.Equal("out.txt", command.OutputTextPath);
        Assert.Contains("-m \"model.bin\"", command.Arguments);
        Assert.Contains("-f \"input.wav\"", command.Arguments);
        Assert.Contains("-nt", command.Arguments);
        Assert.Contains("-otxt", command.Arguments);
    }
}
