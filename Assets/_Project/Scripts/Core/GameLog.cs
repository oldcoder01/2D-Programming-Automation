using UnityEngine;

public enum GameLogSeverity
{
    Info,
    Success,
    Warning,
    Error
}

public sealed class GameLog : MonoBehaviour
{
    [SerializeField] private LogPanelUI _logPanelUI;

    [Header("Log Colors")]
    [SerializeField] private string _infoColor = "#D8D8D8";
    [SerializeField] private string _successColor = "#7CFF7C";
    [SerializeField] private string _warningColor = "#FFD166";
    [SerializeField] private string _errorColor = "#FF6B6B";

    public void Clear()
    {
        if (_logPanelUI != null)
        {
            _logPanelUI.ClearLog();
        }
    }

    public void WriteLine(string message)
    {
        Write(message, GameLogSeverity.Info);
    }

    public void WriteInfo(string message)
    {
        Write(message, GameLogSeverity.Info);
    }

    public void WriteSuccess(string message)
    {
        Write(message, GameLogSeverity.Success);
    }

    public void WriteWarning(string message)
    {
        Write(message, GameLogSeverity.Warning);
    }

    public void WriteError(string message)
    {
        Write(message, GameLogSeverity.Error);
    }

    public void Write(string message, GameLogSeverity severity)
    {
        string prefixedMessage = AddPrefix(message, severity);
        string coloredMessage = ApplyColor(prefixedMessage, severity);

        switch (severity)
        {
            case GameLogSeverity.Error:
                Debug.LogError(prefixedMessage);
                break;

            case GameLogSeverity.Warning:
                Debug.LogWarning(prefixedMessage);
                break;

            default:
                Debug.Log(prefixedMessage);
                break;
        }

        if (_logPanelUI != null)
        {
            _logPanelUI.AppendLine(coloredMessage);
        }
    }

    private string AddPrefix(string message, GameLogSeverity severity)
    {
        switch (severity)
        {
            case GameLogSeverity.Success:
                return "SUCCESS: " + message;

            case GameLogSeverity.Warning:
                return "WARNING: " + message;

            case GameLogSeverity.Error:
                return "ERROR: " + message;

            default:
                return message;
        }
    }

    private string ApplyColor(string message, GameLogSeverity severity)
    {
        string colorCode = GetColorCode(severity);
        return "<color=" + colorCode + ">" + EscapeRichText(message) + "</color>";
    }

    private string GetColorCode(GameLogSeverity severity)
    {
        switch (severity)
        {
            case GameLogSeverity.Success:
                return _successColor;

            case GameLogSeverity.Warning:
                return _warningColor;

            case GameLogSeverity.Error:
                return _errorColor;

            default:
                return _infoColor;
        }
    }

    private string EscapeRichText(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        string escapedMessage = message;
        escapedMessage = escapedMessage.Replace("&", "&amp;");
        escapedMessage = escapedMessage.Replace("<", "&lt;");
        escapedMessage = escapedMessage.Replace(">", "&gt;");
        return escapedMessage;
    }
}