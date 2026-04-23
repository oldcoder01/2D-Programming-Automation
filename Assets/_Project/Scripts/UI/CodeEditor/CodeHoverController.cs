using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CodeHoverController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CodeViewerPresenter _viewerPresenter;
    [SerializeField] private CodeHoverPanelUI _hoverPanel;
    [SerializeField] private Camera _uiCamera;

    [Header("Behavior")]
    [SerializeField] private float _hoverDelaySeconds = 0.2f;
    [SerializeField] private float _pointerMoveThreshold = 2f;
    [SerializeField] private Vector2 _panelOffset = new Vector2(16f, -16f);
    [SerializeField] private float _horizontalHitPadding = 1.5f;
    [SerializeField] private float _verticalHitPadding = 2f;

    private readonly CodeSymbolResolver _symbolResolver = new CodeSymbolResolver();

    private Vector2 _lastMouseScreenPosition;
    private float _nextHoverTime;
    private bool _hasMousePosition;

    private string _currentIdentifier = string.Empty;
    private int _currentStartIndex = -1;
    private int _currentEndIndex = -1;

    private void Start()
    {
        if (_viewerPresenter == null || _hoverPanel == null)
        {
            Debug.LogError("CodeHoverController is missing required references.");
            enabled = false;
            return;
        }

        _hoverPanel.Hide();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            HideHover();
            return;
        }

        Vector2 screenPosition = mouse.position.ReadValue();

        if (!_hasMousePosition)
        {
            _lastMouseScreenPosition = screenPosition;
            _nextHoverTime = Time.unscaledTime + _hoverDelaySeconds;
            _hasMousePosition = true;
            return;
        }

        if (Vector2.Distance(screenPosition, _lastMouseScreenPosition) > _pointerMoveThreshold)
        {
            _lastMouseScreenPosition = screenPosition;
            _nextHoverTime = Time.unscaledTime + _hoverDelaySeconds;
            HideHover();
            return;
        }

        if (mouse.leftButton.isPressed)
        {
            HideHover();
            return;
        }

        if (_viewerPresenter.HasSelection())
        {
            HideHover();
            return;
        }

        RectTransform viewportRect = _viewerPresenter.GetCodeViewportRect();

        if (viewportRect != null && !RectTransformUtility.RectangleContainsScreenPoint(viewportRect, screenPosition, _uiCamera))
        {
            HideHover();
            return;
        }

        if (Time.unscaledTime < _nextHoverTime)
        {
            return;
        }

        RefreshHover(screenPosition);
    }

    private void RefreshHover(Vector2 screenPosition)
    {
        CodeDocument document = _viewerPresenter.GetDocument();

        if (document == null || document.Length <= 0)
        {
            HideHover();
            return;
        }

        int caretIndex = GetCaretIndexFromScreenPosition(screenPosition);
        int startIndex;
        int endIndex;
        string identifier;

        if (!_symbolResolver.TryGetIdentifierAtIndex(document, caretIndex, out startIndex, out endIndex, out identifier))
        {
            HideHover();
            return;
        }

        if (!IsPointerOverIdentifierBody(screenPosition, startIndex, endIndex))
        {
            HideHover();
            return;
        }

        CodeSymbolInfo symbolInfo = _symbolResolver.ResolveByName(document, identifier);

        if (symbolInfo == null)
        {
            HideHover();
            return;
        }

        if (_hoverPanel.IsOpen && _currentStartIndex == startIndex && _currentEndIndex == endIndex && _currentIdentifier == identifier)
        {
            return;
        }

        RectTransform parentRect = _hoverPanel.transform.parent as RectTransform;

        if (parentRect == null)
        {
            return;
        }

        Vector2 localPoint;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, _uiCamera, out localPoint))
        {
            HideHover();
            return;
        }

        Vector2 anchoredPosition = ConvertParentLocalPointToTopLeftAnchoredPosition(parentRect, localPoint);
        anchoredPosition += _panelOffset;

        _hoverPanel.Show(symbolInfo.Kind, symbolInfo.Title, symbolInfo.Description, anchoredPosition, _viewerPresenter.GetCodeText());
        _currentIdentifier = identifier;
        _currentStartIndex = startIndex;
        _currentEndIndex = endIndex;
    }

    private bool IsPointerOverIdentifierBody(Vector2 screenPosition, int startIndex, int endIndexExclusive)
    {
        CodeDocument document = _viewerPresenter.GetDocument();
        TMP_Text codeText = _viewerPresenter.GetCodeText();

        if (document == null || codeText == null || endIndexExclusive <= startIndex)
        {
            return false;
        }

        RectTransform codeRect = codeText.rectTransform;

        int lineIndex = document.GetLineIndexFromCharacterIndex(startIndex);
        int lineStartIndex = document.GetLineStartIndex(lineIndex);
        int startColumn = startIndex - lineStartIndex;
        int endColumn = endIndexExclusive - lineStartIndex;

        Vector2 localPoint;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(codeRect, screenPosition, _uiCamera, out localPoint))
        {
            return false;
        }

        Vector2 leftCaretPosition = _viewerPresenter.GetCaretLocalPositionForHitTest(lineIndex, startColumn);
        Vector2 rightCaretPosition = _viewerPresenter.GetCaretLocalPositionForHitTest(lineIndex, endColumn);

        float xMin = Mathf.Min(leftCaretPosition.x, rightCaretPosition.x) - _horizontalHitPadding;
        float xMax = Mathf.Max(leftCaretPosition.x, rightCaretPosition.x) + _horizontalHitPadding;

        TMP_TextInfo textInfo = codeText.textInfo;

        if (textInfo == null || lineIndex < 0 || lineIndex >= textInfo.lineCount)
        {
            return false;
        }

        TMP_LineInfo lineInfo = textInfo.lineInfo[lineIndex];
        float yTop = codeRect.anchoredPosition.y + lineInfo.ascender + _verticalHitPadding;
        float yBottom = codeRect.anchoredPosition.y + lineInfo.descender - _verticalHitPadding;

        if (localPoint.x < xMin || localPoint.x > xMax)
        {
            return false;
        }

        if (localPoint.y > yTop || localPoint.y < yBottom)
        {
            return false;
        }

        return true;
    }

    private void HideHover()
    {
        if (_hoverPanel != null)
        {
            _hoverPanel.Hide();
        }

        _currentIdentifier = string.Empty;
        _currentStartIndex = -1;
        _currentEndIndex = -1;
    }

    private Vector2 ConvertParentLocalPointToTopLeftAnchoredPosition(RectTransform parentRect, Vector2 localPoint)
    {
        float anchorReferenceX = -parentRect.rect.width * parentRect.pivot.x;
        float anchorReferenceY = parentRect.rect.height * (1f - parentRect.pivot.y);
        return new Vector2(localPoint.x - anchorReferenceX, localPoint.y - anchorReferenceY);
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

        if (textInfo == null || textInfo.lineCount <= 0 || document == null)
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
        RectTransform codeTextRect = codeText.rectTransform;

        if (textInfo == null || textInfo.lineCount <= 0)
        {
            return -1;
        }

        Vector2 localPoint;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(codeTextRect, new Vector2(0f, screenY), _uiCamera, out localPoint))
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

    private int GetCaretIndexFromLineHit(int lineIndex, Vector2 screenPosition)
    {
        TMP_Text textComponent = _viewerPresenter.GetCodeText();
        CodeDocument document = _viewerPresenter.GetDocument();
        RectTransform codeTextRect = textComponent.rectTransform;
        TMP_TextInfo textInfo = textComponent.textInfo;

        if (lineIndex < 0 || lineIndex >= textInfo.lineCount || document == null)
        {
            return _viewerPresenter.GetCaretIndex();
        }

        Vector2 localPoint;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(codeTextRect, screenPosition, _uiCamera, out localPoint))
        {
            return _viewerPresenter.GetCaretIndex();
        }

        int lineStartIndex = document.GetLineStartIndex(lineIndex);
        int lineLength = document.GetLineLength(lineIndex);

        int bestCaretIndex = lineStartIndex;
        float bestDistance = float.MaxValue;

        for (int columnIndex = 0; columnIndex <= lineLength; columnIndex++)
        {
            Vector2 caretLocalPosition = _viewerPresenter.GetCaretLocalPositionForHitTest(lineIndex, columnIndex);
            float distance = Mathf.Abs(localPoint.x - caretLocalPosition.x);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestCaretIndex = lineStartIndex + columnIndex;
            }
        }

        return bestCaretIndex;
    }

    private float GetTopLineScreenTop(TMP_TextInfo textInfo)
    {
        if (textInfo == null || textInfo.lineCount <= 0)
        {
            return 0f;
        }

        RectTransform codeTextRect = _viewerPresenter.GetCodeText().rectTransform;
        TMP_LineInfo firstLine = textInfo.lineInfo[0];
        Vector3 worldTop = codeTextRect.TransformPoint(new Vector3(0f, firstLine.ascender, 0f));
        Vector2 screenTop = RectTransformUtility.WorldToScreenPoint(_uiCamera, worldTop);
        return screenTop.y;
    }

    private float GetBottomLineScreenBottom(TMP_TextInfo textInfo)
    {
        if (textInfo == null || textInfo.lineCount <= 0)
        {
            return 0f;
        }

        RectTransform codeTextRect = _viewerPresenter.GetCodeText().rectTransform;
        TMP_LineInfo lastLine = textInfo.lineInfo[textInfo.lineCount - 1];
        Vector3 worldBottom = codeTextRect.TransformPoint(new Vector3(0f, lastLine.descender, 0f));
        Vector2 screenBottom = RectTransformUtility.WorldToScreenPoint(_uiCamera, worldBottom);
        return screenBottom.y;
    }
}