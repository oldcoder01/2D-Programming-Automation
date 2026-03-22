using TMPro;
using UnityEngine;

public sealed class CodePanelUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _codeInputField;
    [SerializeField] private CodeEditorInput _codeEditorInput;

    public string GetCodeText()
    {
        if (_codeInputField == null)
        {
            Debug.LogError("CodePanelUI is missing TMP_InputField reference.");
            return string.Empty;
        }

        string rawText = _codeInputField.text;

        if (_codeEditorInput != null)
        {
            return _codeEditorInput.NormalizeTabsToSpaces(rawText);
        }

        return rawText;
    }

    public void SetCodeText(string codeText)
    {
        if (_codeInputField == null)
        {
            Debug.LogError("CodePanelUI is missing TMP_InputField reference.");
            return;
        }

        if (_codeEditorInput != null)
        {
            _codeInputField.text = _codeEditorInput.NormalizeTabsToSpaces(codeText);
            return;
        }

        _codeInputField.text = codeText;
    }
}