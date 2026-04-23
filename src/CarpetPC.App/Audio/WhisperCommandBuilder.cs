namespace CarpetPC.App.Audio;

public sealed record WhisperCommand(string ExecutablePath, string Arguments, string OutputTextPath);

public static class WhisperCommandBuilder
{
    public static WhisperCommand Build(string executablePath, string modelPath, string wavPath, string outputBasePath)
    {
        var arguments = $"-m \"{modelPath}\" -f \"{wavPath}\" -l en -nt -otxt -of \"{outputBasePath}\"";
        return new WhisperCommand(executablePath, arguments, $"{outputBasePath}.txt");
    }
}

