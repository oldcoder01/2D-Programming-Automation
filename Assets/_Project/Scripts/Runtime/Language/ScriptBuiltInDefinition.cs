public enum ScriptBuiltInKind
{
    Action,
    Query
}

public sealed class ScriptBuiltInDefinition
{
    public string Name;
    public ScriptBuiltInKind Kind;
    public bool UnlockedByDefault;
    public string RequiredUpgradeId;
    public string Description;
}