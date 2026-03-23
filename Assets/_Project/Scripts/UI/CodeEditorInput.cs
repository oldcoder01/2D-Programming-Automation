using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class CodeEditorInput : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private CodeCompletionPopupController _completionPopupController;
    [SerializeField] private CodeEditorHistoryController _historyController;
    [SerializeField] private int _spacesPerTab = 4;

    private string IndentString
    {
        get { return new string(' ', _spacesPerTab); }
    }

    private void Reset()
    {
        _inputField = GetComponent<TMP_InputField>();
        _completionPopupController = GetComponent<CodeCompletionPopupController>();
        _historyController = GetComponent<CodeEditorHistoryController>();
    }

    private void Awake()
    {
        if (_inputField == null)
        {
            _inputField = GetComponent<TMP_InputField>();
        }

        if (_completionPopupController == null)
        {
            _completionPopupController = GetComponent<CodeCompletionPopupController>();
        }

        if (_historyController == null)
        {
            _historyController = GetComponent<CodeEditorHistoryController>();
        }

        if (_inputField != null)
        {
            _inputField.onValidateInput = ValidateCharacter;
        }
    }

    private void OnGUI()
    {
        if (!IsInputFieldFocused())
        {
            return;
        }

        Event currentEvent = Event.current;
        if (currentEvent == null)
        {
            return;
        }

        if (currentEvent.type != EventType.KeyDown)
        {
            return;
        }

        if (HandleCompletionPopupKeys(currentEvent))
        {
            return;
        }

        if (HandleUndoRedoKeys(currentEvent))
        {
            RefreshCompletionPopup();
            return;
        }

        if ((currentEvent.shift && currentEvent.keyCode == KeyCode.Delete) || (currentEvent.control && currentEvent.keyCode == KeyCode.X))
        {
            if (HasSelection())
            {
                ExecuteHistoryAction(CutSelection);
                RefreshCompletionPopup();
                currentEvent.Use();
            }

            return;
        }

        if ((currentEvent.shift && currentEvent.keyCode == KeyCode.Insert) || (currentEvent.control && currentEvent.keyCode == KeyCode.V))
        {
            ExecuteHistoryAction(PasteSelection);
            RefreshCompletionPopup();
            currentEvent.Use();
            return;
        }

        if (currentEvent.keyCode == KeyCode.Tab)
        {
            ExecuteHistoryAction(InsertIndent);
            RefreshCompletionPopup();
            currentEvent.Use();
            return;
        }

        if (currentEvent.keyCode == KeyCode.Backspace)
        {
            bool changed = ExecuteHistoryActionWithResult(TryHandleSoftTabBackspace);
            if (changed)
            {
                RefreshCompletionPopup();
                currentEvent.Use();
            }

            return;
        }

        if (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter)
        {
            ExecuteHistoryAction(InsertSmartNewLine);
            RefreshCompletionPopup();
            currentEvent.Use();
            return;
        }

        if (ShouldRefreshPopupAfterTyping(currentEvent))
        {
            QueuePopupRefresh();
        }
    }

    public string NormalizeTabsToSpaces(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.Replace("\t", IndentString);
    }

    private char ValidateCharacter(string text, int charIndex, char addedChar)
    {
        if (addedChar == '\t')
        {
            return '\0';
        }

        if (addedChar == '\n' || addedChar == '\r')
        {
            return '\0';
        }

        return addedChar;
    }

    private bool IsInputFieldFocused()
    {
        if (_inputField == null)
        {
            return false;
        }

        if (!_inputField.isFocused)
        {
            return false;
        }

        GameObject selectedObject = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selectedObject == _inputField.gameObject;
    }

    private void ExecuteHistoryAction(System.Action action)
    {
        if (action == null)
        {
            return;
        }

        if (_historyController == null)
        {
            action();
            return;
        }

        _historyController.BeginCompositeEdit();
        action();
        _historyController.EndCompositeEdit();
    }

    private bool ExecuteHistoryActionWithResult(System.Func<bool> action)
    {
        if (action == null)
        {
            return false;
        }

        if (_historyController == null)
        {
            return action();
        }

        _historyController.BeginCompositeEdit();

        bool changed = action();

        if (changed)
        {
            _historyController.EndCompositeEdit();
        }
        else
        {
            _historyController.CancelCompositeEdit();
        }

        return changed;
    }

    private bool HasSelection()
    {
        return _inputField.selectionStringAnchorPosition != _inputField.selectionStringFocusPosition;
    }

    private bool HandleCompletionPopupKeys(Event currentEvent)
    {
        if (_completionPopupController == null || !_completionPopupController.IsOpen)
        {
            return false;
        }

        if (currentEvent.keyCode == KeyCode.DownArrow)
        {
            _completionPopupController.SelectNext();
            currentEvent.Use();
            return true;
        }

        if (currentEvent.keyCode == KeyCode.UpArrow)
        {
            _completionPopupController.SelectPrevious();
            currentEvent.Use();
            return true;
        }

        if (currentEvent.keyCode == KeyCode.Escape)
        {
            _completionPopupController.HidePopup();
            currentEvent.Use();
            return true;
        }

        if (currentEvent.keyCode == KeyCode.Tab)
        {
            _completionPopupController.AcceptSelected();
            currentEvent.Use();
            return true;
        }

        if (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter)
        {
            _completionPopupController.AcceptSelected();
            currentEvent.Use();
            return true;
        }

        return false;
    }

    private bool HandleUndoRedoKeys(Event currentEvent)
    {
        if (_historyController == null)
        {
            return false;
        }

        bool isUndo = currentEvent.control && !currentEvent.shift && currentEvent.keyCode == KeyCode.Z;
        bool isRedoPrimary = currentEvent.control && currentEvent.keyCode == KeyCode.Y;
        bool isRedoAlternate = currentEvent.control && currentEvent.shift && currentEvent.keyCode == KeyCode.Z;

        if (isUndo)
        {
            _historyController.Undo();
            currentEvent.Use();
            return true;
        }

        if (isRedoPrimary || isRedoAlternate)
        {
            _historyController.Redo();
            currentEvent.Use();
            return true;
        }

        return false;
    }

    private bool ShouldRefreshPopupAfterTyping(Event currentEvent)
    {
        if (_completionPopupController == null)
        {
            return false;
        }

        if (currentEvent.control || currentEvent.alt || currentEvent.command)
        {
            return false;
        }

        if (currentEvent.character == '\0')
        {
            return false;
        }

        if (char.IsLetterOrDigit(currentEvent.character))
        {
            return true;
        }

        if (currentEvent.character == '_')
        {
            return true;
        }

        return false;
    }

    private void QueuePopupRefresh()
    {
        if (_completionPopupController == null)
        {
            return;
        }

        _completionPopupController.RefreshPopup();
    }

    private void RefreshCompletionPopup()
    {
        if (_completionPopupController == null)
        {
            return;
        }

        _completionPopupController.RefreshPopup();
    }

    private void InsertIndent()
    {
        BreakTypingUndoGroup();
        ReplaceSelection(IndentString);
        _inputField.ForceLabelUpdate();
    }

    private void CutSelection()
    {
        BreakTypingUndoGroup();
        string selectedText = GetSelectedText();
        if (string.IsNullOrEmpty(selectedText))
        {
            return;
        }

        GUIUtility.systemCopyBuffer = selectedText;
        DeleteSelection();
        _inputField.ForceLabelUpdate();
    }

    private void PasteSelection()
    {
        string clipboardText = GUIUtility.systemCopyBuffer;
        if (string.IsNullOrEmpty(clipboardText))
        {
            return;
        }

        BreakTypingUndoGroup();
        ReplaceSelection(NormalizeTabsToSpaces(clipboardText));
        _inputField.ForceLabelUpdate();
    }

    private void InsertSmartNewLine()
    {
        if (_inputField == null)
        {
            return;
        }
        BreakTypingUndoGroup();

        string text = _inputField.text;
        if (text == null)
        {
            text = string.Empty;
        }

        int caret = Mathf.Min(_inputField.selectionStringAnchorPosition, _inputField.selectionStringFocusPosition);
        if (!HasSelection())
        {
            caret = _inputField.stringPosition;
        }

        if (caret < 0)
        {
            caret = 0;
        }

        if (caret > text.Length)
        {
            caret = text.Length;
        }

        int lineStart = GetLineStartIndex(text, caret);
        int lineEnd = GetLineEndIndex(text, caret);

        string currentLine = text.Substring(lineStart, lineEnd - lineStart);
        string lineIndentation = GetLeadingIndentation(currentLine);

        string lineBeforeCaret = text.Substring(lineStart, caret - lineStart);
        string lineAfterCaret = text.Substring(caret, lineEnd - caret);
        string trimmedBeforeCaret = lineBeforeCaret.TrimEnd();

        bool isWhitespaceOnlyBeforeCaret = string.IsNullOrWhiteSpace(lineBeforeCaret);
        bool isWhitespaceOnlyAfterCaret = string.IsNullOrWhiteSpace(lineAfterCaret);

        string newIndentation = lineIndentation;

        if (trimmedBeforeCaret.EndsWith(":"))
        {
            newIndentation += IndentString;
        }
        else if (isWhitespaceOnlyBeforeCaret && isWhitespaceOnlyAfterCaret)
        {
            int reducedIndentLength = Mathf.Max(0, lineIndentation.Length - _spacesPerTab);
            newIndentation = new string(' ', reducedIndentLength);
        }

        ReplaceSelection("\n" + newIndentation);
        _inputField.ForceLabelUpdate();
    }

    private bool TryHandleSoftTabBackspace()
    {
        if (_inputField == null)
        {
            return false;
        }

        BreakTypingUndoGroup();

        if (HasSelection())
        {
            return false;
        }

        int caret = _inputField.stringPosition;
        string text = _inputField.text;

        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        if (caret <= 0 || caret > text.Length)
        {
            return false;
        }

        int lineStart = GetLineStartIndex(text, caret);
        int firstNonWhitespace = GetFirstNonWhitespaceIndex(text, lineStart);

        if (caret > firstNonWhitespace)
        {
            return false;
        }

        int indentationColumn = caret - lineStart;
        if (indentationColumn <= 0)
        {
            return false;
        }

        int previousTabStop = ((indentationColumn - 1) / _spacesPerTab) * _spacesPerTab;
        int spacesToDelete = indentationColumn - previousTabStop;

        if (spacesToDelete <= 0)
        {
            return false;
        }

        int deleteStart = caret - spacesToDelete;
        if (deleteStart < lineStart)
        {
            deleteStart = lineStart;
        }

        for (int i = deleteStart; i < caret; i++)
        {
            if (text[i] != ' ')
            {
                return false;
            }
        }

        _inputField.text = text.Remove(deleteStart, caret - deleteStart);
        SetCaret(deleteStart);
        _inputField.ForceLabelUpdate();
        return true;
    }

    private int GetLineStartIndex(string text, int caret)
    {
        int index = Mathf.Clamp(caret, 0, text.Length);

        while (index > 0)
        {
            if (text[index - 1] == '\n')
            {
                break;
            }

            index--;
        }

        return index;
    }

    private int GetLineEndIndex(string text, int caret)
    {
        int index = Mathf.Clamp(caret, 0, text.Length);

        while (index < text.Length)
        {
            if (text[index] == '\n')
            {
                break;
            }

            index++;
        }

        return index;
    }

    private string GetLeadingIndentation(string lineText)
    {
        if (string.IsNullOrEmpty(lineText))
        {
            return string.Empty;
        }

        int index = 0;

        while (index < lineText.Length && lineText[index] == ' ')
        {
            index++;
        }

        return lineText.Substring(0, index);
    }

    private int GetFirstNonWhitespaceIndex(string text, int lineStart)
    {
        int index = lineStart;

        while (index < text.Length)
        {
            char c = text[index];

            if (c == '\n')
            {
                return index;
            }

            if (c != ' ' && c != '\t')
            {
                return index;
            }

            index++;
        }

        return index;
    }

    private string GetSelectedText()
    {
        string text = _inputField.text;
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        int start = Mathf.Min(_inputField.selectionStringAnchorPosition, _inputField.selectionStringFocusPosition);
        int end = Mathf.Max(_inputField.selectionStringAnchorPosition, _inputField.selectionStringFocusPosition);

        if (start < 0 || end > text.Length || start >= end)
        {
            return string.Empty;
        }

        return text.Substring(start, end - start);
    }

    private void DeleteSelection()
    {
        string text = _inputField.text;
        if (text == null)
        {
            text = string.Empty;
        }

        int start = Mathf.Min(_inputField.selectionStringAnchorPosition, _inputField.selectionStringFocusPosition);
        int end = Mathf.Max(_inputField.selectionStringAnchorPosition, _inputField.selectionStringFocusPosition);

        if (start < 0 || end > text.Length || start >= end)
        {
            return;
        }

        _inputField.text = text.Remove(start, end - start);
        SetCaret(start);
    }

    private void ReplaceSelection(string insertedText)
    {
        string text = _inputField.text;
        if (text == null)
        {
            text = string.Empty;
        }

        int start = Mathf.Min(_inputField.selectionStringAnchorPosition, _inputField.selectionStringFocusPosition);
        int end = Mathf.Max(_inputField.selectionStringAnchorPosition, _inputField.selectionStringFocusPosition);

        if (start < 0)
        {
            start = 0;
        }

        if (end < 0)
        {
            end = 0;
        }

        if (start > text.Length)
        {
            start = text.Length;
        }

        if (end > text.Length)
        {
            end = text.Length;
        }

        string before = text.Substring(0, start);
        string after = text.Substring(end);

        _inputField.text = before + insertedText + after;
        SetCaret(start + insertedText.Length);
    }

    private void SetCaret(int position)
    {
        _inputField.stringPosition = position;
        _inputField.selectionStringAnchorPosition = position;
        _inputField.selectionStringFocusPosition = position;
        _inputField.caretPosition = position;
        _inputField.selectionAnchorPosition = position;
        _inputField.selectionFocusPosition = position;
    }

    private void BreakTypingUndoGroup()
    {
        if (_historyController == null)
        {
            return;
        }

        _historyController.BreakTypingGroup();
    }
}