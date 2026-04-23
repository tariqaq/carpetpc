namespace CarpetPC.Core.Agent;

public enum AgentActionKind
{
    OpenApp,
    OpenUrl,
    Click,
    Type,
    KeyPress,
    Wait,
    AskConfirmation,
    Finish,
    Abort
}

public enum RiskLevel
{
    Low,
    Medium,
    High
}

public sealed record AgentAction(
    AgentActionKind Action,
    string Target,
    string? Text,
    double Confidence,
    RiskLevel RiskLevel,
    string Summary);

