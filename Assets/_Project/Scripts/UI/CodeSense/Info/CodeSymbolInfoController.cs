using TMPro;
using UnityEngine;

public sealed class CodeSymbolInfoController : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private CodeEditorCodeSenseController _codeSenseController;
    [SerializeField] private CodeSymbolInfoPanel _symbolInfoPanel;

    private int _lastCaretPosition = -1;
    private string _lastText = string.Empty;
    private string _lastSymbolText = string.Empty;

    private void Awake()
    {
        if (_inputField == null)
        {
            _inputField = GetComponentInChildren<TMP_InputField>();
        }

        if (_codeSenseController == null)
        {
            _codeSenseController = GetComponent<CodeEditorCodeSenseController>();
        }
    }

    private void OnEnable()
    {
        RefreshNow();
    }

    private void LateUpdate()
    {
        if (_inputField == null)
        {
            return;
        }

        string currentText = _inputField.text;
        if (currentText == null)
        {
            currentText = string.Empty;
        }

        int currentCaret = _inputField.stringPosition;

        if (currentCaret == _lastCaretPosition && currentText == _lastText)
        {
            return;
        }

        _lastCaretPosition = currentCaret;
        _lastText = currentText;

        RefreshNow();
    }

    [ContextMenu("Refresh Symbol Info")]
    public void RefreshNow()
    {
        if (_codeSenseController == null || _symbolInfoPanel == null)
        {
            return;
        }

        CodeSymbolLookupResult lookupResult = _codeSenseController.GetSymbolAtCaret();

        if (lookupResult == null)
        {
            _lastSymbolText = string.Empty;
            _symbolInfoPanel.Hide();
            return;
        }

        _lastSymbolText = lookupResult.SymbolText;
        _symbolInfoPanel.Show(lookupResult);
    }
}