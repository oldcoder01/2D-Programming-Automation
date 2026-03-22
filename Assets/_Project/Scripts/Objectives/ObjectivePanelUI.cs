using TMPro;
using UnityEngine;

public sealed class ObjectivePanelUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _objectiveHeaderText;
    [SerializeField] private TextMeshProUGUI _objectiveBodyText;

    public void SetObjective(string title, string description)
    {
        if (_objectiveHeaderText == null)
        {
            Debug.LogError("ObjectivePanelUI is missing objective header text reference.");
            return;
        }

        if (_objectiveBodyText == null)
        {
            Debug.LogError("ObjectivePanelUI is missing objective body text reference.");
            return;
        }

        _objectiveHeaderText.text = title;
        _objectiveBodyText.text = description;
    }

    public void ClearObjective()
    {
        if (_objectiveHeaderText != null)
        {
            _objectiveHeaderText.text = "Objectives Complete";
        }

        if (_objectiveBodyText != null)
        {
            _objectiveBodyText.text = "No more objectives right now.";
        }
    }
}