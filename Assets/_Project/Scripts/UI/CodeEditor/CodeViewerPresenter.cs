using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CodeViewerPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _codeRichText;
    [SerializeField] private RectTransform _contentRect;
    [SerializeField] private RectTransform _viewportRect;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _caretVisual;
    [SerializeField] private float _caretWidth = 2f;
    [SerializeField] private CodeEditorInputBridge _inputBridge;
    [SerializeField] private RectTransform _selectionVisualRoot;
    [SerializeField] private Image _selectionVisualPrefab;
    [SerializeField] private CodeEditorPointerBridge _pointerBridge;
    [SerializeField] private RectTransform _currentLineHighlight;
    [SerializeField] private float _currentLineHighlightExtraWidth = 32f;
    [SerializeField] private RectTransform _codeViewportRect;

    [Header("Runtime Line Highlights")]
    [SerializeField] private Color _executionLineHighlightColor = new Color(1f, 0.85f, 0.2f, 0.18f);
    [SerializeField] private Color _errorLineHighlightColor = new Color(1f, 0.25f, 0.25f, 0.22f);

    [Header("Layout")]
    [SerializeField] private float _topPadding = 8f;
    [SerializeField] private float _bottomPadding = 8f;
    [SerializeField] private float _leftPadding = 8f;
    [SerializeField] private float _rightPadding = 8f;

    [Header("Colors")]
    [SerializeField] private Color _defaultColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color _keywordColor = new Color(0.35f, 0.75f, 1f, 1f);
    [SerializeField] private Color _stringColor = new Color(0.95f, 0.70f, 0.35f, 1f);
    [SerializeField] private Color _numberColor = new Color(0.65f, 0.90f, 0.45f, 1f);
    [SerializeField] private Color _commentColor = new Color(0.45f, 0.80f, 0.45f, 1f);

    [Header("Caret")]
    [SerializeField] private bool _blinkCaret = true;
    [SerializeField] private float _caretBlinkInterval = 0.5f;
    [SerializeField] private bool _enableHorizontalScroll = true;
    [SerializeField] private float _horizontalPadding = 32f;


    private float _lastCaretBlinkToggleTime;
    private bool _caretBlinkVisible = true;
    private readonly System.Collections.Generic.List<Image> _selectionVisualPool = new System.Collections.Generic.List<Image>();
    private readonly CodeEditorHistory _history = new CodeEditorHistory();

    private static readonly string[] Keywords = new string[]
    {
        "if",
        "else",
        "while",
        "for",
        "break",
        "continue",
        "return",
        "def",
        "import",
        "true",
        "false",
        "null",
        "and",
        "or",
        "not",
        "class",
        "from",
        "as",
        "pass"
    };

    private bool _hasAppliedSourceText;
    private readonly CodeDocument _document = new CodeDocument();
    private readonly CodeEditorState _editorState = new CodeEditorState();
    private RectTransform _executionLineHighlight;
    private RectTransform _errorLineHighlight;
    private int _executionLineIndex = -1;
    private int _errorLineIndex = -1;

    private void Update()
    {
        if (_caretVisual == null || !_blinkCaret)
        {
            return;
        }

        if (Time.unscaledTime - _lastCaretBlinkToggleTime >= _caretBlinkInterval)
        {
            _lastCaretBlinkToggleTime = Time.unscaledTime;
            _caretBlinkVisible = !_caretBlinkVisible;
            _caretVisual.gameObject.SetActive(_caretBlinkVisible);
        }
    }

    public void SetSourceText(string sourceText)
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError("CodeViewerPresenter is missing required references.");
            return;
        }

        _document.SetText(sourceText);
        _history.Clear();
        _editorState.SetCaret(0);
        _hasAppliedSourceText = true;

        ApplyText();
        RefreshLayout(true);
    }

    public void RefreshLayout(bool snapToTop)
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        if (!_hasAppliedSourceText)
        {
            return;
        }

        ApplyRectLayout();
        ResizeForCurrentText();

        Canvas.ForceUpdateCanvases();

        ApplySelectionRootLayout();
        EnsureRuntimeHighlightVisuals();
        RefreshCaretVisual();
        RefreshSelectionVisual();
        RefreshCurrentLineHighlight();
        RefreshExecutionLineHighlight();
        RefreshErrorLineHighlight();

        if (_scrollRect != null && snapToTop)
        {
            _scrollRect.verticalNormalizedPosition = 1f;
        }

        if (snapToTop)
        {
            ResetHorizontalScroll();
        }
        else
        {
            EnsureCaretVisibleHorizontally();
        }
    }

    public void RefreshCaretVisual()
    {
        if (_caretVisual == null)
        {
            return;
        }

        TMP_TextInfo textInfo = _codeRichText.textInfo;
        int caretIndex = _document.ClampIndex(_editorState.CaretIndex);

        if (textInfo == null || textInfo.characterCount == 0)
        {
            _caretVisual.gameObject.SetActive(false);
            return;
        }

        _caretVisual.gameObject.SetActive(true);

        int lineIndex = _document.GetLineIndexFromCharacterIndex(caretIndex);
        int columnIndex = _document.GetColumnFromCharacterIndex(caretIndex);
        string lineText = _document.GetLineText(lineIndex);

        if (columnIndex > lineText.Length)
        {
            columnIndex = lineText.Length;
        }

        Vector2 localPosition = GetCaretLocalPosition(lineIndex, columnIndex);
        float lineHeight = GetCaretLineHeight(lineIndex);

        _caretVisual.anchorMin = new Vector2(0f, 1f);
        _caretVisual.anchorMax = new Vector2(0f, 1f);
        _caretVisual.pivot = new Vector2(0f, 1f);
        _caretVisual.anchoredPosition = localPosition;
        _caretVisual.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _caretWidth);
        _caretVisual.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, lineHeight);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (!_hasAppliedSourceText)
        {
            return;
        }

        RefreshLayout(false);
    }

    private bool HasRequiredReferences()
    {
        return _codeRichText != null
            && _contentRect != null
            && _viewportRect != null;
    }

    private void ApplyText()
    {
        string richText = BuildRichText(_document.Text);

        _codeRichText.text = richText;
    }

    private void ApplyRectLayout()
    {
        RectTransform codeRect = _codeRichText.rectTransform;

        _contentRect.anchorMin = new Vector2(0f, 1f);
        _contentRect.anchorMax = new Vector2(0f, 1f);
        _contentRect.pivot = new Vector2(0f, 1f);

        float viewportWidth = _viewportRect.rect.width;
        float codeX = _leftPadding;
        float codeWidth = viewportWidth - _leftPadding - _rightPadding;

        if (_enableHorizontalScroll)
        {
            float preferredCodeWidth = _codeRichText.GetPreferredValues(_codeRichText.text, 0f, 0f).x + _horizontalPadding;

            if (preferredCodeWidth > codeWidth)
            {
                codeWidth = preferredCodeWidth;
            }
        }

        if (codeWidth < 32f)
        {
            codeWidth = 32f;
        }

        codeRect.anchorMin = new Vector2(0f, 1f);
        codeRect.anchorMax = new Vector2(0f, 1f);
        codeRect.pivot = new Vector2(0f, 1f);
        codeRect.anchoredPosition = new Vector2(codeX, -_topPadding);
        codeRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, codeWidth);
    }

    private void ResizeForCurrentText()
    {
        float viewportWidth = _viewportRect.rect.width;
        float codeX = _leftPadding;
        float codeWidth = viewportWidth - _leftPadding - _rightPadding;

        if (_enableHorizontalScroll)
        {
            float preferredCodeWidth = _codeRichText.GetPreferredValues(_codeRichText.text, 0f, 0f).x + _horizontalPadding;

            if (preferredCodeWidth > codeWidth)
            {
                codeWidth = preferredCodeWidth;
            }
        }

        if (codeWidth < 32f)
        {
            codeWidth = 32f;
        }

        Vector2 codePreferred = _codeRichText.GetPreferredValues(_codeRichText.text, codeWidth, 0f);

        float textHeight = codePreferred.y;
        float contentHeight = _topPadding + textHeight + _bottomPadding;

        RectTransform codeRect = _codeRichText.rectTransform;

        codeRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);

        float contentWidth = codeX + codeRect.rect.width + _rightPadding;

        _contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);
        _contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);

        LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRect);
        _codeRichText.ForceMeshUpdate();

        Canvas.ForceUpdateCanvases();

        if (_scrollRect != null)
        {
            _scrollRect.SetLayoutHorizontal();
            _scrollRect.SetLayoutVertical();
        }
    }

    private static string NormalizeNewlines(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string normalized = value.Replace("\r\n", "\n");
        normalized = normalized.Replace('\r', '\n');
        return normalized;
    }

    private string BuildRichText(string sourceText)
    {
        StringBuilder builder = new StringBuilder(sourceText.Length * 2);

        int index = 0;

        while (index < sourceText.Length)
        {
            char current = sourceText[index];

            if (current == '#')
            {
                int commentEnd = FindLineEnd(sourceText, index);
                string commentText = sourceText.Substring(index, commentEnd - index);
                builder.Append(WrapWithColor(EscapeRichText(commentText), _commentColor));
                index = commentEnd;
                continue;
            }

            if (current == '"' || current == '\'')
            {
                int stringEnd = FindStringEnd(sourceText, index, current);
                string stringText = sourceText.Substring(index, stringEnd - index);
                builder.Append(WrapWithColor(EscapeRichText(stringText), _stringColor));
                index = stringEnd;
                continue;
            }

            if (IsNumberStart(sourceText, index))
            {
                int numberEnd = FindNumberEnd(sourceText, index);
                string numberText = sourceText.Substring(index, numberEnd - index);
                builder.Append(WrapWithColor(EscapeRichText(numberText), _numberColor));
                index = numberEnd;
                continue;
            }

            if (IsIdentifierStart(current))
            {
                int identifierEnd = FindIdentifierEnd(sourceText, index);
                string identifier = sourceText.Substring(index, identifierEnd - index);

                if (IsKeyword(identifier))
                {
                    builder.Append(WrapWithColor(EscapeRichText(identifier), _keywordColor));
                }
                else
                {
                    builder.Append(EscapeRichText(identifier));
                }

                index = identifierEnd;
                continue;
            }

            builder.Append(EscapeRichText(current.ToString()));
            index++;
        }

        return WrapWithColor(builder.ToString(), _defaultColor);
    }

    private static int FindLineEnd(string sourceText, int startIndex)
    {
        int index = startIndex;

        while (index < sourceText.Length && sourceText[index] != '\n')
        {
            index++;
        }

        return index;
    }

    private static int FindStringEnd(string sourceText, int startIndex, char quoteCharacter)
    {
        int index = startIndex + 1;

        while (index < sourceText.Length)
        {
            char current = sourceText[index];

            if (current == '\\')
            {
                index += 2;
                continue;
            }

            if (current == quoteCharacter)
            {
                index++;
                break;
            }

            index++;
        }

        return Mathf.Min(index, sourceText.Length);
    }

    private static bool IsNumberStart(string sourceText, int index)
    {
        if (!char.IsDigit(sourceText[index]))
        {
            return false;
        }

        if (index == 0)
        {
            return true;
        }

        char previous = sourceText[index - 1];
        return !IsIdentifierPart(previous);
    }

    private static int FindNumberEnd(string sourceText, int startIndex)
    {
        int index = startIndex;

        while (index < sourceText.Length)
        {
            char current = sourceText[index];

            if (!char.IsDigit(current) && current != '.')
            {
                break;
            }

            index++;
        }

        return index;
    }

    private static bool IsIdentifierStart(char value)
    {
        return char.IsLetter(value) || value == '_';
    }

    private static bool IsIdentifierPart(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }

    private static int FindIdentifierEnd(string sourceText, int startIndex)
    {
        int index = startIndex;

        while (index < sourceText.Length && IsIdentifierPart(sourceText[index]))
        {
            index++;
        }

        return index;
    }

    private static bool IsKeyword(string value)
    {
        for (int i = 0; i < Keywords.Length; i++)
        {
            if (Keywords[i] == value)
            {
                return true;
            }
        }

        return false;
    }

    private static string EscapeRichText(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return "<noparse>" + value + "</noparse>";
    }

    private static string WrapWithColor(string value, Color color)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGBA(color) + ">" + value + "</color>";
    }

    public void SetCaretIndex(int index)
    {
        int safeIndex = _document.ClampIndex(index);
        _editorState.SetCaret(safeIndex);
        RefreshCaretVisual();
        RefreshSelectionVisual();
        RefreshCurrentLineHighlight();
        EnsureCaretVisible();
        EnsureCaretVisibleHorizontally();
        ResetCaretBlink();
    }

    public int GetCaretIndex()
    {
        return _editorState.CaretIndex;
    }

    public void ReplaceDocumentText(string text)
    {
        _document.SetText(text);
        _editorState.SetCaret(_document.ClampIndex(_editorState.CaretIndex));
        _hasAppliedSourceText = true;

        ApplyText();
        RefreshLayout(false);
    }

    public CodeDocument GetDocument()
    {
        return _document;
    }

    public void RebuildFromDocument(bool snapToTop)
    {
        _editorState.SetCaret(_document.ClampIndex(_editorState.CaretIndex));
        _hasAppliedSourceText = true;

        ApplyText();
        RefreshLayout(snapToTop);
    }

    public int GetLineIndexFromCaret()
    {
        return _document.GetLineIndexFromCharacterIndex(_editorState.CaretIndex);
    }

    public int GetColumnFromCaret()
    {
        return _document.GetColumnFromCharacterIndex(_editorState.CaretIndex);
    }

    public string GetDebugCaretLabel()
    {
        int lineIndex = GetLineIndexFromCaret();
        int columnIndex = GetColumnFromCaret();
        return "Caret " + _editorState.CaretIndex + "  Line " + (lineIndex + 1) + "  Column " + (columnIndex + 1);
    }

    private Vector2 GetCaretLocalPosition(int lineIndex, int columnIndex)
    {
        RectTransform codeRect = _codeRichText.rectTransform;
        TMP_TextInfo textInfo = _codeRichText.textInfo;

        if (_document == null)
        {
            return new Vector2(codeRect.anchoredPosition.x, codeRect.anchoredPosition.y);
        }

        int documentLineCount = _document.LineCount;

        if (documentLineCount <= 0)
        {
            return new Vector2(codeRect.anchoredPosition.x, codeRect.anchoredPosition.y);
        }

        int safeDocumentLineIndex = lineIndex;

        if (safeDocumentLineIndex < 0)
        {
            safeDocumentLineIndex = 0;
        }

        if (safeDocumentLineIndex >= documentLineCount)
        {
            safeDocumentLineIndex = documentLineCount - 1;
        }

        if (textInfo == null || textInfo.lineCount <= 0)
        {
            return new Vector2(codeRect.anchoredPosition.x, codeRect.anchoredPosition.y);
        }

        bool isRealTrailingDocumentLine =
            safeDocumentLineIndex == documentLineCount - 1 &&
            safeDocumentLineIndex >= textInfo.lineCount;

        int safeRenderedLineIndex = safeDocumentLineIndex;

        if (safeRenderedLineIndex < 0)
        {
            safeRenderedLineIndex = 0;
        }

        if (safeRenderedLineIndex >= textInfo.lineCount)
        {
            safeRenderedLineIndex = textInfo.lineCount - 1;
        }

        TMP_LineInfo renderedLineInfo = textInfo.lineInfo[safeRenderedLineIndex];

        if (isRealTrailingDocumentLine)
        {
            float previousLineHeight = GetCaretLineHeight(safeRenderedLineIndex);
            float previousLineY = codeRect.anchoredPosition.y + renderedLineInfo.ascender;
            float trailingLineY = previousLineY - previousLineHeight;

            return new Vector2(codeRect.anchoredPosition.x, trailingLineY);
        }

        float y = codeRect.anchoredPosition.y + renderedLineInfo.ascender;
        int lineLength = _document.GetLineLength(safeDocumentLineIndex);

        if (lineLength <= 0 || columnIndex <= 0)
        {
            return new Vector2(codeRect.anchoredPosition.x, y);
        }

        int safeColumnIndex = columnIndex;

        if (safeColumnIndex > lineLength)
        {
            safeColumnIndex = lineLength;
        }

        int firstCharacterIndex = renderedLineInfo.firstCharacterIndex;
        int lastCharacterIndex = renderedLineInfo.lastCharacterIndex;

        if (firstCharacterIndex < 0 || lastCharacterIndex < firstCharacterIndex)
        {
            return new Vector2(codeRect.anchoredPosition.x, y);
        }

        if (safeColumnIndex >= lineLength)
        {
            TMP_CharacterInfo lastCharacter = textInfo.characterInfo[lastCharacterIndex];
            return new Vector2(codeRect.anchoredPosition.x + lastCharacter.topRight.x, y);
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

        TMP_CharacterInfo characterInfo = textInfo.characterInfo[renderedCharacterIndex];
        float x = codeRect.anchoredPosition.x + characterInfo.topRight.x;

        return new Vector2(x, y);
    }

    private float GetCaretLineHeight(int lineIndex)
    {
        TMP_TextInfo textInfo = _codeRichText.textInfo;

        if (textInfo == null || textInfo.lineCount <= 0)
        {
            return _codeRichText.fontSize;
        }

        int safeLineIndex = lineIndex;

        if (safeLineIndex < 0)
        {
            safeLineIndex = 0;
        }

        if (safeLineIndex >= textInfo.lineCount)
        {
            safeLineIndex = textInfo.lineCount - 1;
        }

        TMP_LineInfo lineInfo = textInfo.lineInfo[safeLineIndex];
        float height = lineInfo.ascender - lineInfo.descender;

        if (height <= 0f)
        {
            height = _codeRichText.fontSize;
        }

        return height;
    }

    public TMP_Text GetCodeText()
    {
        return _codeRichText;
    }

    public void RefreshDebugLabel()
    {
        if (_inputBridge == null)
        {
            return;
        }

        _inputBridge.RefreshDebugCaretExternal();
    }

    public void RefreshSelectionVisual()
    {
        if (_selectionVisualRoot == null || _selectionVisualPrefab == null)
        {
            HideAllSelectionVisuals();
            return;
        }

        if (!_editorState.HasSelection())
        {
            HideAllSelectionVisuals();
            return;
        }

        int selectionStart = _document.ClampIndex(_editorState.GetSelectionStart());
        int selectionEnd = _document.ClampIndex(_editorState.GetSelectionEnd());

        if (selectionEnd <= selectionStart)
        {
            HideAllSelectionVisuals();
            return;
        }

        TMP_TextInfo textInfo = _codeRichText.textInfo;

        if (textInfo == null || textInfo.lineCount <= 0)
        {
            HideAllSelectionVisuals();
            return;
        }

        HideAllSelectionVisuals();

        int startLineIndex = _document.GetLineIndexFromCharacterIndex(selectionStart);
        int endLineIndex = _document.GetLineIndexFromCharacterIndex(selectionEnd);

        int visualIndex = 0;

        for (int lineIndex = startLineIndex; lineIndex <= endLineIndex; lineIndex++)
        {
            int lineStartIndex = _document.GetLineStartIndex(lineIndex);
            int lineLength = _document.GetLineLength(lineIndex);
            int lineEndIndex = lineStartIndex + lineLength;

            int segmentStart = selectionStart > lineStartIndex ? selectionStart : lineStartIndex;
            int segmentEnd = selectionEnd < lineEndIndex ? selectionEnd : lineEndIndex;

            if (segmentEnd < segmentStart)
            {
                continue;
            }

            if (segmentStart == segmentEnd && lineLength > 0)
            {
                continue;
            }

            Rect selectionRect = GetSelectionRectForLineSegment(lineIndex, segmentStart, segmentEnd);
            Image visual = GetOrCreateSelectionVisual(visualIndex);
            visualIndex++;

            RectTransform visualRect = visual.rectTransform;
            visualRect.anchorMin = new Vector2(0f, 1f);
            visualRect.anchorMax = new Vector2(0f, 1f);
            visualRect.pivot = new Vector2(0f, 1f);
            visualRect.anchoredPosition = new Vector2(selectionRect.x, selectionRect.y);
            visualRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, selectionRect.width);
            visualRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, selectionRect.height);
            visual.gameObject.SetActive(true);
        }
    }

    private Image GetOrCreateSelectionVisual(int index)
    {
        while (_selectionVisualPool.Count <= index)
        {
            Image instance = Instantiate(_selectionVisualPrefab, _selectionVisualRoot);
            instance.gameObject.SetActive(false);
            _selectionVisualPool.Add(instance);
        }

        return _selectionVisualPool[index];
    }

    private void HideAllSelectionVisuals()
    {
        for (int i = 0; i < _selectionVisualPool.Count; i++)
        {
            _selectionVisualPool[i].gameObject.SetActive(false);
        }
    }

    private Rect GetSelectionRectForLineSegment(int lineIndex, int segmentStart, int segmentEnd)
    {
        int lineStartIndex = _document.GetLineStartIndex(lineIndex);
        int startColumn = segmentStart - lineStartIndex;
        int endColumn = segmentEnd - lineStartIndex;

        if (startColumn < 0)
        {
            startColumn = 0;
        }

        if (endColumn < 0)
        {
            endColumn = 0;
        }

        Vector2 startPosition = GetCaretLocalPosition(lineIndex, startColumn);
        Vector2 endPosition = GetCaretLocalPosition(lineIndex, endColumn);
        float lineHeight = GetCaretLineHeight(lineIndex);

        float xMin = startPosition.x;
        float xMax = endPosition.x;

        if (xMax < xMin)
        {
            float swap = xMin;
            xMin = xMax;
            xMax = swap;
        }

        float width = xMax - xMin;

        if (width < 2f)
        {
            width = 2f;
        }

        float yTop = startPosition.y;

        return new Rect(xMin, yTop, width, lineHeight);
    }

    public void SetSelection(int anchorIndex, int focusIndex)
    {
        int safeAnchorIndex = _document.ClampIndex(anchorIndex);
        int safeFocusIndex = _document.ClampIndex(focusIndex);
        _editorState.SetSelection(safeAnchorIndex, safeFocusIndex);
        RefreshCaretVisual();
        RefreshSelectionVisual();
        RefreshCurrentLineHighlight();
        EnsureCaretVisible();
        EnsureCaretVisibleHorizontally();
        ResetCaretBlink();
    }

    private void ApplySelectionRootLayout()
    {
        if (_selectionVisualRoot == null)
        {
            return;
        }

        _selectionVisualRoot.anchorMin = new Vector2(0f, 1f);
        _selectionVisualRoot.anchorMax = new Vector2(0f, 1f);
        _selectionVisualRoot.pivot = new Vector2(0f, 1f);
        _selectionVisualRoot.anchoredPosition = Vector2.zero;
        _selectionVisualRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
        _selectionVisualRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
    }

    public Vector2 GetCaretLocalPositionForHitTest(int lineIndex, int columnIndex)
    {
        return GetCaretLocalPosition(lineIndex, columnIndex);
    }

    public bool HasSelection()
    {
        return _editorState.HasSelection();
    }

    public int GetSelectionStart()
    {
        return _editorState.GetSelectionStart();
    }

    public int GetSelectionEnd()
    {
        return _editorState.GetSelectionEnd();
    }

    public string GetSelectedText()
    {
        if (!_editorState.HasSelection())
        {
            return string.Empty;
        }

        int selectionStart = _editorState.GetSelectionStart();
        int selectionEnd = _editorState.GetSelectionEnd();
        int length = selectionEnd - selectionStart;

        if (length <= 0)
        {
            return string.Empty;
        }

        return _document.Text.Substring(selectionStart, length);
    }

    public int GetSelectionAnchorIndex()
    {
        return _editorState.SelectionAnchorIndex;
    }

    public int GetSelectionFocusIndex()
    {
        return _editorState.SelectionFocusIndex;
    }

    public void PushHistoryState()
    {
        _history.PushUndoState(_document.Text, _editorState.CaretIndex, _editorState.SelectionAnchorIndex, _editorState.SelectionFocusIndex);
    }

    public bool Undo()
    {
        string text;
        int caretIndex;
        int selectionAnchorIndex;
        int selectionFocusIndex;

        bool success = _history.TryUndo(_document.Text, _editorState.CaretIndex, _editorState.SelectionAnchorIndex, _editorState.SelectionFocusIndex, out text, out caretIndex, out selectionAnchorIndex, out selectionFocusIndex);

        if (!success)
        {
            return false;
        }

        _document.SetText(text);
        _editorState.SetSelection(selectionAnchorIndex, selectionFocusIndex);
        _editorState.CaretIndex = caretIndex;
        ApplyText();
        RefreshLayout(false);
        return true;
    }

    public bool Redo()
    {
        string text;
        int caretIndex;
        int selectionAnchorIndex;
        int selectionFocusIndex;

        bool success = _history.TryRedo(_document.Text, _editorState.CaretIndex, _editorState.SelectionAnchorIndex, _editorState.SelectionFocusIndex, out text, out caretIndex, out selectionAnchorIndex, out selectionFocusIndex);

        if (!success)
        {
            return false;
        }

        _document.SetText(text);
        _editorState.SetSelection(selectionAnchorIndex, selectionFocusIndex);
        _editorState.CaretIndex = caretIndex;
        ApplyText();
        RefreshLayout(false);
        return true;
    }

    public void EnsureCaretVisible()
    {
        if (_scrollRect == null || _viewportRect == null || _contentRect == null)
        {
            return;
        }

        float caretTop = -_caretVisual.anchoredPosition.y;
        float caretBottom = caretTop + _caretVisual.rect.height;

        float viewportHeight = _viewportRect.rect.height;
        float contentHeight = _contentRect.rect.height;
        float currentScrollY = _contentRect.anchoredPosition.y;

        float visibleTop = currentScrollY;
        float visibleBottom = currentScrollY + viewportHeight;

        float targetScrollY = currentScrollY;

        if (caretTop < visibleTop)
        {
            targetScrollY = caretTop;
        }
        else if (caretBottom > visibleBottom)
        {
            targetScrollY = caretBottom - viewportHeight;
        }

        float maxScrollY = Mathf.Max(0f, contentHeight - viewportHeight);

        if (targetScrollY < 0f)
        {
            targetScrollY = 0f;
        }

        if (targetScrollY > maxScrollY)
        {
            targetScrollY = maxScrollY;
        }

        Vector2 contentPosition = _contentRect.anchoredPosition;
        contentPosition.y = targetScrollY;
        _contentRect.anchoredPosition = contentPosition;
    }

    public bool ShouldMergeTypingHistory(string typingKind, float currentTime, float mergeWindowSeconds)
    {
        return _history.ShouldMergeTyping(typingKind, currentTime, mergeWindowSeconds);
    }

    public void BreakTypingHistoryGroup()
    {
        _history.BreakTypingGroup();
    }

    private void ResetCaretBlink()
    {
        if (_caretVisual == null)
        {
            return;
        }

        _lastCaretBlinkToggleTime = Time.unscaledTime;
        _caretBlinkVisible = true;
        _caretVisual.gameObject.SetActive(true);
    }

    public int GetSelectionStartLineIndex()
    {
        return _document.GetLineIndexContainingSelectionStart(_editorState.GetSelectionStart());
    }

    public int GetSelectionEndLineIndex()
    {
        return _document.GetLineIndexContainingSelectionEnd(_editorState.GetSelectionEnd());
    }

    public void RefreshCaretWithoutScrolling()
    {
        RefreshCaretVisual();
        RefreshSelectionVisual();
    }

    public float GetVerticalScrollPosition()
    {
        if (_contentRect == null)
        {
            return 0f;
        }

        return _contentRect.anchoredPosition.y;
    }

    public void SetVerticalScrollPosition(float scrollY)
    {
        if (_contentRect == null || _viewportRect == null)
        {
            return;
        }

        float viewportHeight = _viewportRect.rect.height;
        float contentHeight = _contentRect.rect.height;
        float maxScrollY = Mathf.Max(0f, contentHeight - viewportHeight);

        float clampedScrollY = scrollY;

        if (clampedScrollY < 0f)
        {
            clampedScrollY = 0f;
        }

        if (clampedScrollY > maxScrollY)
        {
            clampedScrollY = maxScrollY;
        }

        Vector2 anchoredPosition = _contentRect.anchoredPosition;
        anchoredPosition.y = clampedScrollY;
        _contentRect.anchoredPosition = anchoredPosition;
    }

    public int GetCurrentLineIndex()
    {
        return _document.GetLineIndexFromCharacterIndex(_editorState.CaretIndex);
    }

    private void RefreshCurrentLineHighlight()
    {
        if (_currentLineHighlight == null)
        {
            return;
        }

        if (!_hasAppliedSourceText || _document == null)
        {
            _currentLineHighlight.gameObject.SetActive(false);
            return;
        }

        TMP_TextInfo textInfo = _codeRichText.textInfo;

        if (textInfo == null || textInfo.lineCount <= 0 || _document.LineCount <= 0)
        {
            _currentLineHighlight.gameObject.SetActive(false);
            return;
        }

        int caretIndex = _editorState.CaretIndex;
        int lineIndex = _document.GetLineIndexFromCharacterIndex(caretIndex);

        if (lineIndex < 0)
        {
            lineIndex = 0;
        }

        if (lineIndex >= _document.LineCount)
        {
            lineIndex = _document.LineCount - 1;
        }

        Rect highlightArea = GetLineHighlightRect(lineIndex, _currentLineHighlightExtraWidth);

        _currentLineHighlight.gameObject.SetActive(true);
        _currentLineHighlight.anchorMin = new Vector2(0f, 1f);
        _currentLineHighlight.anchorMax = new Vector2(0f, 1f);
        _currentLineHighlight.pivot = new Vector2(0f, 1f);
        _currentLineHighlight.anchoredPosition = new Vector2(highlightArea.x, highlightArea.y);
        _currentLineHighlight.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, highlightArea.width);
        _currentLineHighlight.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, highlightArea.height);
    }

    public void SetExecutionLine(int lineIndex)
    {
        _executionLineIndex = lineIndex;
        RefreshExecutionLineHighlight();
    }

    public void ClearExecutionLine()
    {
        _executionLineIndex = -1;
        RefreshExecutionLineHighlight();
    }

    public void SetErrorLine(int lineIndex)
    {
        _errorLineIndex = lineIndex;
        RefreshErrorLineHighlight();
    }

    public void ClearErrorLine()
    {
        _errorLineIndex = -1;
        RefreshErrorLineHighlight();
    }

    public void ClearRuntimeLineState()
    {
        _executionLineIndex = -1;
        _errorLineIndex = -1;
        RefreshExecutionLineHighlight();
        RefreshErrorLineHighlight();
    }

    public void RefreshExecutionLineHighlight()
    {
        RefreshRuntimeHighlight(_executionLineHighlight, _executionLineIndex);
    }

    public void RefreshErrorLineHighlight()
    {
        RefreshRuntimeHighlight(_errorLineHighlight, _errorLineIndex);
    }

    private void EnsureRuntimeHighlightVisuals()
    {
        if (_contentRect == null)
        {
            return;
        }

        _executionLineHighlight = EnsureRuntimeHighlightVisual(_executionLineHighlight, "ExecutionLineHighlight", _executionLineHighlightColor);
        _errorLineHighlight = EnsureRuntimeHighlightVisual(_errorLineHighlight, "ErrorLineHighlight", _errorLineHighlightColor);

        if (_executionLineHighlight != null)
        {
            _executionLineHighlight.SetAsFirstSibling();
        }

        if (_errorLineHighlight != null)
        {
            _errorLineHighlight.SetAsFirstSibling();
        }
    }

    private RectTransform EnsureRuntimeHighlightVisual(RectTransform existing, string objectName, Color color)
    {
        if (existing == null)
        {
            Transform child = _contentRect.Find(objectName);

            if (child != null)
            {
                existing = child as RectTransform;
            }
        }

        if (existing == null)
        {
            GameObject highlightObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            RectTransform rectTransform = highlightObject.GetComponent<RectTransform>();
            rectTransform.SetParent(_contentRect, false);

            Image image = highlightObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = color;

            existing = rectTransform;
        }

        Image existingImage = existing.GetComponent<Image>();

        if (existingImage != null)
        {
            existingImage.raycastTarget = false;
            existingImage.color = color;
        }

        existing.anchorMin = new Vector2(0f, 1f);
        existing.anchorMax = new Vector2(0f, 1f);
        existing.pivot = new Vector2(0f, 1f);
        existing.gameObject.SetActive(false);
        return existing;
    }

    private void RefreshRuntimeHighlight(RectTransform highlightRect, int lineIndex)
    {
        if (highlightRect == null)
        {
            return;
        }

        if (!_hasAppliedSourceText || lineIndex < 0)
        {
            highlightRect.gameObject.SetActive(false);
            return;
        }

        TMP_TextInfo textInfo = _codeRichText.textInfo;

        if (textInfo == null || textInfo.lineCount <= 0)
        {
            highlightRect.gameObject.SetActive(false);
            return;
        }

        int safeLineIndex = lineIndex;

        if (safeLineIndex >= textInfo.lineCount)
        {
            safeLineIndex = textInfo.lineCount - 1;
        }

        if (safeLineIndex < 0)
        {
            safeLineIndex = 0;
        }

        Rect highlightArea = GetLineHighlightRect(safeLineIndex, _currentLineHighlightExtraWidth);

        highlightRect.gameObject.SetActive(true);
        highlightRect.anchorMin = new Vector2(0f, 1f);
        highlightRect.anchorMax = new Vector2(0f, 1f);
        highlightRect.pivot = new Vector2(0f, 1f);
        highlightRect.anchoredPosition = new Vector2(highlightArea.x, highlightArea.y);
        highlightRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, highlightArea.width);
        highlightRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, highlightArea.height);
    }

    private Rect GetLineHighlightRect(int lineIndex, float extraWidth)
    {
        TMP_TextInfo textInfo = _codeRichText.textInfo;
        RectTransform codeRect = _codeRichText.rectTransform;

        if (_document == null || textInfo == null || textInfo.lineCount <= 0)
        {
            return new Rect(
                codeRect.anchoredPosition.x,
                codeRect.anchoredPosition.y,
                codeRect.rect.width + extraWidth,
                GetDefaultLineHeight()
            );
        }

        int documentLineCount = _document.LineCount;

        if (documentLineCount <= 0)
        {
            return new Rect(
                codeRect.anchoredPosition.x,
                codeRect.anchoredPosition.y,
                codeRect.rect.width + extraWidth,
                GetDefaultLineHeight()
            );
        }

        int safeDocumentLineIndex = lineIndex;

        if (safeDocumentLineIndex < 0)
        {
            safeDocumentLineIndex = 0;
        }

        if (safeDocumentLineIndex >= documentLineCount)
        {
            safeDocumentLineIndex = documentLineCount - 1;
        }

        bool isRealTrailingDocumentLine =
            safeDocumentLineIndex == documentLineCount - 1 &&
            safeDocumentLineIndex >= textInfo.lineCount;

        int safeRenderedLineIndex = safeDocumentLineIndex;

        if (safeRenderedLineIndex < 0)
        {
            safeRenderedLineIndex = 0;
        }

        if (safeRenderedLineIndex >= textInfo.lineCount)
        {
            safeRenderedLineIndex = textInfo.lineCount - 1;
        }

        TMP_LineInfo renderedLineInfo = textInfo.lineInfo[safeRenderedLineIndex];
        float lineHeight = GetCaretLineHeight(safeRenderedLineIndex);
        float x = codeRect.anchoredPosition.x;
        float width = codeRect.rect.width + extraWidth;

        if (isRealTrailingDocumentLine)
        {
            float previousLineY = codeRect.anchoredPosition.y + renderedLineInfo.ascender;
            float trailingLineY = previousLineY - lineHeight;

            return new Rect(x, trailingLineY, width, lineHeight);
        }

        float yTop = codeRect.anchoredPosition.y + renderedLineInfo.ascender;
        return new Rect(x, yTop, width, lineHeight);
    }

    public void EnsureCaretVisibleHorizontally()
    {
        if (_scrollRect == null || _viewportRect == null || _contentRect == null || _caretVisual == null || _codeRichText == null)
        {
            return;
        }

        RectTransform codeRect = _codeRichText.rectTransform;

        float currentScrollX = -_contentRect.anchoredPosition.x;
        float viewportWidth = _viewportRect.rect.width;
        float codeColumnStartX = codeRect.anchoredPosition.x;

        float visibleLeft = currentScrollX + codeColumnStartX;
        float visibleRight = visibleLeft + viewportWidth;

        float caretLeft = _caretVisual.anchoredPosition.x;
        float caretRight = caretLeft + _caretVisual.rect.width;

        float targetScrollX = currentScrollX;

        if (caretLeft < visibleLeft)
        {
            targetScrollX = caretLeft - codeColumnStartX;
        }
        else if (caretRight > visibleRight)
        {
            targetScrollX = caretRight - codeColumnStartX - viewportWidth;
        }

        float maxScrollX = Mathf.Max(0f, _contentRect.rect.width - _viewportRect.rect.width);

        if (targetScrollX < 0f)
        {
            targetScrollX = 0f;
        }

        if (targetScrollX > maxScrollX)
        {
            targetScrollX = maxScrollX;
        }

        Vector2 anchoredPosition = _contentRect.anchoredPosition;
        anchoredPosition.x = -targetScrollX;
        _contentRect.anchoredPosition = anchoredPosition;
    }

    public bool ShouldMergeNonTypingHistory(string actionKind)
    {
        return _history.ShouldMergeNonTypingAction(actionKind);
    }

    public void MarkNonTypingHistory(string actionKind)
    {
        _history.MarkNonTypingAction(actionKind);
    }

    public void ClearNonTypingHistory()
    {
        _history.ClearNonTypingAction();
    }

    public void ResetHorizontalScroll()
    {
        if (_contentRect == null)
        {
            return;
        }

        Vector2 anchoredPosition = _contentRect.anchoredPosition;
        anchoredPosition.x = 0f;
        _contentRect.anchoredPosition = anchoredPosition;
    }

    public void RevealLine(int lineIndex)
    {
        if (_scrollRect == null || _viewportRect == null || _contentRect == null || _codeRichText == null)
        {
            return;
        }

        TMP_TextInfo textInfo = _codeRichText.textInfo;
        if (textInfo == null || textInfo.lineCount <= 0)
        {
            return;
        }

        int safeLineIndex = lineIndex;

        if (safeLineIndex < 0)
        {
            safeLineIndex = 0;
        }

        if (safeLineIndex >= textInfo.lineCount)
        {
            safeLineIndex = textInfo.lineCount - 1;
        }

        TMP_LineInfo lineInfo = textInfo.lineInfo[safeLineIndex];
        RectTransform codeRect = _codeRichText.rectTransform;

        float lineTop = -(codeRect.anchoredPosition.y + lineInfo.ascender);
        float lineBottom = lineTop + GetCaretLineHeight(safeLineIndex);

        float viewportHeight = _viewportRect.rect.height;
        float contentHeight = _contentRect.rect.height;
        float currentScrollY = _contentRect.anchoredPosition.y;

        float visibleTop = currentScrollY;
        float visibleBottom = currentScrollY + viewportHeight;

        float targetScrollY = currentScrollY;

        if (lineTop < visibleTop)
        {
            targetScrollY = lineTop;
        }
        else if (lineBottom > visibleBottom)
        {
            targetScrollY = lineBottom - viewportHeight;
        }

        float maxScrollY = Mathf.Max(0f, contentHeight - viewportHeight);

        if (targetScrollY < 0f)
        {
            targetScrollY = 0f;
        }

        if (targetScrollY > maxScrollY)
        {
            targetScrollY = maxScrollY;
        }

        Vector2 anchoredPosition = _contentRect.anchoredPosition;
        anchoredPosition.y = targetScrollY;
        _contentRect.anchoredPosition = anchoredPosition;
    }

    public void SetExecutionLineAndReveal(int lineIndex)
    {
        SetExecutionLine(lineIndex);
        RevealLine(lineIndex);
    }

    public void SetErrorLineAndReveal(int lineIndex)
    {
        SetErrorLine(lineIndex);
        RevealLine(lineIndex);
    }

    private float GetDefaultLineHeight()
    {
        if (_codeRichText != null && _codeRichText.fontSize > 0f)
        {
            return _codeRichText.fontSize * 1.2f;
        }

        return 24f;
    }

