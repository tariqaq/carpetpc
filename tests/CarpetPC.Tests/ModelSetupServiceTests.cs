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
    public void AreRequiredAssetsPresent_IsFalseWhenAssetsAreMissing()
    {
        var service = new ModelSetupService(new ModelCatalog(), new CarpetPaths());

        Assert.False(service.AreRequiredAssetsPresent());
    }

    [Fact]
    public void IsReadyForVoiceTest_IsFalseWhenWhisperAssetsAreMissing()
    {
        var service = new ModelSetupService(new ModelCatalog(), new CarpetPaths());

        Assert.False(service.IsReadyForVoiceTest());
    }

    [Fact]
    public void WakeWordModel_IsManualUntilTrainingExportsOnnx()
    {
        var service = new ModelSetupService(new ModelCatalog(), new CarpetPaths());

        var wakeModel = service.GetAvailableModels().Single(model => model.Kind == ModelAssetKind.WakeWordModel);

        Assert.Null(wakeModel.DirectDownloadUri);
        Assert.Equal("hey-carpet.onnx", wakeModel.FileName);
    }
}
