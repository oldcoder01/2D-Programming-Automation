using TMPro;
using UnityEngine;

public sealed class StatusPanelUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _statusText;

    public void SetStatus(string runtimeState, Vector2Int gridPosition, bool isCarryingPackage, int deliveredCount)
    {
        if (_statusText == null)
        {
            Debug.LogError("StatusPanelUI is missing TextMeshProUGUI reference.");
            return;
        }

        string carryingText = isCarryingPackage ? "Yes" : "No";

        _statusText.text =
            "Runtime: " + runtimeState + "\n" +
            "Position: " + gridPosition.x + ", " + gridPosition.y + "\n" +
            "Carrying: " + carryingText + "\n" +
            "Deliveries: " + deliveredCount;
    }
}