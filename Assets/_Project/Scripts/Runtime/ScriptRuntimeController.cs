using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

public sealed class ScriptRuntimeController : MonoBehaviour
{
    [SerializeField] private WorldController _worldController;
    [SerializeField] private GameLog _gameLog;
    [SerializeField] private float _stepDelay = 0.35f;

    private Coroutine _runtimeCoroutine;
    private bool _isRunning;

    public event Action RuntimeStarted;
    public event Action RuntimeStopped;
    public event Action RuntimeFinished;
    public event Action<int> ExecutionLineChanged;
    public event Action<int, string> RuntimeErrorRaised;

    public bool IsRunning
    {
        get { return _isRunning; }
    }

    public void RunSource(string sourceCode)
    {
        if (_isRunning)
        {
            WriteLog(ScriptMessageFormatter.RuntimeAlreadyRunning());
            return;
        }

        if (_worldController == null)
        {
            Debug.LogError(ScriptMessageFormatter.MissingWorldControllerReference());
            return;
        }

        try
        {
            ScriptLexer lexer = new ScriptLexer();
            ScriptParser parser = new ScriptParser();

            ScriptBlockStatement root = parser.Parse(lexer.Tokenize(sourceCode));
            _runtimeCoroutine = StartCoroutine(RunScriptCoroutine(root));
        }
        catch (Exception exception)
        {
            int lineNumber = ExtractLineNumber(exception.Message);
            RaiseRuntimeError(lineNumber, exception.Message);
        }
    }

    public void StopRuntime()
    {
        if (_runtimeCoroutine != null)
        {
            StopCoroutine(_runtimeCoroutine);
            _runtimeCoroutine = null;
        }

        if (_isRunning)
        {
            _isRunning = false;
            WriteLog(ScriptMessageFormatter.RuntimeStopped());

            if (RuntimeStopped != null)
            {
                RuntimeStopped();
            }
        }
    }

    private IEnumerator RunScriptCoroutine(ScriptBlockStatement root)
    {
        _isRunning = true;
        WriteLog(ScriptMessageFormatter.RuntimeStarted());

        if (RuntimeStarted != null)
        {
            RuntimeStarted();
        }

        ScriptRuntimeContext context = new ScriptRuntimeContext();
        context.WorldController = _worldController;
        context.GameLog = _gameLog;
        context.BuiltInRegistry = new ScriptBuiltInRegistry();
        context.IsRunning = true;
        context.ExecutionLineChanged = HandleExecutionLineChanged;
        context.RuntimeErrorRaised = HandleRuntimeErrorRaised;

        ScriptInterpreter interpreter = new ScriptInterpreter();
        IEnumerator executionCoroutine = null;
        bool failedToStart = false;
        string errorMessage = string.Empty;

        try
        {
            executionCoroutine = interpreter.ExecuteRootCoroutine(root, context, _stepDelay);
        }
        catch (Exception exception)
        {
            failedToStart = true;
            errorMessage = exception.Message;
        }

        if (failedToStart)
        {
            int failedLineNumber = ExtractLineNumber(errorMessage);
            RaiseRuntimeError(failedLineNumber, errorMessage);
            _isRunning = false;
            _runtimeCoroutine = null;
            yield break;
        }

        yield return StartCoroutine(executionCoroutine);

        _isRunning = false;
        _runtimeCoroutine = null;

        if (context.IsRunning)
        {
            WriteLog(ScriptMessageFormatter.RuntimeFinished());

            if (RuntimeFinished != null)
            {
                RuntimeFinished();
            }
        }
    }

    private void HandleExecutionLineChanged(int lineNumber)
    {
        if (ExecutionLineChanged != null)
        {
            ExecutionLineChanged(lineNumber);
        }
    }

    private void HandleRuntimeErrorRaised(int lineNumber, string message)
    {
        RaiseRuntimeError(lineNumber, message);
    }

    private void RaiseRuntimeError(int lineNumber, string message)
    {
        if (RuntimeErrorRaised != null)
        {
            RuntimeErrorRaised(lineNumber, message);
        }

        WriteError(message);
    }

    private static int ExtractLineNumber(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return -1;
        }

        Match match = Regex.Match(message, @"\bLine\s+(\d+)\b");

        if (!match.Success)
        {
            return -1;
        }

        int lineNumber;
        bool success = int.TryParse(match.Groups[1].Value, out lineNumber);

        if (!success)
        {
            return -1;
        }

        return lineNumber;
    }

    private void WriteLog(string message)
    {
        if (_gameLog != null)
        {
            _gameLog.WriteInfo(message);
            return;
        }

        Debug.Log(message);
    }

    private void WriteError(string message)
    {
        if (_gameLog != null)
        {
            _gameLog.WriteError(message);
            return;
        }

        Debug.LogError(message);
    }
}