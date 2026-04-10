using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    [SerializeField] private WorldController _worldController;
    [SerializeField] private ScriptRuntimeController _scriptRuntimeController;
    [SerializeField] private DroneController _droneController;
    [SerializeField] private GameLog _gameLog;
    [SerializeField] private StatusPanelUI _statusPanelUI;
    [SerializeField] private ObjectiveController _objectiveController;
    [SerializeField] private CodeViewerPresenter _codeViewerPresenter;

    private void OnEnable()
    {
        SubscribeToRuntimeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromRuntimeEvents();
    }

    private void Start()
    {
        if (_gameLog != null)
        {
            _gameLog.Clear();
            _gameLog.WriteLine("System ready.");
        }

        if (_codeViewerPresenter != null)
        {
            CodeDocument document = _codeViewerPresenter.GetDocument();

            if (document != null && string.IsNullOrEmpty(document.Text))
            {
                _codeViewerPresenter.SetSourceText(string.Empty);
            }

            _codeViewerPresenter.ClearRuntimeLineState();
        }

        RefreshStatusPanel();
    }

    private void Update()
    {
        RefreshStatusPanel();
    }

    public void RunScript()
    {
        if (_scriptRuntimeController == null)
        {
            Debug.LogError("GameManager is missing ScriptRuntimeController reference.");
            return;
        }

        if (_codeViewerPresenter == null)
        {
            Debug.LogError("GameManager is missing CodeViewerPresenter reference.");
            return;
        }

        CodeDocument document = _codeViewerPresenter.GetDocument();

        if (document == null)
        {
            Debug.LogError("GameManager could not read the current code document.");
            return;
        }

        string sourceText = document.Text;

        if (string.IsNullOrWhiteSpace(sourceText))
        {
            if (_gameLog != null)
            {
                _gameLog.WriteWarning("No code to run.");
            }

            _codeViewerPresenter.ClearRuntimeLineState();
            RefreshStatusPanel();
            return;
        }

        _codeViewerPresenter.ClearRuntimeLineState();
        _scriptRuntimeController.RunSource(sourceText);
        RefreshStatusPanel();
    }

    public void StopScript()
    {
        if (_scriptRuntimeController == null)
        {
            Debug.LogError("GameManager is missing ScriptRuntimeController reference.");
            return;
        }

        _scriptRuntimeController.StopRuntime();

        if (_codeViewerPresenter != null)
        {
            _codeViewerPresenter.ClearExecutionLine();
        }

        RefreshStatusPanel();
    }

    public void ResetWorld()
    {
        if (_scriptRuntimeController != null)
        {
            _scriptRuntimeController.StopRuntime();
        }

        if (_worldController == null)
        {
            Debug.LogError("GameManager is missing WorldController reference.");
            return;
        }

        _worldController.ResetWorldState();

        if (_gameLog != null)
        {
            _gameLog.WriteLine("World reset.");
        }

        if (_codeViewerPresenter != null)
        {
            _codeViewerPresenter.ClearRuntimeLineState();
        }

        RefreshStatusPanel();

        if (_objectiveController != null)
        {
            _objectiveController.ResetCurrentObjectiveProgress();
        }
    }

    private void RefreshStatusPanel()
    {
        if (_statusPanelUI == null || _worldController == null || _scriptRuntimeController == null)
        {
            return;
        }

        string runtimeState = _scriptRuntimeController.IsRunning ? "Running" : "Idle";

        _statusPanelUI.SetStatus(
            runtimeState,
            _worldController.CurrentDroneGridPosition,
            _worldController.IsCarryingPackage(),
            _worldController.DeliveredCount
        );
    }

    private void SubscribeToRuntimeEvents()
    {
        if (_scriptRuntimeController == null)
        {
            return;
        }

        _scriptRuntimeController.ExecutionLineChanged += HandleExecutionLineChanged;
        _scriptRuntimeController.RuntimeErrorRaised += HandleRuntimeErrorRaised;
        _scriptRuntimeController.RuntimeStarted += HandleRuntimeStarted;
        _scriptRuntimeController.RuntimeStopped += HandleRuntimeStopped;
        _scriptRuntimeController.RuntimeFinished += HandleRuntimeFinished;
    }

    private void UnsubscribeFromRuntimeEvents()
    {
        if (_scriptRuntimeController == null)
        {
            return;
        }

        _scriptRuntimeController.ExecutionLineChanged -= HandleExecutionLineChanged;
        _scriptRuntimeController.RuntimeErrorRaised -= HandleRuntimeErrorRaised;
        _scriptRuntimeController.RuntimeStarted -= HandleRuntimeStarted;
        _scriptRuntimeController.RuntimeStopped -= HandleRuntimeStopped;
        _scriptRuntimeController.RuntimeFinished -= HandleRuntimeFinished;
    }

    private void HandleExecutionLineChanged(int runtimeLineNumber)
    {
        if (_codeViewerPresenter == null)
        {
            return;
        }

        int lineIndex = runtimeLineNumber - 1;

        if (lineIndex < 0)
        {
            lineIndex = 0;
        }

        _codeViewerPresenter.SetExecutionLineAndReveal(lineIndex);
    }

    private void HandleRuntimeErrorRaised(int runtimeLineNumber, string message)
    {
        if (_codeViewerPresenter == null)
        {
            return;
        }

        _codeViewerPresenter.ClearExecutionLine();

        int lineIndex = runtimeLineNumber - 1;

        if (lineIndex >= 0)
        {
            _codeViewerPresenter.SetErrorLineAndReveal(lineIndex);
        }
        else
        {
            _codeViewerPresenter.ClearErrorLine();
        }
    }

    private void HandleRuntimeStarted()
    {
        if (_codeViewerPresenter != null)
        {
            _codeViewerPresenter.ClearErrorLine();
        }

        RefreshStatusPanel();
    }

    private void HandleRuntimeStopped()
    {
        if (_codeViewerPresenter != null)
        {
            _codeViewerPresenter.ClearExecutionLine();
        }

        RefreshStatusPanel();
    }

    private void HandleRuntimeFinished()
    {
        if (_codeViewerPresenter != null)
        {
            _codeViewerPresenter.ClearExecutionLine();
        }

        RefreshStatusPanel();
    }
}