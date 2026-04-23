using CarpetPC.Core;
using CarpetPC.Core.Audio;
using CarpetPC.Core.Models;
using NAudio.Wave;
using System.Diagnostics;
using System.IO;

namespace CarpetPC.App.Audio;

public sealed class WhisperSpeechTranscriber(
    ModelSetupService modelSetupService,
    CarpetPaths paths,
    MicrophoneSelection microphoneSelection,
    IRuntimeLog runtimeLog) : ISpeechTranscriber
{
    private static readonly string[] WhisperExecutableNames = ["whisper-cli.exe", "whisper.exe", "main.exe"];

    public async Task<TranscriptSegment> ListenForCommandAsync(CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.Now;
        var deviceNumber = microphoneSelection.SelectedDeviceNumber ?? 0;
        var tempDirectory = Path.Combine(paths.LogDirectory, "temp");
        Directory.CreateDirectory(tempDirectory);

        var wavPath = Path.Combine(tempDirectory, $"command-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.wav");
        runtimeLog.Info($"Recording command from mic {deviceNumber}...");
        await RecordCommandAsync(deviceNumber, wavPath, cancellationToken);

        runtimeLog.Info("Transcribing command with whisper.cpp...");
        var transcript = await TranscribeAsync(wavPath, cancellationToken);
        var parsed = WhisperTranscriptParser.Parse(transcript);
        if (string.IsNullOrWhiteSpace(parsed))
        {
            throw new InvalidOperationException("Whisper returned an empty transcript.");
        }

        return new TranscriptSegment(parsed, 0.80, startedAt, DateTimeOffset.Now);
    }

    private static async Task RecordCommandAsync(int deviceNumber, string wavPath, CancellationToken cancellationToken)
    {
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startedAt = DateTimeOffset.Now;
        var lastSpeechAt = DateTimeOffset.Now;
        var hasSpeech = false;

        using var waveIn = new WaveInEvent
        {
            DeviceNumber = deviceNumber,
            WaveFormat = new WaveFormat(16_000, 16, 1),
            BufferMilliseconds = 50
        };
        await using var writer = new WaveFileWriter(wavPath, waveIn.WaveFormat);

        waveIn.DataAvailable += (_, e) =>
        {
            writer.Write(e.Buffer, 0, e.BytesRecorded);
            var level = GetPeakLevel(e.Buffer, e.BytesRecorded);
            var now = DateTimeOffset.Now;
            if (level > 0.025f)
            {
                hasSpeech = true;
                lastSpeechAt = now;
            }

            var elapsed = now - startedAt;
            var silence = now - lastSpeechAt;
            if (elapsed >= TimeSpan.FromSeconds(8) || (hasSpeech && elapsed >= TimeSpan.FromSeconds(2) && silence >= TimeSpan.FromSeconds(1.1)))
            {
                waveIn.StopRecording();
            }
        };

        waveIn.RecordingStopped += (_, _) => stopped.TrySetResult();
        waveIn.StartRecording();

        await using var registration = cancellationToken.Register(() =>
        {
            waveIn.StopRecording();
            stopped.TrySetCanceled(cancellationToken);
        });

        await stopped.Task.WaitAsync(cancellationToken);
        writer.Flush();
    }

    private async Task<string> TranscribeAsync(string wavPath, CancellationToken cancellationToken)
    {
        var executablePath = FindWhisperExecutable()
            ?? throw new FileNotFoundException("whisper.cpp runtime missing. Install it from Model Setup.");
        var modelPath = FindWhisperModel()
            ?? throw new FileNotFoundException("Whisper model missing. Install ggml-tiny.en.bin from Model Setup.");
        var outputBase = Path.Combine(Path.GetDirectoryName(wavPath)!, Path.GetFileNameWithoutExtension(wavPath));
        var command = WhisperCommandBuilder.Build(executablePath, modelPath, wavPath, outputBase);

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = command.ExecutablePath,
            Arguments = command.Arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        if (process is null)
        {
            throw new InvalidOperationException("Could not start whisper.cpp.");
        }

        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"whisper.cpp failed: {stderr}");
        }

        return File.Exists(command.OutputTextPath)
            ? await File.ReadAllTextAsync(command.OutputTextPath, cancellationToken)
            : stdout;
    }

    private string? FindWhisperExecutable()
    {
        var runtimeRoot = modelSetupService.GetRuntimeDirectory();
        return Directory.Exists(runtimeRoot)
            ? Directory.EnumerateFiles(runtimeRoot, "*.*", SearchOption.AllDirectories)
                .FirstOrDefault(path => WhisperExecutableNames.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
            : null;
    }

    private string? FindWhisperModel()
    {
        var model = modelSetupService.GetAvailableModels()
            .Where(item => item.Kind == ModelAssetKind.SpeechModel)
            .OrderBy(item => item.Required ? 0 : 1)
            .FirstOrDefault(modelSetupService.IsModelPresent);

        return model is null ? null : modelSetupService.GetAssetPath(model);
    }

    private static float GetPeakLevel(byte[] buffer, int bytesRecorded)
    {
        float max = 0;
        for (var index = 0; index < bytesRecorded; index += 2)
        {
            var sample = BitConverter.ToInt16(buffer, index) / 32768f;
            max = Math.Max(max, Math.Abs(sample));
        }

        return max;
    }
}
