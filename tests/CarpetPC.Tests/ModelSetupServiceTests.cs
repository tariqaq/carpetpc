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
    }
}
