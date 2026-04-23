using CarpetPC.Core;
using CarpetPC.Core.Models;
using Xunit;

namespace CarpetPC.Tests;

public sealed class ModelSetupServiceTests
{
    [Fact]
    public async Task CreateDownloadPlanAsync_RequiresExplicitConfirmation()
    {
        var service = new ModelSetupService(new ModelCatalog(), new CarpetPaths());
        var item = service.GetAvailableModels().First();

        var plan = await service.CreateDownloadPlanAsync(item, CancellationToken.None);

        Assert.True(plan.RequiresExplicitConfirmation);
        Assert.EndsWith(item.FileName, plan.DestinationPath);
    }

    [Fact]
    public void WakeWordModel_IsManualUntilTrainingExportsOnnx()
    {
        var service = new ModelSetupService(new ModelCatalog(), new CarpetPaths());

        var wakeModel = service.GetAvailableModels().Single(model => model.Kind == ModelAssetKind.WakeWordModel);

        Assert.Null(wakeModel.DirectDownloadUri);
        Assert.Equal("hey-carpet.onnx", wakeModel.FileName);
    }

    [Fact]
    public void AgentModelCatalog_UsesKnownDownloadedGemmaFilename()
    {
        var service = new ModelSetupService(new ModelCatalog(), new CarpetPaths());

        var agentModel = service.GetAvailableModels().Single(model => model.Kind == ModelAssetKind.AgentModel);

        Assert.Equal("gemma-4-E2B-it-Q4_K_M.gguf", agentModel.FileName);
    }

    [Fact]
    public void VisionProjector_IsOptionalManualAsset()
    {
        var service = new ModelSetupService(new ModelCatalog(), new CarpetPaths());

        var projector = service.GetAvailableModels().Single(model => model.Kind == ModelAssetKind.VisionProjector);

        Assert.False(projector.Required);
        Assert.Null(projector.DirectDownloadUri);
        Assert.Contains("mmproj", projector.FileName);
    }
}
