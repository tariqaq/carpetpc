namespace CarpetPC.App.Audio;

public static class WhisperTranscriptParser
{
    public static string Parse(string text)
    {
        return text
            .Replace("[BLANK_AUDIO]", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("[MUSIC]", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
    }
}

