using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    [SerializeField] private WorldController _worldController;
    [SerializeField] private ScriptRuntimeController _scriptRuntimeController;
    [SerializeField] private DroneController _droneController;
    [SerializeField] private GameLog _gameLog;
    [SerializeField] private StatusPanelUI _statusPanelUI;
    [SerializeField] private ObjectiveController _objectiveController;

    private void Start()
    {
        if (_gameLog != null)
        {
            _gameLog.Clear();
            _gameLog.WriteLine("System ready.");
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

        RefreshStatusPanel();
        _objectiveController.ResetCurrentObjectiveProgress();
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
}