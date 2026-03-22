using System.Collections.Generic;

public sealed class ScriptRuntimeContext
{
    public WorldController WorldController;
    public GameLog GameLog;
    public ScriptBuiltInRegistry BuiltInRegistry;

    public readonly Dictionary<string, ScriptFunctionDefinitionStatement> UserFunctions = new Dictionary<string, ScriptFunctionDefinitionStatement>();

    public bool IsRunning;
    public int StepCounter;
    public int MaxSteps = 10000;
    public int CallDepth;
    public int MaxCallDepth = 32;

    public void WriteLine(string message)
    {
        if (GameLog != null)
        {
            GameLog.WriteInfo(message);
        }
    }

    public void WriteInfo(string message)
    {
        if (GameLog != null)
        {
            GameLog.WriteInfo(message);
        }
    }

    public void WriteSuccess(string message)
    {
        if (GameLog != null)
        {
            GameLog.WriteSuccess(message);
        }
    }

    public void WriteWarning(string message)
    {
        if (GameLog != null)
        {
            GameLog.WriteWarning(message);
        }
    }

    public void WriteError(string message)
    {
        if (GameLog != null)
        {
            GameLog.WriteError(message);
        }
    }

    public bool IsBuiltInUnlocked(ScriptBuiltInDefinition definition)
    {
        if (definition == null)
        {
            return false;
        }

        if (definition.UnlockedByDefault)
        {
            return true;
        }

        return false;
    }
}