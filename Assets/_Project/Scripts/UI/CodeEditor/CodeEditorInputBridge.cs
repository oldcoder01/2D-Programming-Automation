using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

public sealed class CodeEditorInputBridge : MonoBehaviour
{
    [SerializeField] private TMP_InputField _hiddenInputField;
    [SerializeField] private CodeViewerPresenter _viewerPresenter;
    [SerializeField] private CodeCompletionPopupUI _completionPopup;
    [SerializeField] private TMP_Text _debugCaretText;
    [SerializeField] private float _keyRepeatDelay = 0.4f;
    [SerializeField] private float _keyRepeatRate = 0.05f;
    [SerializeField] private int _pageMoveLineCount = 12;
    [SerializeField] private float _typingHistoryMergeWindowSeconds = 0.75f;

    private bool _previousSendNavigationEvents;
    private bool _isApplyingInternalChange;
    private bool _suppressHiddenInputNavigationThisFrame;
    private int _selectionPivotIndex = -1;
    private Key _heldRepeatKey;
    private float _nextRepeatTime;
    private bool _restoreScrollNextLateUpdate;
    private float _restoredScrollY;

    private readonly CodeCompletionProvider _completionProvider = new CodeCompletionProvider();
    private readonly List<CodeCompletionItem> _completionItems = new List<CodeCompletionItem>();
    private CodeCompletionContext _completionContext;

    private void Start()
    {
        if (_hiddenInputField == null || _viewerPresenter == null || _completionPopup == null)
        {
            Debug.LogError("CodeEditorInputBridge is missing required references.");
            enabled = false;
            return;
        }

        DisableSelectableNavigation(_hiddenInputField);

        if (EventSystem.current != null)
        {
            _previousSendNavigationEvents = EventSystem.current.sendNavigationEvents;
            EventSystem.current.sendNavigationEvents = false;
        }

        _hiddenInputField.onValueChanged.AddListener(HandleInputValueChanged);
        _hiddenInputField.onSubmit.AddListener(HandleInputSubmitted);

        _completionPopup.SelectionChanged += HandleCompletionSelectionChanged;
        _completionPopup.ItemClicked += HandleCompletionItemClicked;
        _completionPopup.Hide();
        _completionPopup.InitializeFromReferenceText(_viewerPresenter.GetCodeText());

        ActivateHiddenInputField();
        ResetHiddenInputField();
        RefreshDebugCaretText();
    }

    private void OnDestroy()
    {
        if (_hiddenInputField != null)
        {
            _hiddenInputField.onValueChanged.RemoveListener(HandleInputValueChanged);
            _hiddenInputField.onSubmit.RemoveListener(HandleInputSubmitted);
        }

        if (_completionPopup != null)
        {
            _completionPopup.SelectionChanged -= HandleCompletionSelectionChanged;
            _completionPopup.ItemClicked -= HandleCompletionItemClicked;
        }
    }

    private void OnEnable()
    {
        if (EventSystem.current != null)
        {
            _previousSendNavigationEvents = EventSystem.current.sendNavigationEvents;
            EventSystem.current.sendNavigationEvents = false;
        }
    }

