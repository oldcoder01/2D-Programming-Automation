using UnityEngine;

public sealed class ObjectiveFeedbackTester : MonoBehaviour
{
    [SerializeField] private ObjectiveController _objectiveController;

    private void OnEnable()
    {
        if (_objectiveController == null)
        {
            return;
        }

        _objectiveController.OnObjectiveCompleted += HandleObjectiveCompleted;
        _objectiveController.OnAllObjectivesCompleted += HandleAllObjectivesCompleted;
    }

    private void OnDisable()
    {
        if (_objectiveController == null)
        {
            return;
        }

        _objectiveController.OnObjectiveCompleted -= HandleObjectiveCompleted;
        _objectiveController.OnAllObjectivesCompleted -= HandleAllObjectivesCompleted;
    }

    private void HandleObjectiveCompleted(ObjectiveDefinition objective)
    {
        Debug.Log("Feedback hook received objective completion: " + objective.Title);
    }

    private void HandleAllObjectivesCompleted()
    {
        Debug.Log("Feedback hook received all objectives completed.");
    }
}