namespace CarpetPC.Core.Agent;

public sealed class AgentActionValidator
{
    private const double MinimumConfidence = 0.35;

    public ValidationResult Validate(AgentAction action, bool riskyActionsConfirmed)
    {
        if (action.Confidence < MinimumConfidence)
        {
            return ValidationResult.Blocked("Action confidence is too low.");
        }

        if (string.IsNullOrWhiteSpace(action.Summary))
        {
            return ValidationResult.Blocked("Action summary is required for the live log.");
        }

        if (action.RiskLevel == RiskLevel.High && !riskyActionsConfirmed)
        {
            return ValidationResult.NeedsConfirmation("Risky action requires user confirmation.");
        }

        return action.Action switch
        {
            AgentActionKind.OpenApp or AgentActionKind.OpenUrl or AgentActionKind.Click
                when string.IsNullOrWhiteSpace(action.Target)
                => ValidationResult.Blocked("Target is required."),
            AgentActionKind.Type when string.IsNullOrWhiteSpace(action.Text)
                => ValidationResult.Blocked("Text is required."),
            _ => ValidationResult.Allowed()
        };
    }
}

public sealed record ValidationResult(bool IsAllowed, bool RequiresConfirmation, string? Reason)
{
    public static ValidationResult Allowed() => new(true, false, null);

    public static ValidationResult Blocked(string reason) => new(false, false, reason);

    public static ValidationResult NeedsConfirmation(string reason) => new(false, true, reason);
}

