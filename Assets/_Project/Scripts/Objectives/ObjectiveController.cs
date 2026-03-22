using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ObjectiveController : MonoBehaviour
{
    [Header("Objective Data")]
    [SerializeField] private List<ObjectiveDefinition> _objectives = new List<ObjectiveDefinition>();

    [Header("Scene References")]
    [SerializeField] private ObjectivePanelUI _objectivePanelUI;
    [SerializeField] private GameLog _gameLog;
    [SerializeField] private WorldController _worldController;

    [Header("Startup")]
    [SerializeField] private int _startingObjectiveIndex;

    public event Action<ObjectiveDefinition> OnObjectiveCompleted;
    public event Action OnAllObjectivesCompleted;

    public int CurrentObjectiveIndex { get; private set; } = -1;
    public bool IsCurrentObjectiveComplete { get; private set; }

    public ObjectiveDefinition CurrentObjective
    {
        get
        {
            if (CurrentObjectiveIndex < 0 || CurrentObjectiveIndex >= _objectives.Count)
            {
                return null;
            }

            return _objectives[CurrentObjectiveIndex];
        }
    }

    private void Start()
    {
        LoadStartingObjective();
    }

    private void Update()
    {
        EvaluateCurrentObjective();
    }

    public void LoadStartingObjective()
    {
        if (_objectives.Count == 0)
        {
            Debug.LogWarning("ObjectiveController has no objectives configured.");
            return;
        }

        int clampedIndex = Mathf.Clamp(_startingObjectiveIndex, 0, _objectives.Count - 1);
        LoadObjective(clampedIndex);
    }

    public void ResetCurrentObjectiveProgress()
    {
        IsCurrentObjectiveComplete = false;

        ObjectiveDefinition objective = CurrentObjective;
        if (objective == null)
        {
            return;
        }

        if (_objectivePanelUI != null)
        {
            _objectivePanelUI.SetObjective(objective.Title, objective.Description);
        }
    }

    private void LoadObjective(int objectiveIndex)
    {
        if (objectiveIndex < 0 || objectiveIndex >= _objectives.Count)
        {
            Debug.LogError("ObjectiveController received invalid objective index: " + objectiveIndex);
            return;
        }

        CurrentObjectiveIndex = objectiveIndex;
        IsCurrentObjectiveComplete = false;

        ObjectiveDefinition objective = _objectives[objectiveIndex];

        if (_objectivePanelUI != null)
        {
            _objectivePanelUI.SetObjective(objective.Title, objective.Description);
        }

        if (_gameLog != null)
        {
            _gameLog.WriteLine("Objective active: " + objective.Title);
        }
    }

    private void EvaluateCurrentObjective()
    {
        if (IsCurrentObjectiveComplete)
        {
            return;
        }

        ObjectiveDefinition objective = CurrentObjective;
        if (objective == null)
        {
            return;
        }

        bool isComplete = CheckObjectiveGoal(objective);
        if (!isComplete)
        {
            return;
        }

        IsCurrentObjectiveComplete = true;

        if (_gameLog != null)
        {
            _gameLog.WriteLine("Objective complete: " + objective.Title);
        }

        OnObjectiveCompleted?.Invoke(objective);

        AdvanceToNextObjective();
    }

    private void AdvanceToNextObjective()
    {
        int nextObjectiveIndex = CurrentObjectiveIndex + 1;

        if (nextObjectiveIndex >= _objectives.Count)
        {
            CurrentObjectiveIndex = -1;
            IsCurrentObjectiveComplete = false;

            if (_objectivePanelUI != null)
            {
                _objectivePanelUI.ClearObjective();
            }

            if (_gameLog != null)
            {
                _gameLog.WriteLine("All objectives complete.");
            }

            OnAllObjectivesCompleted?.Invoke();
            return;
        }

        LoadObjective(nextObjectiveIndex);
    }

    private bool CheckObjectiveGoal(ObjectiveDefinition objective)
    {
        if (_worldController == null)
        {
            return false;
        }

        switch (objective.GoalType)
        {
            case ObjectiveGoalType.None:
                return false;

            case ObjectiveGoalType.ReachPickup:
                return _worldController.IsDroneAtPickup;

            case ObjectiveGoalType.CarryPackage:
                return _worldController.IsCarryingPackage();

            case ObjectiveGoalType.CompleteDeliveryCount:
                return _worldController.DeliveredCount >= objective.RequiredCount;

            default:
                return false;
        }
    }
}