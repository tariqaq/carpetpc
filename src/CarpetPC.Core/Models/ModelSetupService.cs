using CarpetPC.Core;
using System.Net.Http;
using System.IO.Compression;

namespace CarpetPC.Core.Models;

public sealed class ModelSetupService(ModelCatalog catalog, CarpetPaths paths, HttpClient? httpClient = null)
{
    private readonly HttpClient _httpClient = httpClient ?? new HttpClient();

    public IReadOnlyList<ModelCatalogItem> GetAvailableModels() => catalog.Items;

    public string GetModelDirectory() => paths.ModelDirectory;

    public string GetRuntimeDirectory() => paths.RuntimeDirectory;

    public bool IsModelPresent(ModelCatalogItem item)
    {
        if (File.Exists(GetAssetPath(item)))
        {
            return true;
        }

        if (item.Kind == ModelAssetKind.AgentModel && Directory.Exists(Path.Combine(paths.ModelDirectory, item.Kind.ToString())))
        {
            return Directory.EnumerateFiles(Path.Combine(paths.ModelDirectory, item.Kind.ToString()), "*.gguf", SearchOption.AllDirectories).Any();
        }

        if (item.Kind == ModelAssetKind.VisionProjector)
        {
            return FindVisionProjector() is not null;
        }

        if (item.Kind != ModelAssetKind.Runtime)
        {
            return false;
        }

        return FindRuntimeExecutable(item.FileName) is not null;
    }

    public bool AreRequiredAssetsPresent() => catalog.Items.Where(item => item.Required).All(IsModelPresent);

    public bool IsReadyForVoiceTest()
    {
        var hasWhisperRuntime = catalog.Items
            .Where(item => item.Kind == ModelAssetKind.Runtime && item.Family.Equals("whisper.cpp", StringComparison.OrdinalIgnoreCase))
            .Any(IsModelPresent);
        var hasSpeechModel = catalog.Items
            .Where(item => item.Kind == ModelAssetKind.SpeechModel && item.Required)
            .Any(IsModelPresent);

        return hasWhisperRuntime && hasSpeechModel;
    }

    public Task<ModelDownloadPlan> CreateDownloadPlanAsync(ModelCatalogItem item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var destination = GetAssetPath(item);
        return Task.FromResult(new ModelDownloadPlan(item, destination, RequiresExplicitConfirmation: true));
    }

    public string GetAssetPath(ModelCatalogItem item)
    {
        var root = item.Kind == ModelAssetKind.Runtime ? paths.RuntimeDirectory : paths.ModelDirectory;
        return Path.Combine(root, item.Kind.ToString(), item.FileName);
    }

    public string? FindAgentModel()
    {
        var configured = catalog.Items.FirstOrDefault(item => item.Kind == ModelAssetKind.AgentModel);
        if (configured is not null && File.Exists(GetAssetPath(configured)))
        {
            return GetAssetPath(configured);
        }

        var agentDirectory = Path.Combine(paths.ModelDirectory, ModelAssetKind.AgentModel.ToString());
        return Directory.Exists(agentDirectory)
            ? Directory.EnumerateFiles(agentDirectory, "*.gguf", SearchOption.AllDirectories).FirstOrDefault()
            : null;
    }

    public string? FindVisionProjector()
    {
        var configured = catalog.Items.FirstOrDefault(item => item.Kind == ModelAssetKind.VisionProjector);
        if (configured is not null && File.Exists(GetAssetPath(configured)))
        {
            return GetAssetPath(configured);
        }

        var projectorDirectory = Path.Combine(paths.ModelDirectory, ModelAssetKind.VisionProjector.ToString());
        if (!Directory.Exists(projectorDirectory))
        {
            return null;
        }

        return Directory
            .EnumerateFiles(projectorDirectory, "*.gguf", SearchOption.AllDirectories)
            .OrderByDescending(path => Path.GetFileName(path).Contains("mmproj", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
    }

    public string? FindRuntimeExecutable(string fileName)
    {
        return Directory.Exists(paths.RuntimeDirectory)
            ? Directory.EnumerateFiles(paths.RuntimeDirectory, fileName, SearchOption.AllDirectories).FirstOrDefault()
            : null;
    }

    public async Task DownloadAsync(
        ModelDownloadPlan plan,
        IProgress<ModelDownloadProgress> progress,
        CancellationToken cancellationToken)
    {
        if (!plan.RequiresExplicitConfirmation)
        {
            throw new InvalidOperationException("Downloads must require explicit confirmation.");
        }

        if (plan.Item.DirectDownloadUri is null)
        {
            throw new InvalidOperationException($"{plan.Item.DisplayName} cannot be downloaded automatically. Train or provide it manually.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(plan.DestinationPath)!);
        var tempPath = $"{plan.DestinationPath}.download";
        using var response = await _httpClient.GetAsync(plan.Item.DirectDownloadUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? plan.Item.ApproximateBytes;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = File.Create(tempPath);

        var buffer = new byte[1024 * 128];
        long downloaded = 0;
        int read;

        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            downloaded += read;
            progress.Report(new ModelDownloadProgress(plan.Item, downloaded, totalBytes));
        }

        output.Close();
        File.Move(tempPath, plan.DestinationPath, overwrite: true);
        ExtractZipRuntimeIfNeeded(plan);
        progress.Report(new ModelDownloadProgress(plan.Item, downloaded, totalBytes));
    }

    private static void ExtractZipRuntimeIfNeeded(ModelDownloadPlan plan)
    {
        if (plan.Item.Kind != ModelAssetKind.Runtime || !plan.DestinationPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var extractDirectory = Path.Combine(
            Path.GetDirectoryName(plan.DestinationPath)!,
            Path.GetFileNameWithoutExtension(plan.DestinationPath));

        if (Directory.Exists(extractDirectory))
        {
            Directory.Delete(extractDirectory, recursive: true);
        }

        ZipFile.ExtractToDirectory(plan.DestinationPath, extractDirectory);
    }
}

public sealed record ModelDownloadPlan(ModelCatalogItem Item, string DestinationPath, bool RequiresExplicitConfirmation);

public sealed record ModelDownloadProgress(ModelCatalogItem Item, long DownloadedBytes, long? TotalBytes)
{
    public double Fraction => TotalBytes is > 0 ? Math.Clamp((double)DownloadedBytes / TotalBytes.Value, 0, 1) : 0;
}
