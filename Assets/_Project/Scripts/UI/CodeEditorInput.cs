using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class CodeEditorInput : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private int _spacesPerTab = 4;

    private bool _preferLineStartOnHome;

    private string IndentString
    {
        get { return new string(' ', _spacesPerTab); }
    }

    private void Reset()
    {
        _inputField = GetComponent<TMP_InputField>();
    }

    private void Awake()
    {
        if (_inputField == null)
        {
            _inputField = GetComponent<TMP_InputField>();
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

        if (currentEvent.keyCode == KeyCode.LeftArrow ||
            currentEvent.keyCode == KeyCode.RightArrow ||
            currentEvent.keyCode == KeyCode.UpArrow ||
            currentEvent.keyCode == KeyCode.DownArrow ||
            currentEvent.keyCode == KeyCode.Backspace ||
            currentEvent.keyCode == KeyCode.Delete ||
            currentEvent.keyCode == KeyCode.Return ||
            currentEvent.keyCode == KeyCode.KeypadEnter ||
            currentEvent.keyCode == KeyCode.Tab ||
            currentEvent.keyCode == KeyCode.Home ||
            currentEvent.keyCode == KeyCode.End ||
            currentEvent.character != '\0')
        {
            _preferLineStartOnHome = true;
        }

        if (currentEvent.keyCode == KeyCode.Tab)
        {
            if (currentEvent.shift)
            {
                UnindentSelectionOrLine();
            }
            else
            {
                IndentSelectionOrLine();
            }

            currentEvent.Use();
            return;
        }

        if (currentEvent.keyCode == KeyCode.Home)
        {
            MoveCaretHome(currentEvent.shift);
            currentEvent.Use();
            return;
        }

        if ((currentEvent.shift && currentEvent.keyCode == KeyCode.Delete) || (currentEvent.control && currentEvent.keyCode == KeyCode.X))
        {
            if (HasSelection())
            {
                CutSelection();
                currentEvent.Use();
            }

            return;
        }

        if ((currentEvent.shift && currentEvent.keyCode == KeyCode.Insert) || (currentEvent.control && currentEvent.keyCode == KeyCode.V))
        {
            PasteSelection();
            currentEvent.Use();
            return;
        }

        if (currentEvent.keyCode == KeyCode.Backspace)
        {
            if (TryHandleSoftTabBackspace())
            {
                currentEvent.Use();
            }

            return;
        }

        if (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter)
        {
            InsertSmartNewLine();
            currentEvent.Use();
            return;
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

    private bool HasSelection()
    {
        return _inputField.selectionStringAnchorPosition != _inputField.selectionStringFocusPosition;
    }

    private void IndentSelectionOrLine()
    {
        if (HasSelection())
        {
            IndentSelectedLines();
        }
        else
        {
            ReplaceSelection(IndentString);
        }

        _inputField.ForceLabelUpdate();
    }

    private void UnindentSelectionOrLine()
    {
        if (HasSelection())
        {
            UnindentSelectedLines();
        }
        else
        {
            UnindentCurrentLine();
        }

        _inputField.ForceLabelUpdate();
    }

    private void CutSelection()
    {
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

        ReplaceSelection(NormalizeTabsToSpaces(clipboardText));
        _inputField.ForceLabelUpdate();
    }

    private void InsertSmartNewLine()
    {
        if (_inputField == null)
        {
            return;
        }

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
        string trimmedBeforeCaret = lineBeforeCaret.TrimEnd();

        bool isWhitespaceOnlyBeforeCaret = string.IsNullOrWhiteSpace(lineBeforeCaret);
        bool isWhitespaceOnlyAfterCaret = string.IsNullOrWhiteSpace(text.Substring(caret, lineEnd - caret));

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

        int i = deleteStart;
        while (i < caret)
        {
            if (text[i] != ' ')
            {
                return false;
            }

            i++;
        }

        _inputField.text = text.Remove(deleteStart, caret - deleteStart);
        SetCaret(deleteStart);
        _inputField.ForceLabelUpdate();
        return true;
    }

    private void MoveCaretHome(bool extendSelection)
    {
        if (_inputField == null)
        {
            return;
        }

        string text = _inputField.text;
        if (text == null)
        {
            text = string.Empty;
        }

        int caret = _inputField.stringPosition;
        if (caret < 0)
        {
            caret = 0;
        }

        if (caret > text.Length)
        {
            caret = text.Length;
        }

        int lineStart = GetLineStartIndex(text, caret);
        int firstNonWhitespace = GetFirstNonWhitespaceIndex(text, lineStart);

        int target;
        if (_preferLineStartOnHome && caret != firstNonWhitespace)
        {
            target = firstNonWhitespace;
            _preferLineStartOnHome = false;
        }
        else
        {
            target = lineStart;
            _preferLineStartOnHome = true;
        }

        if (extendSelection)
        {
            SetCaretWithSelection(target, _inputField.selectionStringAnchorPosition);
        }
        else
        {
            SetCaret(target);
        }

        _inputField.ForceLabelUpdate();
    }

    private void IndentSelectedLines()
    {
        string text = _inputField.text;
        if (text == null)
        {
            text = string.Empty;
        }

        int originalStart = Mathf.Min(_inputField.selectionStringAnchorPosition, _inputField.selectionStringFocusPosition);
        int originalEnd = Mathf.Max(_inputField.selectionStringAnchorPosition, _inputField.selectionStringFocusPosition);

        if (originalStart < 0)
        {
            originalStart = 0;
        }

        if (originalEnd < 0)
        {
            originalEnd = 0;
        }

        if (originalStart > text.Length)
        {
            originalStart = text.Length;
        }

        if (originalEnd > text.Length)
        {
            originalEnd = text.Length;
        }

        int blockStart = GetLineStartIndex(text, originalStart);
        int blockEnd = GetLineEndIndexForSelection(text, originalEnd);

        string blockText = text.Substring(blockStart, blockEnd - blockStart);
        string[] lines = blockText.Split('\n');

        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        int i = 0;
        while (i < lines.Length)
        {
            builder.Append(IndentString);
            builder.Append(lines[i]);

            if (i < lines.Length - 1)
            {
                builder.Append('\n');
            }

            i++;
        }

        string updatedBlock = builder.ToString();
        _inputField.text = text.Substring(0, blockStart) + updatedBlock + text.Substring(blockEnd);

        int newStart = originalStart + IndentString.Length;
        int newEnd = originalEnd + (IndentString.Length * lines.Length);

        SetSelection(newStart, newEnd);
    }

    private void UnindentSelectedLines()
    {
        string text = _inputField.text;
        if (text == null)
        {
            text = string.Empty;
        }

        int originalStart = Mathf.Min(_inputField.selectionStringAnchorPosition, _inputField.selectionStringFocusPosition);
        int originalEnd = Mathf.Max(_inputField.selectionStringAnchorPosition, _inputField.selectionStringFocusPosition);

        if (originalStart < 0)
        {
            originalStart = 0;
        }

        if (originalEnd < 0)
        {
            originalEnd = 0;
        }

        if (originalStart > text.Length)
        {
            originalStart = text.Length;
        }

        if (originalEnd > text.Length)
        {
            originalEnd = text.Length;
        }

        int blockStart = GetLineStartIndex(text, originalStart);
        int blockEnd = GetLineEndIndexForSelection(text, originalEnd);

        string blockText = text.Substring(blockStart, blockEnd - blockStart);
        string[] lines = blockText.Split('\n');

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int removedFromFirstLineBeforeSelection = 0;
        int totalRemoved = 0;

        int i = 0;
        while (i < lines.Length)
        {
            int removable = GetLeadingSpacesToRemove(lines[i]);

            if (i == 0)
            {
                int selectionOffsetInFirstLine = originalStart - blockStart;
                removedFromFirstLineBeforeSelection = Mathf.Min(selectionOffsetInFirstLine, removable);
            }

            totalRemoved += removable;

            if (removable > 0)
            {
                builder.Append(lines[i].Substring(removable));
            }
            else
            {
                builder.Append(lines[i]);
            }

            if (i < lines.Length - 1)
            {
                builder.Append('\n');
            }

            i++;
        }

        string updatedBlock = builder.ToString();
        _inputField.text = text.Substring(0, blockStart) + updatedBlock + text.Substring(blockEnd);

        int newStart = originalStart - removedFromFirstLineBeforeSelection;
        int newEnd = originalEnd - totalRemoved;

        if (newStart < blockStart)
        {
            newStart = blockStart;
        }

        if (newEnd < newStart)
        {
            newEnd = newStart;
        }

        SetSelection(newStart, newEnd);
    }

    private void UnindentCurrentLine()
    {
        string text = _inputField.text;
        if (text == null)
        {
            text = string.Empty;
        }

        int caret = _inputField.stringPosition;
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
        string lineText = text.Substring(lineStart, lineEnd - lineStart);

        int removable = GetLeadingSpacesToRemove(lineText);
        if (removable <= 0)
        {
            return;
        }

        _inputField.text = text.Remove(lineStart, removable);

        int newCaret = caret - removable;
        if (newCaret < lineStart)
        {
            newCaret = lineStart;
        }

        SetCaret(newCaret);
    }

    private int GetLeadingSpacesToRemove(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return 0;
        }

        int count = 0;
        while (count < line.Length && count < _spacesPerTab && line[count] == ' ')
        {
            count++;
        }

        return count;
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

    private int GetLineEndIndexForSelection(string text, int selectionEnd)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        int clampedEnd = Mathf.Clamp(selectionEnd, 0, text.Length);

        if (clampedEnd > 0 && clampedEnd <= text.Length && text[clampedEnd - 1] == '\n')
        {
            return clampedEnd;
        }

        return GetLineEndIndex(text, clampedEnd);
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

    private void SetSelection(int start, int end)
    {
        _inputField.stringPosition = end;
        _inputField.selectionStringAnchorPosition = start;
        _inputField.selectionStringFocusPosition = end;
        _inputField.caretPosition = end;
        _inputField.selectionAnchorPosition = start;
        _inputField.selectionFocusPosition = end;
    }

    private void SetCaretWithSelection(int focusPosition, int anchorPosition)
    {
        _inputField.stringPosition = focusPosition;
        _inputField.selectionStringAnchorPosition = anchorPosition;
        _inputField.selectionStringFocusPosition = focusPosition;
        _inputField.caretPosition = focusPosition;
        _inputField.selectionAnchorPosition = anchorPosition;
        _inputField.selectionFocusPosition = focusPosition;
    }
}