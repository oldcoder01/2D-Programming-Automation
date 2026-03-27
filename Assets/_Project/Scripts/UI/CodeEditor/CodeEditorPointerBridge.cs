using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public sealed class CodeEditorPointerBridge : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private CodeViewerPresenter _viewerPresenter;
    [SerializeField] private RectTransform _codeTextRect;
    [SerializeField] private Camera _uiCamera;
    [SerializeField] private CodeEditorInputBridge _inputBridge;
    [SerializeField] private float _doubleClickThresholdSeconds = 0.3f;
    [SerializeField] private float _tripleClickThresholdSeconds = 0.45f;
    [SerializeField] private float _dragStartThreshold = 4f;

    private bool _hasDragSelectionStarted;
    private Vector2 _pointerDownScreenPosition;
    private bool _isSelecting;
    private int _selectionAnchorIndex;
    private float _lastClickTime = -10f;
    private float _secondLastClickTime = -10f;
    private int _lastClickCaretIndex = -1;
    private bool _wordDragMode;
    private bool _lineDragMode;

    private void Update()
    {
        if (!_isSelecting || _viewerPresenter == null)
        {
            return;
        }

        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            return;
        }

        if (!mouse.leftButton.isPressed)
        {
            _isSelecting = false;
            _hasDragSelectionStarted = false;
            _wordDragMode = false;
            _lineDragMode = false;
            return;
        }

        Vector2 screenPosition = mouse.position.ReadValue();

        if (!_hasDragSelectionStarted)
        {
            float dragDistance = Vector2.Distance(screenPosition, _pointerDownScreenPosition);

            if (dragDistance < _dragStartThreshold)
            {
                return;
            }

            _hasDragSelectionStarted = true;
        }

        int caretIndex = GetCaretIndexFromScreenPosition(screenPosition);

        if (_lineDragMode)
        {
            ExpandLineSelectionToCaret(caretIndex);
            _viewerPresenter.RefreshDebugLabel();
            return;
        }

        if (_wordDragMode)
        {
            ExpandWordSelectionToCaret(caretIndex);
            _viewerPresenter.RefreshDebugLabel();
            return;
        }

        _viewerPresenter.SetSelection(_selectionAnchorIndex, caretIndex);
        _viewerPresenter.RefreshDebugLabel();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_viewerPresenter == null || _codeTextRect == null)
        {
            return;
        }

        if (_inputBridge != null)
        {
            _inputBridge.ResetSelectionPivot();
        }

        int caretIndex = GetCaretIndexFromScreenPosition(eventData.position);
        float now = Time.unscaledTime;

        bool isDoubleClick = now - _lastClickTime <= _doubleClickThresholdSeconds;
        bool isTripleClick = isDoubleClick
            && _lastClickCaretIndex == caretIndex
            && now - _secondLastClickTime <= _tripleClickThresholdSeconds;

        _secondLastClickTime = _lastClickTime;
        _lastClickTime = now;
        _lastClickCaretIndex = caretIndex;

        _selectionAnchorIndex = caretIndex;
        _isSelecting = true;
        _hasDragSelectionStarted = false;
        _pointerDownScreenPosition = eventData.position;
        _wordDragMode = false;
        _lineDragMode = false;

        if (isTripleClick)
        {
            _lineDragMode = true;
            SelectLineAtCaret(caretIndex);
            return;
        }

        if (isDoubleClick)
        {
            _wordDragMode = true;
            SelectWordAtCaret(caretIndex);
            return;
        }

        _viewerPresenter.SetCaretIndex(caretIndex);
        _viewerPresenter.RefreshCaretVisual();
        _viewerPresenter.RefreshDebugLabel();
    }

    private int GetCaretIndexFromCharacterHit(int characterIndex, Vector2 screenPosition)
    {
        TMP_Text textComponent = _viewerPresenter.GetCodeText();
        TMP_CharacterInfo characterInfo = textComponent.textInfo.characterInfo[characterIndex];

        Vector3 bottomLeft = characterInfo.bottomLeft;
        Vector3 topRight = characterInfo.topRight;
        Vector3 worldLeft = textComponent.transform.TransformPoint(bottomLeft);
        Vector3 worldRight = textComponent.transform.TransformPoint(topRight);

        Vector2 screenLeft = RectTransformUtility.WorldToScreenPoint(_uiCamera, worldLeft);
        Vector2 screenRight = RectTransformUtility.WorldToScreenPoint(_uiCamera, worldRight);
        float midpoint = (screenLeft.x + screenRight.x) * 0.5f;

        if (screenPosition.x < midpoint)
        {
            return characterIndex;
        }

        return characterIndex + 1;
    }

    private int GetCaretIndexFromLineHit(int lineIndex, Vector2 screenPosition)
    {
        TMP_Text textComponent = _viewerPresenter.GetCodeText();
        CodeDocument document = _viewerPresenter.GetDocument();
        TMP_TextInfo textInfo = textComponent.textInfo;

        if (lineIndex < 0 || lineIndex >= textInfo.lineCount)
        {
            return _viewerPresenter.GetCaretIndex();
        }

        Vector2 localPoint;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_codeTextRect, screenPosition, _uiCamera, out localPoint))
        {
            return _viewerPresenter.GetCaretIndex();
        }

        int lineStartIndex = document.GetLineStartIndex(lineIndex);
        int lineLength = document.GetLineLength(lineIndex);

        int bestCaretIndex = lineStartIndex;
        float bestDistance = float.MaxValue;

        for (int columnIndex = 0; columnIndex <= lineLength; columnIndex++)
        {
            float caretX = GetCaretXInCodeLocalSpace(lineIndex, columnIndex);
            float distance = Mathf.Abs(localPoint.x - caretX);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestCaretIndex = lineStartIndex + columnIndex;
            }
        }

        return bestCaretIndex;
    }

    private int GetCaretIndexFromScreenPosition(Vector2 screenPosition)
    {
        TMP_Text codeText = _viewerPresenter.GetCodeText();

        if (codeText == null)
        {
            return _viewerPresenter.GetCaretIndex();
        }

        TMP_TextInfo textInfo = codeText.textInfo;
        CodeDocument document = _viewerPresenter.GetDocument();

        if (textInfo == null || textInfo.lineCount <= 0)
        {
            return 0;
        }

        float topLineTop = GetTopLineScreenTop(textInfo);
        float bottomLineBottom = GetBottomLineScreenBottom(textInfo);

        if (screenPosition.y > topLineTop + 24f)
        {
            return 0;
        }

        if (screenPosition.y < bottomLineBottom - 24f)
        {
            return document.Length;
        }

        int nearestLineIndex = GetNearestLineIndexFromScreenY(screenPosition.y);

        if (nearestLineIndex < 0)
        {
            return _viewerPresenter.GetCaretIndex();
        }

        return GetCaretIndexFromLineHit(nearestLineIndex, screenPosition);
    }

    private int GetNearestLineIndexFromScreenY(float screenY)
    {
        TMP_Text codeText = _viewerPresenter.GetCodeText();
        TMP_TextInfo textInfo = codeText.textInfo;

        if (textInfo == null || textInfo.lineCount <= 0)
        {
            return -1;
        }

        Vector2 localPoint;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_codeTextRect, new Vector2(0f, screenY), _uiCamera, out localPoint))
        {
            return -1;
        }

        int bestLineIndex = -1;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < textInfo.lineCount; i++)
        {
            TMP_LineInfo lineInfo = textInfo.lineInfo[i];
            float lineCenter = (lineInfo.ascender + lineInfo.descender) * 0.5f;
            float distance = Mathf.Abs(localPoint.y - lineCenter);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestLineIndex = i;
            }
        }

        return bestLineIndex;
    }

    private float GetTopLineScreenTop(TMP_TextInfo textInfo)
    {
        if (textInfo == null || textInfo.lineCount <= 0)
        {
            return 0f;
        }

        TMP_LineInfo firstLine = textInfo.lineInfo[0];
        Vector3 worldTop = _codeTextRect.TransformPoint(new Vector3(0f, firstLine.ascender, 0f));
        Vector2 screenTop = RectTransformUtility.WorldToScreenPoint(_uiCamera, worldTop);
        return screenTop.y;
    }

    private float GetBottomLineScreenBottom(TMP_TextInfo textInfo)
    {
        if (textInfo == null || textInfo.lineCount <= 0)
        {
            return 0f;
        }

        TMP_LineInfo lastLine = textInfo.lineInfo[textInfo.lineCount - 1];
        Vector3 worldBottom = _codeTextRect.TransformPoint(new Vector3(0f, lastLine.descender, 0f));
        Vector2 screenBottom = RectTransformUtility.WorldToScreenPoint(_uiCamera, worldBottom);
        return screenBottom.y;
    }

    private float GetCaretXInCodeLocalSpace(int lineIndex, int columnIndex)
    {
        TMP_Text textComponent = _viewerPresenter.GetCodeText();
        TMP_TextInfo textInfo = textComponent.textInfo;

        if (textInfo == null || lineIndex < 0 || lineIndex >= textInfo.lineCount)
        {
            return 0f;
        }

        TMP_LineInfo lineInfo = textInfo.lineInfo[lineIndex];
        int lineLength = _viewerPresenter.GetDocument().GetLineLength(lineIndex);

        if (lineLength <= 0 || columnIndex <= 0)
        {
            return lineInfo.lineExtents.min.x;
        }

        int safeColumnIndex = columnIndex;

        if (safeColumnIndex > lineLength)
        {
            safeColumnIndex = lineLength;
        }

        int firstCharacterIndex = lineInfo.firstCharacterIndex;
        int lastCharacterIndex = lineInfo.lastCharacterIndex;

        if (firstCharacterIndex < 0 || lastCharacterIndex < firstCharacterIndex)
        {
            return lineInfo.lineExtents.min.x;
        }

        if (safeColumnIndex >= lineLength)
        {
            return textInfo.characterInfo[lastCharacterIndex].topRight.x;
        }

        int characterIndexInLine = safeColumnIndex - 1;
        int renderedCharacterIndex = firstCharacterIndex + characterIndexInLine;

        if (renderedCharacterIndex < firstCharacterIndex)
        {
            renderedCharacterIndex = firstCharacterIndex;
        }

        if (renderedCharacterIndex > lastCharacterIndex)
        {
            renderedCharacterIndex = lastCharacterIndex;
        }

        return textInfo.characterInfo[renderedCharacterIndex].topRight.x;
    }

    private void SelectWordAtCaret(int caretIndex)
    {
        CodeDocument document = _viewerPresenter.GetDocument();
        string text = document.Text;

        if (string.IsNullOrEmpty(text))
        {
            _viewerPresenter.SetCaretIndex(0);
            _viewerPresenter.RefreshDebugLabel();
            return;
        }

        int safeCaretIndex = document.ClampIndex(caretIndex);
        int wordIndex = safeCaretIndex;

        if (wordIndex >= text.Length)
        {
            wordIndex = text.Length - 1;
        }

        if (wordIndex < 0)
        {
            wordIndex = 0;
        }

        if (!IsWordCharacter(text[wordIndex]) && wordIndex > 0 && IsWordCharacter(text[wordIndex - 1]))
        {
            wordIndex--;
        }

        if (!IsWordCharacter(text[wordIndex]))
        {
            _viewerPresenter.SetCaretIndex(safeCaretIndex);
            _viewerPresenter.RefreshDebugLabel();
            return;
        }

        int startIndex = wordIndex;
        int endIndex = wordIndex + 1;

        while (startIndex > 0 && IsWordCharacter(text[startIndex - 1]))
        {
            startIndex--;
        }

        while (endIndex < text.Length && IsWordCharacter(text[endIndex]))
        {
            endIndex++;
        }

        _viewerPresenter.SetSelection(startIndex, endIndex);
        _selectionAnchorIndex = startIndex;
        _viewerPresenter.RefreshDebugLabel();
    }

    private void SelectLineAtCaret(int caretIndex)
    {
        CodeDocument document = _viewerPresenter.GetDocument();
        int safeCaretIndex = document.ClampIndex(caretIndex);
        int lineIndex = document.GetLineIndexFromCharacterIndex(safeCaretIndex);
        int lineStartIndex = document.GetLineStartIndex(lineIndex);
        int lineEndIndex = document.GetLineEndIndexExclusive(lineIndex);

        _viewerPresenter.SetSelection(lineStartIndex, lineEndIndex);
        _selectionAnchorIndex = lineStartIndex;
        _viewerPresenter.RefreshDebugLabel();
    }

    private static bool IsWordCharacter(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }

    private void ExpandWordSelectionToCaret(int caretIndex)
    {
        CodeDocument document = _viewerPresenter.GetDocument();
        string text = document.Text;

        if (string.IsNullOrEmpty(text))
        {
            _viewerPresenter.SetCaretIndex(0);
            return;
        }

        int safeCaretIndex = document.ClampIndex(caretIndex);
        int anchorIndex = _selectionAnchorIndex;

        int selectionStart = GetWordStartIndex(anchorIndex);
        int selectionEnd = GetWordEndIndex(safeCaretIndex);

        if (safeCaretIndex < anchorIndex)
        {
            selectionStart = GetWordStartIndex(safeCaretIndex);
            selectionEnd = GetWordEndIndex(anchorIndex);
        }

        _viewerPresenter.SetSelection(selectionStart, selectionEnd);
    }

    private void ExpandLineSelectionToCaret(int caretIndex)
    {
        CodeDocument document = _viewerPresenter.GetDocument();
        int safeCaretIndex = document.ClampIndex(caretIndex);

        int anchorLineIndex = document.GetLineIndexFromCharacterIndex(_selectionAnchorIndex);
        int focusLineIndex = document.GetLineIndexFromCharacterIndex(safeCaretIndex);

        int startLineIndex = anchorLineIndex < focusLineIndex ? anchorLineIndex : focusLineIndex;
        int endLineIndex = anchorLineIndex > focusLineIndex ? anchorLineIndex : focusLineIndex;

        int selectionStart = document.GetLineStartIndex(startLineIndex);
        int selectionEnd = document.GetLineEndIndexExclusive(endLineIndex);

        _viewerPresenter.SetSelection(selectionStart, selectionEnd);
    }

    private int GetWordStartIndex(int index)
    {
        CodeDocument document = _viewerPresenter.GetDocument();
        string text = document.Text;

        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        int safeIndex = document.ClampIndex(index);

        if (safeIndex >= text.Length)
        {
            safeIndex = text.Length - 1;
        }

        if (safeIndex < 0)
        {
            return 0;
        }

        if (!IsWordCharacter(text[safeIndex]) && safeIndex > 0 && IsWordCharacter(text[safeIndex - 1]))
        {
            safeIndex--;
        }

        while (safeIndex > 0 && IsWordCharacter(text[safeIndex - 1]))
        {
            safeIndex--;
        }

        return safeIndex;
    }

    private int GetWordEndIndex(int index)
    {
        CodeDocument document = _viewerPresenter.GetDocument();
        string text = document.Text;

        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        int safeIndex = document.ClampIndex(index);

        if (safeIndex > 0 && safeIndex == text.Length)
        {
            safeIndex = text.Length - 1;
        }

        if (safeIndex < text.Length && !IsWordCharacter(text[safeIndex]) && safeIndex > 0 && IsWordCharacter(text[safeIndex - 1]))
        {
            safeIndex--;
        }

        while (safeIndex < text.Length && IsWordCharacter(text[safeIndex]))
        {
            safeIndex++;
        }

        return safeIndex;
    }
}