public Vector2 GetCompletionPopupAnchorLocalPosition(RectTransform popupRect)
{
    if (popupRect == null || _document == null || _contentRect == null)
    {
        return Vector2.zero;
    }

    RectTransform parentRect = popupRect.parent as RectTransform;

    if (parentRect == null)
    {
        return Vector2.zero;
    }

    int caretIndex = _editorState.CaretIndex;
    int lineIndex = _document.GetLineIndexFromCharacterIndex(caretIndex);
    int columnIndex = _document.GetColumnFromCharacterIndex(caretIndex);

    Vector2 caretLocalInContent = GetCaretLocalPosition(lineIndex, columnIndex);
    float lineHeight = GetCurrentCaretLineHeight();

    Vector3 worldPoint = _contentRect.TransformPoint(new Vector3(caretLocalInContent.x, caretLocalInContent.y - lineHeight, 0f));
    Vector3 localPointInParent = parentRect.InverseTransformPoint(worldPoint);

    return new Vector2(localPointInParent.x, localPointInParent.y);
}

    public RectTransform GetCodeViewportRect()
    {
        return _codeViewportRect;
    }

    private float GetCurrentCaretLineHeight()
    {
        if (_document == null || _codeRichText == null || _codeRichText.textInfo == null || _codeRichText.textInfo.lineCount <= 0)
        {
            return GetDefaultLineHeight();
        }

        int caretIndex = _editorState.CaretIndex;
        int lineIndex = _document.GetLineIndexFromCharacterIndex(caretIndex);

        if (lineIndex < 0)
        {
            lineIndex = 0;
        }

        return GetCaretLineHeight(lineIndex);
    }

}
