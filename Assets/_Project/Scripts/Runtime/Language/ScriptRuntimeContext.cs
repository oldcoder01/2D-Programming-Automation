using System;
using System.Collections.Generic;

public sealed class ScriptRuntimeContext
{
    public WorldController WorldController;
    public GameLog GameLog;
    public ScriptBuiltInRegistry BuiltInRegistry;

    public Action<int> ExecutionLineChanged;
    public Action<int, string> RuntimeErrorRaised;

    public readonly Dictionary<string, ScriptFunctionDefinitionStatement> UserFunctions = new Dictionary<string, ScriptFunctionDefinitionStatement>();

    public bool IsRunning;
    public int StepCounter;
    public int MaxSteps = 10000;
    public int CallDepth;
    public int MaxCallDepth = 32;

    public void NotifyExecutionLine(int lineNumber)
    {
        if (lineNumber <= 0)
        {
            return;
        }

        if (ExecutionLineChanged != null)
        {
            ExecutionLineChanged(lineNumber);
        }
    }

    public void ReportRuntimeError(int lineNumber, string message)
    {
        if (RuntimeErrorRaised != null)
        {
            RuntimeErrorRaised(lineNumber, message);
        }
    }

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