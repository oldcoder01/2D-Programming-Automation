using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class CodeEditorHistoryController : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private int _maxUndoStates = 200;
    [SerializeField] private float _typingMergeWindowSeconds = 1.0f;

    private readonly Stack<CodeEditorHistoryState> _undoStack = new Stack<CodeEditorHistoryState>();
    private readonly Stack<CodeEditorHistoryState> _redoStack = new Stack<CodeEditorHistoryState>();

    private CodeEditorHistoryState _lastKnownState;
    private CodeEditorHistoryState _pendingCompositeBeforeState;

    private bool _isApplyingHistory;
    private bool _isCompositeEditActive;

    private float _lastTypingEventTime = -1000.0f;
    private bool _hasOpenTypingGroup;
    private int _lastTypingCaretPosition;
    private string _lastTypingText = string.Empty;

    public bool CanUndo
    {
        get { return _undoStack.Count > 0; }
    }

    public bool CanRedo
    {
        get { return _redoStack.Count > 0; }
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

        _lastKnownState = CaptureCurrentState();
        ResetTypingGroup();
    }

    private void OnEnable()
    {
        if (_inputField != null)
        {
            _inputField.onValueChanged.AddListener(HandleValueChanged);
        }

        _lastKnownState = CaptureCurrentState();
        ResetTypingGroup();
    }

    private void OnDisable()
    {
        if (_inputField != null)
        {
            _inputField.onValueChanged.RemoveListener(HandleValueChanged);
        }
    }

    public void ClearHistory()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _pendingCompositeBeforeState = null;
        _lastKnownState = CaptureCurrentState();
        ResetTypingGroup();
    }

    public void BeginCompositeEdit()
    {
        if (_isApplyingHistory)
        {
            return;
        }

        if (_isCompositeEditActive)
        {
            return;
        }

        BreakTypingGroup();
        _pendingCompositeBeforeState = CaptureCurrentState();
        _isCompositeEditActive = true;
    }

    public void EndCompositeEdit()
    {
        if (_isApplyingHistory)
        {
            return;
        }

        if (!_isCompositeEditActive)
        {
            return;
        }

        CodeEditorHistoryState afterState = CaptureCurrentState();

        if (_pendingCompositeBeforeState != null && !_pendingCompositeBeforeState.ContentEquals(afterState))
        {
            PushUndoState(_pendingCompositeBeforeState);
            _redoStack.Clear();
            _lastKnownState = afterState;
        }
        else
        {
            _lastKnownState = afterState;
        }

        _pendingCompositeBeforeState = null;
        _isCompositeEditActive = false;
        ResetTypingGroup();
    }

    public void CancelCompositeEdit()
    {
        _pendingCompositeBeforeState = null;
        _isCompositeEditActive = false;
        _lastKnownState = CaptureCurrentState();
        ResetTypingGroup();
    }

    public void Undo()
    {
        if (!CanUndo || _inputField == null)
        {
            return;
        }

        BreakTypingGroup();

        CodeEditorHistoryState currentState = CaptureCurrentState();
        CodeEditorHistoryState targetState = _undoStack.Pop();
        _redoStack.Push(currentState);

        ApplyState(targetState);
    }

    public void Redo()
    {
        if (!CanRedo || _inputField == null)
        {
            return;
        }

        BreakTypingGroup();

        CodeEditorHistoryState currentState = CaptureCurrentState();
        CodeEditorHistoryState targetState = _redoStack.Pop();
        _undoStack.Push(currentState);

        ApplyState(targetState);
    }

    public void BreakTypingGroup()
    {
        ResetTypingGroup();
    }

    private void HandleValueChanged(string newValue)
    {
        if (_isApplyingHistory)
        {
            return;
        }

        if (_isCompositeEditActive)
        {
            return;
        }

        CodeEditorHistoryState currentState = CaptureCurrentState();

        if (_lastKnownState == null)
        {
            _lastKnownState = currentState;
            ResetTypingGroup();
            return;
        }

        if (_lastKnownState.ContentEquals(currentState))
        {
            return;
        }

        bool shouldMergeTyping = IsTypingContinuation(_lastKnownState, currentState);

        if (shouldMergeTyping)
        {
            if (!_hasOpenTypingGroup)
            {
                PushUndoState(_lastKnownState);
                _redoStack.Clear();
                _hasOpenTypingGroup = true;
            }

            _lastKnownState = currentState;
            _lastTypingEventTime = Time.unscaledTime;
            _lastTypingCaretPosition = currentState.StringPosition;
            _lastTypingText = currentState.Text ?? string.Empty;
            return;
        }

        PushUndoState(_lastKnownState);
        _redoStack.Clear();
        _lastKnownState = currentState;
        ResetTypingGroup();
    }

    private bool IsTypingContinuation(CodeEditorHistoryState previousState, CodeEditorHistoryState currentState)
    {
        if (previousState == null || currentState == null)
        {
            return false;
        }

        if (HasSelection(previousState) || HasSelection(currentState))
        {
            return false;
        }

        float now = Time.unscaledTime;
        if (now - _lastTypingEventTime > _typingMergeWindowSeconds && _hasOpenTypingGroup)
        {
            return false;
        }

        string previousText = previousState.Text ?? string.Empty;
        string currentText = currentState.Text ?? string.Empty;

        int lengthDelta = currentText.Length - previousText.Length;

        bool isSingleCharacterInsert = lengthDelta == 1
            && currentState.StringPosition == previousState.StringPosition + 1
            && currentText.StartsWith(previousText);

        bool isSingleCharacterBackspace = lengthDelta == -1
            && currentState.StringPosition == previousState.StringPosition - 1
            && previousText.StartsWith(currentText);

        if (_hasOpenTypingGroup)
        {
            bool caretDidNotJumpUnexpectedly =
                previousState.StringPosition == _lastTypingCaretPosition
                || previousState.StringPosition == _lastTypingCaretPosition - 1
                || previousState.StringPosition == _lastTypingCaretPosition + 1;

            if (!caretDidNotJumpUnexpectedly)
            {
                return false;
            }
        }

        if (isSingleCharacterInsert || isSingleCharacterBackspace)
        {
            return true;
        }

        return false;
    }

    private bool HasSelection(CodeEditorHistoryState state)
    {
        if (state == null)
        {
            return false;
        }

        return state.SelectionStringAnchorPosition != state.SelectionStringFocusPosition;
    }

    private CodeEditorHistoryState CaptureCurrentState()
    {
        return CodeEditorHistoryState.Capture(_inputField);
    }

    private void ApplyState(CodeEditorHistoryState state)
    {
        if (state == null || _inputField == null)
        {
            return;
        }

        _isApplyingHistory = true;
        state.ApplyTo(_inputField);
        _isApplyingHistory = false;

        _lastKnownState = CaptureCurrentState();
        ResetTypingGroup();
    }

    private void PushUndoState(CodeEditorHistoryState state)
    {
        if (state == null)
        {
            return;
        }

        _undoStack.Push(state);
        TrimUndoStackIfNeeded();
    }

    private void TrimUndoStackIfNeeded()
    {
        if (_undoStack.Count <= _maxUndoStates)
        {
            return;
        }

        CodeEditorHistoryState[] items = _undoStack.ToArray();
        _undoStack.Clear();

        for (int i = items.Length - 2; i >= 0; i--)
        {
            _undoStack.Push(items[i]);
        }
    }

    private void ResetTypingGroup()
    {
        _hasOpenTypingGroup = false;
        _lastTypingEventTime = Time.unscaledTime;
        _lastTypingCaretPosition = _inputField != null ? _inputField.stringPosition : 0;
        _lastTypingText = _inputField != null ? (_inputField.text ?? string.Empty) : string.Empty;
    }
}