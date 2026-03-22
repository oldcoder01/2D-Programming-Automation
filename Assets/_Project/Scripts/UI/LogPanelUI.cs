using TMPro;
using UnityEngine;

public sealed class LogPanelUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _logText;
    [SerializeField] private int _maxLines = 30;

    private readonly System.Collections.Generic.List<string> _lines = new System.Collections.Generic.List<string>();

    public void ClearLog()
    {
        _lines.Clear();
        RefreshText();
    }

    public void AppendLine(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _lines.Add(message);

        while (_lines.Count > _maxLines)
        {
            _lines.RemoveAt(0);
        }

        RefreshText();
    }

    private void RefreshText()
    {
        if (_logText == null)
        {
            Debug.LogError("LogPanelUI is missing TextMeshProUGUI reference.");
            return;
        }

        _logText.text = string.Join("\n", _lines);
    }
}