namespace CarpetPC.Core.Models;

public enum ModelAssetKind
{
    AgentModel,
    VisionProjector,
    SpeechModel,
    WakeWordModel,
    Runtime
}

public sealed record ModelCatalogItem(
    string Id,
    string DisplayName,
    ModelAssetKind Kind,
    string Family,
    string Quantization,
    Uri SourceUri,
    Uri? DirectDownloadUri,
    string FileName,
    long ApproximateBytes,
    string Description,
    bool Required);

public sealed class ModelCatalog
{
    public IReadOnlyList<ModelCatalogItem> Items { get; } =
    [
        new(
            "gemma4-e2b-q4",
            "Gemma 4 E2B Q4",
            ModelAssetKind.AgentModel,
            "Gemma 4",
            "Q4",
            new Uri("https://huggingface.co/dahus/gemma-4-e2b-it-Q4_K_M-GGUF"),
            new Uri("https://huggingface.co/dahus/gemma-4-e2b-it-Q4_K_M-GGUF/resolve/main/gemma-4-e2b-q4km.gguf"),
            "gemma-4-E2B-it-Q4_K_M.gguf",
            3_190_000_000L,
            "Default local agent model candidate. Download only after user confirmation.",
            true),
        new(
            "llama-runtime-win-x64",
            "llama.cpp Windows x64 runtime",
            ModelAssetKind.Runtime,
            "llama.cpp",
            "win-x64",
            new Uri("https://github.com/ggml-org/llama.cpp/releases"),
            null,
            "llama-server.exe",
            60L * 1024L * 1024L,
            "Runtime containing llama-server.exe. Install manually until a stable direct runtime URL is configured.",
            true),
        new(
            "gemma4-e2b-mmproj",
            "Gemma 4 E2B vision projector",
            ModelAssetKind.VisionProjector,
            "Gemma 4",
            "mmproj",
            new Uri("https://huggingface.co/dahus/gemma-4-e2b-it-Q4_K_M-GGUF"),
            null,
            "mmproj-gemma-4-E2B-it.gguf",
            800L * 1024L * 1024L,
            "Optional multimodal projector for screenshot understanding. Place a compatible mmproj GGUF here manually.",
            false),
        new(
            "whisper-runtime-win-x64",
            "whisper.cpp Windows x64 runtime",
            ModelAssetKind.Runtime,
            "whisper.cpp",
            "win-x64",
            new Uri("https://github.com/ggml-org/whisper.cpp/releases"),
            new Uri("https://github.com/ggml-org/whisper.cpp/releases/download/v1.8.4/whisper-bin-x64.zip"),
            "whisper-bin-x64.zip",
            35L * 1024L * 1024L,
            "Windows CPU runtime containing whisper-cli.exe. Download only after user confirmation.",
            true),
        new(
            "whisper-tiny-en",
            "Whisper tiny.en",
            ModelAssetKind.SpeechModel,
            "Whisper",
            "ggml",
            new Uri("https://github.com/ggml-org/whisper.cpp"),
            new Uri("https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.en.bin"),
            "ggml-tiny.en.bin",
            75L * 1024L * 1024L,
            "Fast CPU speech-to-text model for wake phrase and quick command transcription.",
            true),
        new(
            "whisper-base-en",
            "Whisper base.en",
            ModelAssetKind.SpeechModel,
            "Whisper",
            "ggml",
            new Uri("https://github.com/ggml-org/whisper.cpp"),
            new Uri("https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.en.bin"),
            "ggml-base.en.bin",
            150L * 1024L * 1024L,
            "Higher quality CPU speech-to-text model for post-wake commands.",
            false),
        new(
            "hey-carpet-wake",
            "Hey Carpet Wake Word",
            ModelAssetKind.WakeWordModel,
            "Wake word",
            "ONNX",
            new Uri("https://github.com/livekit/livekit-wakeword"),
            null,
            "hey-carpet.onnx",
            10L * 1024L * 1024L,
            "Custom wake-word model; train locally first, then place the exported ONNX here.",
            true)
    ];
}
