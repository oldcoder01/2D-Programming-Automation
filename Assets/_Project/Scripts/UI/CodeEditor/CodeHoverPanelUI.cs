using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public sealed class CodeHoverPanelUI : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private Color _backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.96f);
    [SerializeField] private Color _borderColor = new Color(0.25f, 0.35f, 0.50f, 1f);
    [SerializeField] private Color _defaultTitleColor = Color.white;
    [SerializeField] private Color _keywordTitleColor = new Color(0.35f, 0.75f, 1f, 1f);
    [SerializeField] private Color _actionTitleColor = new Color(0.95f, 0.80f, 0.35f, 1f);
    [SerializeField] private Color _queryTitleColor = new Color(0.65f, 0.90f, 0.45f, 1f);
    [SerializeField] private Color _userFunctionTitleColor = new Color(0.90f, 0.60f, 1f, 1f);
    [SerializeField] private Color _descriptionColor = new Color(0.82f, 0.82f, 0.82f, 1f);
    [SerializeField] private Vector2 _padding = new Vector2(10f, 8f);
    [SerializeField] private float _width = 300f;
    [SerializeField] private float _titleHeight = 24f;
    [SerializeField] private float _descriptionMinHeight = 44f;
    [SerializeField] private float _descriptionMaxHeight = 120f;

    private RectTransform _rectTransform;
    private Image _backgroundImage;
    private Outline _outline;
    private TMP_Text _titleText;
    private TMP_Text _descriptionText;

    private TMP_FontAsset _fontAsset;
    private Material _fontMaterial;
    private float _fontSize = 20f;

    public bool IsOpen
    {
        get { return gameObject.activeSelf; }
    }

    private void Awake()
    {
        EnsureRootComponents();
        Hide();
    }

    public void InitializeFromReferenceText(TMP_Text referenceText)
    {
        if (referenceText == null)
        {
            return;
        }

        _fontAsset = referenceText.font;
        _fontMaterial = referenceText.fontSharedMaterial;
        _fontSize = referenceText.fontSize;

        ApplyReferenceStyle(_titleText, true);
        ApplyReferenceStyle(_descriptionText, false);
    }

    public void Show(CodeSymbolKind symbolKind, string title, string description, Vector2 parentLocalPosition, TMP_Text referenceText)
    {
        EnsureRootComponents();
        InitializeFromReferenceText(referenceText);

        _titleText.text = string.IsNullOrEmpty(title) ? string.Empty : title;
        _descriptionText.text = string.IsNullOrEmpty(description) ? string.Empty : description;
        _titleText.color = GetTitleColor(symbolKind);
        _descriptionText.color = _descriptionColor;

        float descriptionPreferredHeight = _descriptionText.GetPreferredValues(_width - (_padding.x * 2f), 0f).y;
        float descriptionHeight = Mathf.Clamp(descriptionPreferredHeight, _descriptionMinHeight, _descriptionMaxHeight);
        float height = _padding.y + _titleHeight + 4f + descriptionHeight + _padding.y;

        _rectTransform.anchorMin = new Vector2(0f, 1f);
        _rectTransform.anchorMax = new Vector2(0f, 1f);
        _rectTransform.pivot = new Vector2(0f, 1f);
        _rectTransform.anchoredPosition = parentLocalPosition;
        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _width);
        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        RectTransform titleRect = _titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.offsetMin = new Vector2(_padding.x, -(_padding.y + _titleHeight));
        titleRect.offsetMax = new Vector2(-_padding.x, -_padding.y);

        RectTransform descriptionRect = _descriptionText.rectTransform;
        descriptionRect.anchorMin = new Vector2(0f, 1f);
        descriptionRect.anchorMax = new Vector2(1f, 1f);
        descriptionRect.pivot = new Vector2(0f, 1f);
        descriptionRect.offsetMin = new Vector2(_padding.x, -(_padding.y + _titleHeight + 4f + descriptionHeight));
        descriptionRect.offsetMax = new Vector2(-_padding.x, -(_padding.y + _titleHeight + 4f));

        gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
        ClampToParentBounds();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void EnsureRootComponents()
    {
        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        if (_backgroundImage == null)
        {
            _backgroundImage = GetComponent<Image>();

            if (_backgroundImage == null)
            {
                _backgroundImage = gameObject.AddComponent<Image>();
            }
        }

        _backgroundImage.color = _backgroundColor;

        if (_outline == null)
        {
            _outline = GetComponent<Outline>();

            if (_outline == null)
            {
                _outline = gameObject.AddComponent<Outline>();
            }
        }

        _outline.effectColor = _borderColor;
        _outline.effectDistance = new Vector2(1f, -1f);
        _outline.useGraphicAlpha = true;

        if (_titleText == null)
        {
            _titleText = CreateTextChild("Title");
            _titleText.color = _defaultTitleColor;
            _titleText.fontStyle = FontStyles.Bold;
        }

        if (_descriptionText == null)
        {
            _descriptionText = CreateTextChild("Description");
            _descriptionText.color = _descriptionColor;
            _descriptionText.textWrappingMode = TextWrappingModes.Normal;
        }
    }

    private TMP_Text CreateTextChild(string objectName)
    {
        Transform existingChild = transform.Find(objectName);
        GameObject childObject;

        if (existingChild != null)
        {
            childObject = existingChild.gameObject;
        }
        else
        {
            childObject = new GameObject(objectName, typeof(RectTransform));
            childObject.transform.SetParent(transform, false);
        }

        TextMeshProUGUI textComponent = childObject.GetComponent<TextMeshProUGUI>();

        if (textComponent == null)
        {
            textComponent = childObject.AddComponent<TextMeshProUGUI>();
        }

        textComponent.raycastTarget = false;
        textComponent.alignment = TextAlignmentOptions.TopLeft;
        textComponent.overflowMode = TextOverflowModes.Overflow;
        textComponent.textWrappingMode = TextWrappingModes.NoWrap;
        return textComponent;
    }

    private void ApplyReferenceStyle(TMP_Text target, bool bold)
    {
        if (target == null)
        {
            return;
        }

        if (_fontAsset != null)
        {
            target.font = _fontAsset;
        }

        if (_fontMaterial != null)
        {
            target.fontSharedMaterial = _fontMaterial;
        }

        if (_fontSize > 0f)
        {
            target.fontSize = bold ? _fontSize : _fontSize - 1f;
        }

        target.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
    }

    private Color GetTitleColor(CodeSymbolKind symbolKind)
    {
        switch (symbolKind)
        {
            case CodeSymbolKind.Keyword:
                return _keywordTitleColor;

            case CodeSymbolKind.Action:
                return _actionTitleColor;

            case CodeSymbolKind.Query:
                return _queryTitleColor;

            case CodeSymbolKind.UserFunction:
                return _userFunctionTitleColor;
        }

        return _defaultTitleColor;
    }

    private void ClampToParentBounds()
    {
        RectTransform parentRect = transform.parent as RectTransform;

        if (parentRect == null)
        {
            return;
        }

        Vector2 anchoredPosition = _rectTransform.anchoredPosition;
        float minX = 4f;
        float maxX = parentRect.rect.width - _rectTransform.rect.width - 4f;
        float maxY = -4f;
        float minY = -parentRect.rect.height + _rectTransform.rect.height + 4f;

        if (anchoredPosition.x < minX)
        {
            anchoredPosition.x = minX;
        }

        if (anchoredPosition.x > maxX)
        {
            anchoredPosition.x = maxX;
        }

        if (anchoredPosition.y > maxY)
        {
            anchoredPosition.y = maxY;
        }

        if (anchoredPosition.y < minY)
        {
            anchoredPosition.y = minY;
        }

        _rectTransform.anchoredPosition = anchoredPosition;
    }
}