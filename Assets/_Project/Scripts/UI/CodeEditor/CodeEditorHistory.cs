using System.Collections.Generic;

public sealed class CodeEditorHistory
{
    private struct HistoryState
    {
        public string Text;
        public int CaretIndex;
        public int SelectionAnchorIndex;
        public int SelectionFocusIndex;
    }

    private string _lastTypingKind = string.Empty;
    private float _lastTypingTime = -10f;
    private string _lastNonTypingActionKind = string.Empty;

    private readonly Stack<HistoryState> _undoStack = new Stack<HistoryState>();
    private readonly Stack<HistoryState> _redoStack = new Stack<HistoryState>();

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }

    public void PushUndoState(string text, int caretIndex, int selectionAnchorIndex, int selectionFocusIndex)
    {
        HistoryState state = new HistoryState();
        state.Text = text;
        state.CaretIndex = caretIndex;
        state.SelectionAnchorIndex = selectionAnchorIndex;
        state.SelectionFocusIndex = selectionFocusIndex;

        _undoStack.Push(state);
        _redoStack.Clear();
    }

    public bool TryUndo(string currentText, int currentCaretIndex, int currentSelectionAnchorIndex, int currentSelectionFocusIndex, out string text, out int caretIndex, out int selectionAnchorIndex, out int selectionFocusIndex)
    {
        if (_undoStack.Count == 0)
        {
            text = currentText;
            caretIndex = currentCaretIndex;
            selectionAnchorIndex = currentSelectionAnchorIndex;
            selectionFocusIndex = currentSelectionFocusIndex;
            return false;
        }

        HistoryState currentState = new HistoryState();
        currentState.Text = currentText;
        currentState.CaretIndex = currentCaretIndex;
        currentState.SelectionAnchorIndex = currentSelectionAnchorIndex;
        currentState.SelectionFocusIndex = currentSelectionFocusIndex;
        _redoStack.Push(currentState);

        HistoryState restoredState = _undoStack.Pop();
        text = restoredState.Text;
        caretIndex = restoredState.CaretIndex;
        selectionAnchorIndex = restoredState.SelectionAnchorIndex;
        selectionFocusIndex = restoredState.SelectionFocusIndex;
        return true;
    }

    public bool TryRedo(string currentText, int currentCaretIndex, int currentSelectionAnchorIndex, int currentSelectionFocusIndex, out string text, out int caretIndex, out int selectionAnchorIndex, out int selectionFocusIndex)
    {
        if (_redoStack.Count == 0)
        {
            text = currentText;
            caretIndex = currentCaretIndex;
            selectionAnchorIndex = currentSelectionAnchorIndex;
            selectionFocusIndex = currentSelectionFocusIndex;
            return false;
        }

        HistoryState currentState = new HistoryState();
        currentState.Text = currentText;
        currentState.CaretIndex = currentCaretIndex;
        currentState.SelectionAnchorIndex = currentSelectionAnchorIndex;
        currentState.SelectionFocusIndex = currentSelectionFocusIndex;
        _undoStack.Push(currentState);

        HistoryState restoredState = _redoStack.Pop();
        text = restoredState.Text;
        caretIndex = restoredState.CaretIndex;
        selectionAnchorIndex = restoredState.SelectionAnchorIndex;
        selectionFocusIndex = restoredState.SelectionFocusIndex;
        return true;
    }

    public bool ShouldMergeTyping(string typingKind, float currentTime, float mergeWindowSeconds)
    {
        bool canMerge = _lastTypingKind == typingKind && currentTime - _lastTypingTime <= mergeWindowSeconds;
        _lastTypingKind = typingKind;
        _lastTypingTime = currentTime;
        return canMerge;
    }

    public void BreakTypingGroup()
    {
        _lastTypingKind = string.Empty;
        _lastTypingTime = -10f;
    }

    public bool ShouldMergeNonTypingAction(string actionKind)
    {
        return _lastNonTypingActionKind == actionKind;
    }

    public void MarkNonTypingAction(string actionKind)
    {
        _lastNonTypingActionKind = actionKind;
        BreakTypingGroup();
    }

    public void ClearNonTypingAction()
    {
        _lastNonTypingActionKind = string.Empty;
    }
}