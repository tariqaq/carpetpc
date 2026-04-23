using CarpetPC.Core;
using Xunit;

namespace CarpetPC.Tests;

public sealed class ResourceBudgetServiceTests
{
    [Fact]
    public void SelectProfile_UsesCpuSafeWhenVramIsUnknown()
    {
        var service = new ResourceBudgetService();
        var snapshot = new ResourceSnapshot(null, null, 100 * 1024 * 1024, DateTimeOffset.UtcNow);

        var profile = service.SelectProfile(snapshot);

        Assert.Equal(RuntimeProfile.CpuSafe, profile);
    }

    [Fact]
    public void SelectProfile_UsesWakeOnlyWhenRamBudgetIsExceeded()
    {
        var service = new ResourceBudgetService();
        var snapshot = new ResourceSnapshot(8L * 1024 * 1024 * 1024, 6L * 1024 * 1024 * 1024, 7L * 1024 * 1024 * 1024, DateTimeOffset.UtcNow);

        var profile = service.SelectProfile(snapshot);

        Assert.Equal(RuntimeProfile.WakeOnly, profile);
    }
}
