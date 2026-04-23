using CarpetPC.Core.Agent;
using Xunit;

namespace CarpetPC.Tests;

public sealed class AgentActionValidatorTests
{
    [Fact]
    public void Validate_BlocksLowConfidenceActions()
    {
        var validator = new AgentActionValidator();
        var action = new AgentAction(AgentActionKind.OpenUrl, "https://example.com", null, 0.10, RiskLevel.Low, "Open site.");

        var result = validator.Validate(action, riskyActionsConfirmed: false);

        Assert.False(result.IsAllowed);
        Assert.False(result.RequiresConfirmation);
    }

    [Fact]
    public void Validate_RequiresConfirmationForHighRiskActions()
    {
        var validator = new AgentActionValidator();
        var action = new AgentAction(AgentActionKind.Click, "Buy button", null, 0.90, RiskLevel.High, "Click buy.");

        var result = validator.Validate(action, riskyActionsConfirmed: false);

        Assert.False(result.IsAllowed);
        Assert.True(result.RequiresConfirmation);
    }

    [Fact]
    public void Validate_AllowsConfirmedHighRiskActions()
    {
        var validator = new AgentActionValidator();
        var action = new AgentAction(AgentActionKind.Click, "Install button", null, 0.90, RiskLevel.High, "Click install.");

        var result = validator.Validate(action, riskyActionsConfirmed: true);

        Assert.True(result.IsAllowed);
    }
}
