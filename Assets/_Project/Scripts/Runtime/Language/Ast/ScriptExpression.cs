public abstract class ScriptExpression
{
    public int LineNumber;
}

public sealed class ScriptBoolLiteralExpression : ScriptExpression
{
    public bool Value;
}

public sealed class ScriptCallExpression : ScriptExpression
{
    public string Name;
}

public sealed class ScriptUnaryExpression : ScriptExpression
{
    public string Operator;
    public ScriptExpression Operand;
}

public sealed class ScriptBinaryExpression : ScriptExpression
{
    public string Operator;
    public ScriptExpression Left;
    public ScriptExpression Right;
}