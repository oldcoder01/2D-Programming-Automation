using System.Collections.Generic;

public abstract class ScriptStatement
{
    public int LineNumber;
}

public sealed class ScriptBlockStatement : ScriptStatement
{
    public readonly List<ScriptStatement> Statements = new List<ScriptStatement>();
}

public sealed class ScriptCallStatement : ScriptStatement
{
    public string Name;
}

public sealed class ScriptIfStatement : ScriptStatement
{
    public ScriptExpression Condition;
    public readonly ScriptBlockStatement ThenBlock = new ScriptBlockStatement();
    public readonly List<ScriptElifBranch> ElifBranches = new List<ScriptElifBranch>();
    public ScriptBlockStatement ElseBlock;
}

public sealed class ScriptElifBranch
{
    public int LineNumber;
    public ScriptExpression Condition;
    public readonly ScriptBlockStatement Block = new ScriptBlockStatement();
}

public sealed class ScriptWhileStatement : ScriptStatement
{
    public ScriptExpression Condition;
    public readonly ScriptBlockStatement Block = new ScriptBlockStatement();
}

public sealed class ScriptFunctionDefinitionStatement : ScriptStatement
{
    public string Name;
    public readonly ScriptBlockStatement Block = new ScriptBlockStatement();
}