    private void OnDisable()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.sendNavigationEvents = _previousSendNavigationEvents;
        }

        HideCompletionPopup();
    }

    private void LateUpdate()
    {
        if (_restoreScrollNextLateUpdate)
        {
            _viewerPresenter.SetVerticalScrollPosition(_restoredScrollY);
            _restoreScrollNextLateUpdate = false;
        }

        if (!_suppressHiddenInputNavigationThisFrame)
        {
            return;
        }

        ResetHiddenInputField();
        _suppressHiddenInputNavigationThisFrame = false;
    }

    private void Update()
    {
        if (_hiddenInputField == null)
        {
            return;
        }

        if (!_hiddenInputField.isFocused)
        {
            ActivateHiddenInputField();
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        bool shiftHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        bool ctrlHeld = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        bool commandHeld = keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed;
        bool actionHeld = ctrlHeld || commandHeld;

        if (keyboard.escapeKey.wasPressedThisFrame && IsCompletionPopupOpen())
        {
            HideCompletionPopup();
            ResetHiddenInputField();
            return;
        }

        if (actionHeld && keyboard.cKey.wasPressedThisFrame)
        {
            HandleCopy();
            return;
        }

        if (actionHeld && keyboard.xKey.wasPressedThisFrame)
        {
            HandleCut();
            return;
        }

        if (actionHeld && keyboard.aKey.wasPressedThisFrame)
        {
            HandleSelectAll();
            return;
        }

        if (actionHeld && keyboard.zKey.wasPressedThisFrame)
        {
            if (shiftHeld)
            {
                HandleRedo();
            }
            else
            {
                HandleUndo();
            }

            return;
        }

        if (actionHeld && keyboard.yKey.wasPressedThisFrame)
        {
            HandleRedo();
            return;
        }

        if (keyboard.pageUpKey.wasPressedThisFrame)
        {
            HideCompletionPopup();

            if (shiftHeld)
            {
                HandleMoveCaretPageUpWithSelection();
            }
            else
            {
                _selectionPivotIndex = -1;
                HandleMoveCaretPageUp();
            }

            return;
        }

        if (keyboard.pageDownKey.wasPressedThisFrame)
        {
            HideCompletionPopup();

            if (shiftHeld)
            {
                HandleMoveCaretPageDownWithSelection();
            }
            else
            {
                _selectionPivotIndex = -1;
                HandleMoveCaretPageDown();
            }

            return;
        }

        if (keyboard.homeKey.wasPressedThisFrame)
        {
            HideCompletionPopup();

            if (actionHeld)
            {
                if (shiftHeld)
                {
                    HandleMoveCaretDocumentStartWithSelection();
                }
                else
                {
                    _selectionPivotIndex = -1;
                    HandleMoveCaretDocumentStart();
                }
            }
            else
            {
                if (shiftHeld)
                {
                    HandleMoveCaretHomeWithSelection();
                }
                else
                {
                    _selectionPivotIndex = -1;
                    HandleMoveCaretHome();
                }
            }

            return;
        }

        if (keyboard.endKey.wasPressedThisFrame)
        {
            HideCompletionPopup();

            if (actionHeld)
            {
                if (shiftHeld)
                {
                    HandleMoveCaretDocumentEndWithSelection();
                }
                else
                {
                    _selectionPivotIndex = -1;
                    HandleMoveCaretDocumentEnd();
                }
            }
            else
            {
                if (shiftHeld)
                {
                    HandleMoveCaretEndWithSelection();
                }
                else
                {
                    _selectionPivotIndex = -1;
                    HandleMoveCaretEnd();
                }
            }

            return;
        }

        if (ShouldProcessRepeat(keyboard.upArrowKey))
        {
            if (IsCompletionPopupOpen() && !shiftHeld)
            {
                _completionPopup.MoveSelection(-1);
                return;
            }

            HideCompletionPopup();

            if (shiftHeld)
            {
                HandleMoveCaretUpWithSelection();
            }
            else
            {
                _selectionPivotIndex = -1;
                HandleMoveCaretUp();
            }

            return;
        }

        if (ShouldProcessRepeat(keyboard.downArrowKey))
        {
            if (IsCompletionPopupOpen() && !shiftHeld)
            {
                _completionPopup.MoveSelection(1);
                return;
            }

            HideCompletionPopup();

            if (shiftHeld)
            {
                HandleMoveCaretDownWithSelection();
            }
            else
            {
                _selectionPivotIndex = -1;
                HandleMoveCaretDown();
            }

            return;
        }

        if (ShouldProcessRepeat(keyboard.leftArrowKey))
        {
            HideCompletionPopup();

            if (actionHeld)
            {
                if (shiftHeld)
                {
                    HandleMoveCaretWordLeftWithSelection();
                }
                else
                {
                    _selectionPivotIndex = -1;
                    HandleMoveCaretWordLeft();
                }
            }
            else
            {
                if (shiftHeld)
                {
                    HandleMoveCaretLeftWithSelection();
                }
                else
                {
                    _selectionPivotIndex = -1;
                    HandleMoveCaretLeft();
                }
            }

            return;
        }

        if (ShouldProcessRepeat(keyboard.rightArrowKey))
        {
            HideCompletionPopup();

            if (actionHeld)
            {
                if (shiftHeld)
                {
                    HandleMoveCaretWordRightWithSelection();
                }
                else
                {
                    _selectionPivotIndex = -1;
                    HandleMoveCaretWordRight();
                }
            }
            else
            {
                if (shiftHeld)
                {
                    HandleMoveCaretRightWithSelection();
                }
                else
                {
                    _selectionPivotIndex = -1;
                    HandleMoveCaretRight();
                }
            }

            return;
        }

        if (!shiftHeld)
        {
            _selectionPivotIndex = -1;
        }

        if (keyboard.backspaceKey.wasPressedThisFrame)
        {
            if (actionHeld)
            {
                HandleDeleteWordLeft();
            }
            else
            {
                HandleBackspace();
            }

            return;
        }

        if (keyboard.deleteKey.wasPressedThisFrame)
        {
            if (actionHeld)
            {
                HandleDeleteWordRight();
            }
            else
            {
                HandleDelete();
            }

            return;
        }

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            if (TryCommitCompletion())
            {
                return;
            }

            HandleSmartEnter();
            return;
        }

        if (keyboard.tabKey.wasPressedThisFrame)
        {
            if (!shiftHeld && TryCommitCompletion())
            {
                return;
            }

            HideCompletionPopup();

            if (_viewerPresenter.HasSelection())
            {
                if (shiftHeld)
                {
                    HandleOutdentSelection();
                }
                else
                {
                    HandleIndentSelection();
                }
            }
            else
            {
                if (shiftHeld)
                {
                    HandleOutdentAtCaret();
                }
                else
                {
                    HandleInsertText("    ");
                }
            }

            return;
        }
    }

    private void HandleInputValueChanged(string value)
    {
        if (_isApplyingInternalChange)
        {
            return;
        }

        if (_suppressHiddenInputNavigationThisFrame)
        {
            ResetHiddenInputField();
            return;
        }

        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        HandleInsertText(value);
    }

    private void HandleInputSubmitted(string value)
    {
        ResetHiddenInputField();
    }

    private void HandleInsertText(string value)
    {
        CodeDocument document = _viewerPresenter.GetDocument();
        PushHistoryForTyping("insert");

        if (_viewerPresenter.HasSelection())
        {
            int selectionStart = _viewerPresenter.GetSelectionStart();
            int selectionEnd = _viewerPresenter.GetSelectionEnd();

            document.ReplaceText(selectionStart, selectionEnd - selectionStart, value);
            _viewerPresenter.SetCaretIndex(selectionStart + value.Length);
            _viewerPresenter.RebuildFromDocument(false);
            RefreshDebugCaretText();
            ResetHiddenInputField();
            RefreshCompletionSuggestions();
            return;
        }

        int caretIndex = _viewerPresenter.GetCaretIndex();

        document.InsertText(caretIndex, value);
        caretIndex += value.Length;

        _viewerPresenter.SetCaretIndex(caretIndex);
        _viewerPresenter.RebuildFromDocument(false);
        RefreshDebugCaretText();
        ResetHiddenInputField();
        RefreshCompletionSuggestions();
    }

    private void HandleBackspace()
    {
        _viewerPresenter.BreakTypingHistoryGroup();
        _viewerPresenter.ClearNonTypingHistory();
        CodeDocument document = _viewerPresenter.GetDocument();
        _viewerPresenter.PushHistoryState();

        if (_viewerPresenter.HasSelection())
        {
            int selectionStart = _viewerPresenter.GetSelectionStart();
            int selectionEnd = _viewerPresenter.GetSelectionEnd();

            document.RemoveText(selectionStart, selectionEnd - selectionStart);
            _viewerPresenter.SetCaretIndex(selectionStart);
            _viewerPresenter.RebuildFromDocument(false);
            RefreshDebugCaretText();
            ResetHiddenInputField();
            RefreshCompletionSuggestions();
            return;
        }

        int caretIndex = _viewerPresenter.GetCaretIndex();

        if (caretIndex <= 0)
        {
            ResetHiddenInputField();
            return;
        }

        int lineIndex = document.GetLineIndexFromCharacterIndex(caretIndex);

        if (document.LineStartsWithOnlyWhitespaceBeforeIndex(lineIndex, caretIndex))
        {
            int lineStartIndex = document.GetLineStartIndex(lineIndex);
            int spacesBeforeCaret = caretIndex - lineStartIndex;
            int removeCount = spacesBeforeCaret % 4;

            if (removeCount == 0)
            {
                removeCount = 4;
            }

            if (removeCount > spacesBeforeCaret)
            {
                removeCount = spacesBeforeCaret;
            }

            if (removeCount > 0)
            {
                document.RemoveText(caretIndex - removeCount, removeCount);
                _viewerPresenter.SetCaretIndex(caretIndex - removeCount);
                _viewerPresenter.RebuildFromDocument(false);
                RefreshDebugCaretText();
                ResetHiddenInputField();
                RefreshCompletionSuggestions();
                return;
            }
        }

        document.RemoveText(caretIndex - 1, 1);
        _viewerPresenter.SetCaretIndex(caretIndex - 1);
        _viewerPresenter.RebuildFromDocument(false);
        RefreshDebugCaretText();
        ResetHiddenInputField();
        RefreshCompletionSuggestions();
    }

    private void HandleDelete()
    {
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        PushHistoryForNonTypingAction("block_edit");

        if (_viewerPresenter.HasSelection())
        {
            int selectionStart = _viewerPresenter.GetSelectionStart();
            int selectionEnd = _viewerPresenter.GetSelectionEnd();

            document.RemoveText(selectionStart, selectionEnd - selectionStart);
            _viewerPresenter.SetCaretIndex(selectionStart);
            _viewerPresenter.RebuildFromDocument(false);
            RefreshDebugCaretText();
            ResetHiddenInputField();
            RefreshCompletionSuggestions();
            return;
        }

        int caretIndex = _viewerPresenter.GetCaretIndex();

        if (caretIndex >= document.Length)
        {
            ResetHiddenInputField();
            return;
        }

        document.RemoveText(caretIndex, 1);
        _viewerPresenter.SetCaretIndex(caretIndex);
        _viewerPresenter.RebuildFromDocument(false);
        RefreshDebugCaretText();
        ResetHiddenInputField();
        RefreshCompletionSuggestions();
    }

    private void ResetHiddenInputField()
    {
        _isApplyingInternalChange = true;
        _hiddenInputField.text = string.Empty;
        _hiddenInputField.caretPosition = 0;
        _hiddenInputField.selectionAnchorPosition = 0;
        _hiddenInputField.selectionFocusPosition = 0;
        _isApplyingInternalChange = false;
    }

    private void ActivateHiddenInputField()
    {
        EventSystem currentEventSystem = EventSystem.current;

        if (currentEventSystem == null)
        {
            return;
        }

        currentEventSystem.SetSelectedGameObject(_hiddenInputField.gameObject);
        _hiddenInputField.ActivateInputField();
    }

    private void RefreshDebugCaretText()
    {
        if (_debugCaretText == null || _viewerPresenter == null)
        {
            return;
        }

        _debugCaretText.text = _viewerPresenter.GetDebugCaretLabel();
    }

    public void RefreshDebugCaretExternal()
    {
        RefreshDebugCaretText();
    }

    public void ResetSelectionPivot()
    {
        _selectionPivotIndex = -1;
    }

    private void SuppressHiddenInputNavigationForFrame()
    {
        _suppressHiddenInputNavigationThisFrame = true;
    }

    private void HandleMoveCaretLeft()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        int caretIndex = _viewerPresenter.GetCaretIndex();

        if (caretIndex <= 0)
        {
            RefreshDebugCaretText();
            ResetHiddenInputField();
            return;
        }

        _viewerPresenter.SetCaretIndex(caretIndex - 1);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleMoveCaretRight()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();

        if (caretIndex >= document.Length)
        {
            RefreshDebugCaretText();
            ResetHiddenInputField();
            return;
        }

        _viewerPresenter.SetCaretIndex(caretIndex + 1);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleMoveCaretUp()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();
        int currentLineIndex = document.GetLineIndexFromCharacterIndex(caretIndex);
        int currentColumnIndex = document.GetColumnFromCharacterIndex(caretIndex);

        if (currentLineIndex <= 0)
        {
            RefreshDebugCaretText();
            ResetHiddenInputField();
            return;
        }

        int targetLineIndex = currentLineIndex - 1;
        int targetCaretIndex = document.GetCharacterIndexFromLineAndColumn(targetLineIndex, currentColumnIndex);

        _viewerPresenter.SetCaretIndex(targetCaretIndex);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleMoveCaretDown()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();
        int currentLineIndex = document.GetLineIndexFromCharacterIndex(caretIndex);
        int currentColumnIndex = document.GetColumnFromCharacterIndex(caretIndex);

        if (currentLineIndex >= document.LineCount - 1)
        {
            float scrollY = _viewerPresenter.GetVerticalScrollPosition();
            _viewerPresenter.RefreshCaretWithoutScrolling();
            RestoreScrollPositionNextLateUpdate(scrollY);
            RefreshDebugCaretText();
            ResetHiddenInputField();
            return;
        }

        int targetLineIndex = currentLineIndex + 1;
        int targetCaretIndex = document.GetCharacterIndexFromLineAndColumn(targetLineIndex, currentColumnIndex);

        _viewerPresenter.SetCaretIndex(targetCaretIndex);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private int GetSelectionPivotIndex()
    {
        if (_selectionPivotIndex >= 0)
        {
            return _selectionPivotIndex;
        }

        if (_viewerPresenter.HasSelection())
        {
            int caretIndex = _viewerPresenter.GetCaretIndex();
            int selectionAnchorIndex = _viewerPresenter.GetSelectionAnchorIndex();
            int selectionFocusIndex = _viewerPresenter.GetSelectionFocusIndex();

            if (caretIndex == selectionFocusIndex)
            {
                _selectionPivotIndex = selectionAnchorIndex;
            }
            else
            {
                _selectionPivotIndex = selectionFocusIndex;
            }

            return _selectionPivotIndex;
        }

        _selectionPivotIndex = _viewerPresenter.GetCaretIndex();
        return _selectionPivotIndex;
    }

    private void ApplySelectionFromPivot(int newCaretIndex)
    {
        int pivotIndex = GetSelectionPivotIndex();
        _viewerPresenter.SetSelection(pivotIndex, newCaretIndex);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleMoveCaretLeftWithSelection()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        int caretIndex = _viewerPresenter.GetCaretIndex();

        if (caretIndex <= 0)
        {
            ApplySelectionFromPivot(0);
            return;
        }

        ApplySelectionFromPivot(caretIndex - 1);
    }

    private void HandleMoveCaretRightWithSelection()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();

        if (caretIndex >= document.Length)
        {
            ApplySelectionFromPivot(document.Length);
            return;
        }

        ApplySelectionFromPivot(caretIndex + 1);
    }

    private void HandleMoveCaretUpWithSelection()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();
        int currentLineIndex = document.GetLineIndexFromCharacterIndex(caretIndex);
        int currentColumnIndex = document.GetColumnFromCharacterIndex(caretIndex);

        if (currentLineIndex <= 0)
        {
            ApplySelectionFromPivot(0);
            return;
        }

        int targetLineIndex = currentLineIndex - 1;
        int targetCaretIndex = document.GetCharacterIndexFromLineAndColumn(targetLineIndex, currentColumnIndex);
        ApplySelectionFromPivot(targetCaretIndex);
    }

    private void HandleMoveCaretDownWithSelection()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();
        int currentLineIndex = document.GetLineIndexFromCharacterIndex(caretIndex);
        int currentColumnIndex = document.GetColumnFromCharacterIndex(caretIndex);

        if (currentLineIndex >= document.LineCount - 1)
        {
            float scrollY = _viewerPresenter.GetVerticalScrollPosition();
            _viewerPresenter.RefreshCaretWithoutScrolling();
            RestoreScrollPositionNextLateUpdate(scrollY);
            RefreshDebugCaretText();
            ResetHiddenInputField();
            return;
        }

        int targetLineIndex = currentLineIndex + 1;
        int targetCaretIndex = document.GetCharacterIndexFromLineAndColumn(targetLineIndex, currentColumnIndex);
        ApplySelectionFromPivot(targetCaretIndex);
    }

    private bool ShouldProcessRepeat(KeyControl keyControl)
    {
        if (keyControl == null)
        {
            return false;
        }

        if (keyControl.wasPressedThisFrame)
        {
            _heldRepeatKey = keyControl.keyCode;
            _nextRepeatTime = Time.unscaledTime + _keyRepeatDelay;
            return true;
        }

        if (!keyControl.isPressed)
        {
            if (_heldRepeatKey == keyControl.keyCode)
            {
                _heldRepeatKey = Key.None;
            }

            return false;
        }

        if (_heldRepeatKey != keyControl.keyCode)
        {
            return false;
        }

        if (Time.unscaledTime >= _nextRepeatTime)
        {
            _nextRepeatTime = Time.unscaledTime + _keyRepeatRate;
            return true;
        }

        return false;
    }

    private void HandleCopy()
    {
        _viewerPresenter.BreakTypingHistoryGroup();
        string selectedText = _viewerPresenter.GetSelectedText();

        if (string.IsNullOrEmpty(selectedText))
        {
            return;
        }

        GUIUtility.systemCopyBuffer = selectedText;
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleCut()
    {
        HideCompletionPopup();
        _viewerPresenter.BreakTypingHistoryGroup();

        if (!_viewerPresenter.HasSelection())
        {
            return;
        }

        string selectedText = _viewerPresenter.GetSelectedText();
        PushHistoryForNonTypingAction("block_edit");

        if (!string.IsNullOrEmpty(selectedText))
        {
            GUIUtility.systemCopyBuffer = selectedText;
        }

        CodeDocument document = _viewerPresenter.GetDocument();
        int selectionStart = _viewerPresenter.GetSelectionStart();
        int selectionEnd = _viewerPresenter.GetSelectionEnd();

        document.RemoveText(selectionStart, selectionEnd - selectionStart);
        _viewerPresenter.SetCaretIndex(selectionStart);
        _viewerPresenter.RebuildFromDocument(false);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleMoveCaretHome()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();
        int lineIndex = document.GetLineIndexFromCharacterIndex(caretIndex);
        int lineStartIndex = document.GetLineStartIndex(lineIndex);
        string lineText = document.GetLineText(lineIndex);

        int firstNonWhitespaceColumn = 0;

        while (firstNonWhitespaceColumn < lineText.Length && char.IsWhiteSpace(lineText[firstNonWhitespaceColumn]))
        {
            firstNonWhitespaceColumn++;
        }

        int firstNonWhitespaceIndex = lineStartIndex + firstNonWhitespaceColumn;
        int targetCaretIndex = caretIndex == firstNonWhitespaceIndex ? lineStartIndex : firstNonWhitespaceIndex;

        _viewerPresenter.SetCaretIndex(targetCaretIndex);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleMoveCaretEnd()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();
        int lineIndex = document.GetLineIndexFromCharacterIndex(caretIndex);
        int targetCaretIndex = document.GetLineEndIndexExclusive(lineIndex);

        _viewerPresenter.SetCaretIndex(targetCaretIndex);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleMoveCaretHomeWithSelection()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();
        int lineIndex = document.GetLineIndexFromCharacterIndex(caretIndex);
        int lineStartIndex = document.GetLineStartIndex(lineIndex);
        string lineText = document.GetLineText(lineIndex);

        int firstNonWhitespaceColumn = 0;

        while (firstNonWhitespaceColumn < lineText.Length && char.IsWhiteSpace(lineText[firstNonWhitespaceColumn]))
        {
            firstNonWhitespaceColumn++;
        }

        int firstNonWhitespaceIndex = lineStartIndex + firstNonWhitespaceColumn;
        int targetCaretIndex = caretIndex == firstNonWhitespaceIndex ? lineStartIndex : firstNonWhitespaceIndex;

        ApplySelectionFromPivot(targetCaretIndex);
    }

    private void HandleMoveCaretEndWithSelection()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();
        int lineIndex = document.GetLineIndexFromCharacterIndex(caretIndex);
        int targetCaretIndex = document.GetLineEndIndexExclusive(lineIndex);

        ApplySelectionFromPivot(targetCaretIndex);
    }

    private void HandleSelectAll()
    {
        HideCompletionPopup();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();

        _selectionPivotIndex = 0;
        _viewerPresenter.SetSelection(0, document.Length);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleMoveCaretWordLeft()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();
        int targetCaretIndex = document.GetPreviousWordBoundary(caretIndex);

        _viewerPresenter.SetCaretIndex(targetCaretIndex);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleMoveCaretWordRight()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();
        int targetCaretIndex = document.GetNextWordBoundary(caretIndex);

        _viewerPresenter.SetCaretIndex(targetCaretIndex);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleMoveCaretWordLeftWithSelection()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();
        int targetCaretIndex = document.GetPreviousWordBoundary(caretIndex);

        ApplySelectionFromPivot(targetCaretIndex);
    }

    private void HandleMoveCaretWordRightWithSelection()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();
        int targetCaretIndex = document.GetNextWordBoundary(caretIndex);

        ApplySelectionFromPivot(targetCaretIndex);
    }

    private void HandleMoveCaretDocumentStart()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        _viewerPresenter.SetCaretIndex(0);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleMoveCaretDocumentEnd()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        _viewerPresenter.SetCaretIndex(document.Length);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleMoveCaretDocumentStartWithSelection()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        ApplySelectionFromPivot(0);
    }

    private void HandleMoveCaretDocumentEndWithSelection()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        ApplySelectionFromPivot(document.Length);
    }

    private void HandleDeleteWordLeft()
    {
        HideCompletionPopup();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        PushHistoryForNonTypingAction("block_edit");

        if (_viewerPresenter.HasSelection())
        {
            HandleBackspace();
            return;
        }

        int caretIndex = _viewerPresenter.GetCaretIndex();

        if (caretIndex <= 0)
        {
            ResetHiddenInputField();
            return;
        }

        int targetIndex = document.GetPreviousWordBoundary(caretIndex);
        int removeLength = caretIndex - targetIndex;

        if (removeLength <= 0)
        {
            ResetHiddenInputField();
            return;
        }

        document.RemoveText(targetIndex, removeLength);
        _viewerPresenter.SetCaretIndex(targetIndex);
        _viewerPresenter.RebuildFromDocument(false);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleDeleteWordRight()
    {
        HideCompletionPopup();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        PushHistoryForNonTypingAction("block_edit");

        if (_viewerPresenter.HasSelection())
        {
            HandleDelete();
            return;
        }

        int caretIndex = _viewerPresenter.GetCaretIndex();

        if (caretIndex >= document.Length)
        {
            ResetHiddenInputField();
            return;
        }

        int targetIndex = document.GetNextWordBoundary(caretIndex);
        int removeLength = targetIndex - caretIndex;

        if (removeLength <= 0)
        {
            ResetHiddenInputField();
            return;
        }

        document.RemoveText(caretIndex, removeLength);
        _viewerPresenter.SetCaretIndex(caretIndex);
        _viewerPresenter.RebuildFromDocument(false);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleMoveCaretPageUp()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();
        int currentLineIndex = document.GetLineIndexFromCharacterIndex(caretIndex);
        int currentColumnIndex = document.GetColumnFromCharacterIndex(caretIndex);

        int targetLineIndex = currentLineIndex - _pageMoveLineCount;

        if (targetLineIndex < 0)
        {
            targetLineIndex = 0;
        }

        int targetCaretIndex = document.GetCharacterIndexFromLineAndColumn(targetLineIndex, currentColumnIndex);
        _viewerPresenter.SetCaretIndex(targetCaretIndex);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleMoveCaretPageDown()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();
        int currentLineIndex = document.GetLineIndexFromCharacterIndex(caretIndex);
        int currentColumnIndex = document.GetColumnFromCharacterIndex(caretIndex);

        int targetLineIndex = currentLineIndex + _pageMoveLineCount;

        if (targetLineIndex >= document.LineCount)
        {
            targetLineIndex = document.LineCount - 1;
        }

        int targetCaretIndex = document.GetCharacterIndexFromLineAndColumn(targetLineIndex, currentColumnIndex);
        _viewerPresenter.SetCaretIndex(targetCaretIndex);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleMoveCaretPageUpWithSelection()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();
        int currentLineIndex = document.GetLineIndexFromCharacterIndex(caretIndex);
        int currentColumnIndex = document.GetColumnFromCharacterIndex(caretIndex);

        int targetLineIndex = currentLineIndex - _pageMoveLineCount;

        if (targetLineIndex < 0)
        {
            targetLineIndex = 0;
        }

        int targetCaretIndex = document.GetCharacterIndexFromLineAndColumn(targetLineIndex, currentColumnIndex);
        ApplySelectionFromPivot(targetCaretIndex);
    }

    private void HandleMoveCaretPageDownWithSelection()
    {
        SuppressHiddenInputNavigationForFrame();
        _viewerPresenter.BreakTypingHistoryGroup();
        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();
        int currentLineIndex = document.GetLineIndexFromCharacterIndex(caretIndex);
        int currentColumnIndex = document.GetColumnFromCharacterIndex(caretIndex);

        int targetLineIndex = currentLineIndex + _pageMoveLineCount;

        if (targetLineIndex >= document.LineCount)
        {
            targetLineIndex = document.LineCount - 1;
        }

        int targetCaretIndex = document.GetCharacterIndexFromLineAndColumn(targetLineIndex, currentColumnIndex);
        ApplySelectionFromPivot(targetCaretIndex);
    }

    private void HandleUndo()
    {
        HideCompletionPopup();
        _viewerPresenter.BreakTypingHistoryGroup();
        bool success = _viewerPresenter.Undo();

        if (!success)
        {
            return;
        }

        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleRedo()
    {
        HideCompletionPopup();
        _viewerPresenter.BreakTypingHistoryGroup();
        bool success = _viewerPresenter.Redo();

        if (!success)
        {
            return;
        }

        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void PushHistoryForTyping(string typingKind)
    {
        bool shouldMerge = _viewerPresenter.ShouldMergeTypingHistory(typingKind, Time.unscaledTime, _typingHistoryMergeWindowSeconds);

        if (!shouldMerge)
        {
            _viewerPresenter.PushHistoryState();
        }
    }

    private void HandleIndentSelection()
    {
        _viewerPresenter.BreakTypingHistoryGroup();
        PushHistoryForNonTypingAction("block_edit");

        CodeDocument document = _viewerPresenter.GetDocument();
        int startLineIndex = _viewerPresenter.GetSelectionStartLineIndex();
        int endLineIndex = _viewerPresenter.GetSelectionEndLineIndex();

        int selectionStart = _viewerPresenter.GetSelectionStart();
        int selectionEnd = _viewerPresenter.GetSelectionEnd();

        int lineCount = endLineIndex - startLineIndex + 1;
        int totalInserted = lineCount * 4;

        for (int lineIndex = endLineIndex; lineIndex >= startLineIndex; lineIndex--)
        {
            int lineStartIndex = document.GetLineStartIndex(lineIndex);
            document.InsertText(lineStartIndex, "    ");
        }

        int newSelectionStart = selectionStart + 4;
        int newSelectionEnd = selectionEnd + totalInserted;

        _viewerPresenter.SetSelection(newSelectionStart, newSelectionEnd);
        _viewerPresenter.RebuildFromDocument(false);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleOutdentSelection()
    {
        _viewerPresenter.BreakTypingHistoryGroup();
        PushHistoryForNonTypingAction("block_edit");

        CodeDocument document = _viewerPresenter.GetDocument();
        int startLineIndex = _viewerPresenter.GetSelectionStartLineIndex();
        int endLineIndex = _viewerPresenter.GetSelectionEndLineIndex();

        int selectionStart = _viewerPresenter.GetSelectionStart();
        int selectionEnd = _viewerPresenter.GetSelectionEnd();

        int removedOnFirstLine = 0;
        int removedTotal = 0;

        for (int lineIndex = endLineIndex; lineIndex >= startLineIndex; lineIndex--)
        {
            string lineText = document.GetLineText(lineIndex);
            int removeCount = GetLeadingIndentRemoveCount(lineText);

            if (removeCount <= 0)
            {
                continue;
            }

            int lineStartIndex = document.GetLineStartIndex(lineIndex);
            document.RemoveText(lineStartIndex, removeCount);
            removedTotal += removeCount;

            if (lineIndex == startLineIndex)
            {
                removedOnFirstLine = removeCount;
            }
        }

        int newSelectionStart = selectionStart - removedOnFirstLine;
        int newSelectionEnd = selectionEnd - removedTotal;

        if (newSelectionStart < 0)
        {
            newSelectionStart = 0;
        }

        if (newSelectionEnd < 0)
        {
            newSelectionEnd = 0;
        }

        if (newSelectionEnd < newSelectionStart)
        {
            newSelectionEnd = newSelectionStart;
        }

        _viewerPresenter.SetSelection(newSelectionStart, newSelectionEnd);
        _viewerPresenter.RebuildFromDocument(false);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private void HandleOutdentAtCaret()
    {
        _viewerPresenter.BreakTypingHistoryGroup();
        PushHistoryForNonTypingAction("block_edit");

        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();
        int lineIndex = document.GetLineIndexFromCharacterIndex(caretIndex);
        int lineStartIndex = document.GetLineStartIndex(lineIndex);
        string lineText = document.GetLineText(lineIndex);

        int removeCount = GetLeadingIndentRemoveCount(lineText);

        if (removeCount <= 0)
        {
            ResetHiddenInputField();
            return;
        }

        document.RemoveText(lineStartIndex, removeCount);

        int newCaretIndex = caretIndex - removeCount;

        if (newCaretIndex < lineStartIndex)
        {
            newCaretIndex = lineStartIndex;
        }

        _viewerPresenter.SetCaretIndex(newCaretIndex);
        _viewerPresenter.RebuildFromDocument(false);
        RefreshDebugCaretText();
        ResetHiddenInputField();
    }

    private int GetLeadingIndentRemoveCount(string lineText)
    {
        if (string.IsNullOrEmpty(lineText))
        {
            return 0;
        }

        int maxSpaces = 4;
        int count = 0;

        while (count < lineText.Length && count < maxSpaces && lineText[count] == ' ')
        {
            count++;
        }

        if (count > 0)
        {
            return count;
        }

        if (lineText[0] == '\t')
        {
            return 1;
        }

        return 0;
    }

    private void DisableSelectableNavigation(Selectable selectable)
    {
        if (selectable == null)
        {
            return;
        }

        Navigation navigation = selectable.navigation;
        navigation.mode = Navigation.Mode.None;
        selectable.navigation = navigation;
    }

    private void RestoreScrollPositionNextLateUpdate(float scrollY)
    {
        _restoredScrollY = scrollY;
        _restoreScrollNextLateUpdate = true;
    }

    private void PushHistoryForNonTypingAction(string actionKind)
    {
        bool shouldMerge = _viewerPresenter.ShouldMergeNonTypingHistory(actionKind);

        if (!shouldMerge)
        {
            _viewerPresenter.PushHistoryState();
        }

        _viewerPresenter.MarkNonTypingHistory(actionKind);
    }

    private void HandleSmartEnter()
    {
        HideCompletionPopup();

        CodeDocument document = _viewerPresenter.GetDocument();
        int caretIndex = _viewerPresenter.GetCaretIndex();
        int lineIndex = document.GetLineIndexFromCharacterIndex(caretIndex);
        int lineStartIndex = document.GetLineStartIndex(lineIndex);
        int lineEndIndex = document.GetLineEndIndexExclusive(lineIndex);
        string lineText = document.GetLineText(lineIndex);
        string leadingWhitespace = document.GetLeadingWhitespace(lineIndex);

        int caretColumnInLine = caretIndex - lineStartIndex;
        int lineLength = lineEndIndex - lineStartIndex;

        if (caretColumnInLine < 0)
        {
            caretColumnInLine = 0;
        }

        if (caretColumnInLine > lineLength)
        {
            caretColumnInLine = lineLength;
        }

        bool lineIsWhitespaceOnly = string.IsNullOrWhiteSpace(lineText);
        bool caretAtEndOfIndentOnlyLine = lineIsWhitespaceOnly && caretColumnInLine >= leadingWhitespace.Length;

        if (caretAtEndOfIndentOnlyLine)
        {
            string nextLineIndent = leadingWhitespace;

            if (leadingWhitespace.Length >= 4 && leadingWhitespace.EndsWith("    "))
            {
                nextLineIndent = leadingWhitespace.Substring(0, leadingWhitespace.Length - 4);
            }
            else if (leadingWhitespace.Length >= 1 && leadingWhitespace.EndsWith("\t"))
            {
                nextLineIndent = leadingWhitespace.Substring(0, leadingWhitespace.Length - 1);
            }
            else
            {
                nextLineIndent = string.Empty;
            }

            HandleInsertText("\n" + nextLineIndent);
            return;
        }

        string indentText = leadingWhitespace;

        if (!string.IsNullOrEmpty(lineText))
        {
            string trimmedLine = lineText.TrimEnd();

            if (trimmedLine.EndsWith(":"))
            {
                indentText += "    ";
            }
        }

        HandleInsertText("\n" + indentText);
    }

    private bool IsCompletionPopupOpen()
    {
        return _completionPopup != null && _completionPopup.IsOpen;
    }

    private void RefreshCompletionSuggestions()
    {
        if (_completionPopup == null || _viewerPresenter == null)
        {
            return;
        }

        if (_viewerPresenter.HasSelection())
        {
            HideCompletionPopup();
            return;
        }

        CodeDocument document = _viewerPresenter.GetDocument();

        if (document == null)
        {
            HideCompletionPopup();
            return;
        }

        string previousSelectedLabel = null;

        if (IsCompletionPopupOpen())
        {
            CodeCompletionItem previousSelectedItem = _completionPopup.GetSelectedItem();

            if (previousSelectedItem != null)
            {
                previousSelectedLabel = previousSelectedItem.Label;
            }
        }

        int caretIndex = _viewerPresenter.GetCaretIndex();
        _completionContext = _completionProvider.BuildContext(document, caretIndex);

        if (_completionContext == null || string.IsNullOrEmpty(_completionContext.Prefix))
        {
            HideCompletionPopup();
            return;
        }

        _completionItems.Clear();
        _completionItems.AddRange(_completionProvider.GetSuggestions(document, caretIndex));

        if (_completionItems.Count <= 0)
        {
            HideCompletionPopup();
            return;
        }

        int selectedIndex = FindCompletionSelectionIndex(_completionItems, previousSelectedLabel);
        Vector2 popupPosition = _viewerPresenter.GetCompletionPopupAnchorLocalPosition(_completionPopup.GetComponent<RectTransform>());
        _completionPopup.Show(_completionItems, selectedIndex, popupPosition, _viewerPresenter.GetCodeText());
    }

    private void HideCompletionPopup()
    {
        if (_completionPopup != null)
        {
            _completionPopup.Hide();
        }

        _completionItems.Clear();
        _completionContext = null;

    }

    private void HandleCompletionSelectionChanged(CodeCompletionItem item)
    {
    }

    private void HandleCompletionItemClicked(CodeCompletionItem item)
    {
        if (item == null)
        {
            return;
        }

        CommitCompletionItem(item);
        ActivateHiddenInputField();
        ResetHiddenInputField();
    }

    private bool TryCommitCompletion()
    {
        if (!IsCompletionPopupOpen())
        {
            return false;
        }

        CodeCompletionItem item = _completionPopup.GetSelectedItem();

        if (item == null)
        {
            HideCompletionPopup();
            return false;
        }

        CommitCompletionItem(item);
        return true;
    }

    private void CommitCompletionItem(CodeCompletionItem item)
    {
        if (item == null)
        {
            return;
        }

        CodeDocument document = _viewerPresenter.GetDocument();

        if (document == null)
        {
            return;
        }

        if (_completionContext == null)
        {
            HideCompletionPopup();
            return;
        }

        PushHistoryForNonTypingAction("completion");

        int replaceStart = _completionContext.ReplaceStartIndex;
        int replaceEnd = _completionContext.ReplaceEndIndex;

        if (replaceStart < 0)
        {
            replaceStart = 0;
        }

        if (replaceEnd < replaceStart)
        {
            replaceEnd = replaceStart;
        }

        int replaceLength = replaceEnd - replaceStart;
        string insertText = BuildCommittedText(item);
        int caretOffset = GetCommittedCaretOffset(item, insertText);

        document.ReplaceText(replaceStart, replaceLength, insertText);
        _viewerPresenter.SetCaretIndex(replaceStart + caretOffset);
        _viewerPresenter.RebuildFromDocument(false);
        RefreshDebugCaretText();
        ResetHiddenInputField();
        HideCompletionPopup();
    }

    private string BuildCommittedText(CodeCompletionItem item)
    {
        if (item == null)
        {
            return string.Empty;
        }

        if (item.Kind == CodeSymbolKind.Keyword)
        {
            if (item.Label == "def")
            {
                return "def ";
            }

            return item.InsertText;
        }

        return item.InsertText;
    }

    private int GetCommittedCaretOffset(CodeCompletionItem item, string insertedText)
    {
        if (string.IsNullOrEmpty(insertedText))
        {
            return 0;
        }

        return insertedText.Length;
    }

    private int FindCompletionSelectionIndex(List<CodeCompletionItem> items, string selectedLabel)
    {
        if (items == null || items.Count <= 0 || string.IsNullOrEmpty(selectedLabel))
        {
            return 0;
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && items[i].Label == selectedLabel)
            {
                return i;
            }
        }

        return 0;
    }
}