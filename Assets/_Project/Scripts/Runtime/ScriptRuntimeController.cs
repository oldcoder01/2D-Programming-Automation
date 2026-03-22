using System;
using System.Collections;
using UnityEngine;

public sealed class ScriptRuntimeController : MonoBehaviour
{
    [SerializeField] private WorldController _worldController;
    [SerializeField] private GameLog _gameLog;
    [SerializeField] private float _stepDelay = 0.35f;

    private Coroutine _runtimeCoroutine;
    private bool _isRunning;

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
            WriteError(exception.Message);
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
        }
    }

    private IEnumerator RunScriptCoroutine(ScriptBlockStatement root)
    {
        _isRunning = true;
        WriteLog(ScriptMessageFormatter.RuntimeStarted());

        ScriptRuntimeContext context = new ScriptRuntimeContext();
        context.WorldController = _worldController;
        context.GameLog = _gameLog;
        context.BuiltInRegistry = new ScriptBuiltInRegistry();
        context.IsRunning = true;

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
            WriteError(errorMessage);
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
        }
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