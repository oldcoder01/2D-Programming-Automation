using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public sealed class CodeCompletionPopupUI : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private Color _backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.96f);
    [SerializeField] private Color _borderColor = new Color(0.25f, 0.35f, 0.50f, 1f);
    [SerializeField] private Color _textColor = new Color(0.88f, 0.88f, 0.88f, 1f);
    [SerializeField] private Color _selectedBackgroundColor = new Color(0.18f, 0.32f, 0.55f, 0.95f);
    [SerializeField] private Color _selectedTextColor = Color.white;
    [SerializeField] private Color _detailTitleColor = Color.white;
    [SerializeField] private Color _detailDescriptionColor = new Color(0.82f, 0.82f, 0.82f, 1f);
    [SerializeField] private Color _separatorColor = new Color(1f, 1f, 1f, 0.12f);
    [SerializeField] private Vector2 _padding = new Vector2(8f, 6f);
    [SerializeField] private float _rowHeight = 24f;
    [SerializeField] private float _width = 280f;
    [SerializeField] private int _maxVisibleRows = 8;
    [SerializeField] private float _detailSpacing = 6f;
    [SerializeField] private float _detailTitleHeight = 22f;
    [SerializeField] private float _detailDescriptionMinHeight = 38f;
    [SerializeField] private float _detailDescriptionMaxHeight = 100f;

    private readonly List<TMP_Text> _rowTexts = new List<TMP_Text>();
    private readonly List<Image> _rowBackgrounds = new List<Image>();
    private readonly List<CodeCompletionItem> _items = new List<CodeCompletionItem>();

    private RectTransform _rectTransform;
    private Image _backgroundImage;
    private Outline _outline;

    private TMP_FontAsset _fontAsset;
    private Material _fontMaterial;
    private float _fontSize = 20f;

    private int _selectedIndex;

    private RectTransform _listRoot;
    private RectTransform _detailRoot;
    private Image _separatorImage;
    private TMP_Text _detailTitleText;
    private TMP_Text _detailDescriptionText;

    public event Action<CodeCompletionItem> SelectionChanged;
    public event Action<CodeCompletionItem> ItemClicked;

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

        for (int i = 0; i < _rowTexts.Count; i++)
        {
            ApplyReferenceStyle(_rowTexts[i], false);
        }

        ApplyReferenceStyle(_detailTitleText, true);
        ApplyReferenceStyle(_detailDescriptionText, false);
    }

    public void Show(IReadOnlyList<CodeCompletionItem> items, int selectedIndex, Vector2 parentLocalPosition, TMP_Text referenceText)
    {
        EnsureRootComponents();
        InitializeFromReferenceText(referenceText);

        _items.Clear();

        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                _items.Add(items[i]);
            }
        }

        if (_items.Count <= 0)
        {
            Hide();
            return;
        }

        _selectedIndex = Mathf.Clamp(selectedIndex, 0, _items.Count - 1);

        int visibleRowCount = Mathf.Min(_items.Count, _maxVisibleRows);
        EnsureRowCount(visibleRowCount);

        for (int i = 0; i < _rowTexts.Count; i++)
        {
            bool active = i < visibleRowCount;
            _rowTexts[i].transform.parent.gameObject.SetActive(active);

            if (!active)
            {
                continue;
            }

            CodeCompletionItem item = _items[i];
            _rowTexts[i].text = item.Label;
        }

        RefreshSelectionVisuals();
        RefreshDetailArea();

        _rectTransform.anchorMin = new Vector2(0f, 1f);
        _rectTransform.anchorMax = new Vector2(0f, 1f);
        _rectTransform.pivot = new Vector2(0f, 1f);

        float popupHeight = CalculatePopupHeight(visibleRowCount);

        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _width);
        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, popupHeight);

        RectTransform parentRect = transform.parent as RectTransform;
        Vector2 anchoredPosition = Vector2.zero;

        if (parentRect != null)
        {
            float anchorReferenceX = -parentRect.rect.width * parentRect.pivot.x;
            float anchorReferenceY = parentRect.rect.height * (1f - parentRect.pivot.y);

            anchoredPosition.x = parentLocalPosition.x - anchorReferenceX + 8f;
            anchoredPosition.y = parentLocalPosition.y - anchorReferenceY - 4f;
        }
        else
        {
            anchoredPosition = parentLocalPosition;
        }

        _rectTransform.anchoredPosition = anchoredPosition;

        gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
        ClampToParentBounds();
        NotifySelectionChanged();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        _items.Clear();
        _selectedIndex = 0;
        ClearDetailArea();
        NotifySelectionChanged();
    }

    public void MoveSelection(int delta)
    {
        if (_items.Count <= 0)
        {
            return;
        }

        _selectedIndex += delta;

        if (_selectedIndex < 0)
        {
            _selectedIndex = _items.Count - 1;
        }

        if (_selectedIndex >= _items.Count)
        {
            _selectedIndex = 0;
        }

        RefreshSelectionVisuals();
        RefreshDetailArea();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
        ClampToParentBounds();
        NotifySelectionChanged();
    }

    public void SetSelectedIndex(int selectedIndex)
    {
        if (_items.Count <= 0)
        {
            _selectedIndex = 0;
            RefreshSelectionVisuals();
            RefreshDetailArea();
            NotifySelectionChanged();
            return;
        }

        int clampedIndex = selectedIndex;

        if (clampedIndex < 0)
        {
            clampedIndex = 0;
        }

        if (clampedIndex >= _items.Count)
        {
            clampedIndex = _items.Count - 1;
        }

        if (_selectedIndex == clampedIndex)
        {
            return;
        }

        _selectedIndex = clampedIndex;
        RefreshSelectionVisuals();
        RefreshDetailArea();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
        ClampToParentBounds();
        NotifySelectionChanged();
    }

    public CodeCompletionItem GetSelectedItem()
    {
        if (_items.Count <= 0)
        {
            return null;
        }

        if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
        {
            return null;
        }

        return _items[_selectedIndex];
    }

    private void NotifySelectionChanged()
    {
        if (SelectionChanged != null)
        {
            SelectionChanged(GetSelectedItem());
        }
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
        _backgroundImage.raycastTarget = true;

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

        EnsureListRoot();
        EnsureDetailRoot();
    }

    private void EnsureListRoot()
    {
        if (_listRoot != null)
        {
            return;
        }

        Transform existing = transform.Find("ListRoot");

        if (existing != null)
        {
            _listRoot = existing as RectTransform;
            return;
        }

        GameObject listObject = new GameObject("ListRoot", typeof(RectTransform));
        _listRoot = listObject.GetComponent<RectTransform>();
        _listRoot.SetParent(transform, false);
        _listRoot.anchorMin = new Vector2(0f, 1f);
        _listRoot.anchorMax = new Vector2(0f, 1f);
        _listRoot.pivot = new Vector2(0f, 1f);
    }

    private void EnsureDetailRoot()
    {
        if (_detailRoot != null)
        {
            return;
        }

        Transform existing = transform.Find("DetailRoot");

        if (existing != null)
        {
            _detailRoot = existing as RectTransform;
        }
        else
        {
            GameObject detailObject = new GameObject("DetailRoot", typeof(RectTransform));
            _detailRoot = detailObject.GetComponent<RectTransform>();
            _detailRoot.SetParent(transform, false);
        }

        _detailRoot.anchorMin = new Vector2(0f, 1f);
        _detailRoot.anchorMax = new Vector2(0f, 1f);
        _detailRoot.pivot = new Vector2(0f, 1f);

        EnsureSeparator();
        EnsureDetailTexts();
    }

    private void EnsureSeparator()
    {
        if (_separatorImage != null)
        {
            return;
        }

        Transform existing = _detailRoot.Find("Separator");

        if (existing != null)
        {
            _separatorImage = existing.GetComponent<Image>();
        }
        else
        {
            GameObject separatorObject = new GameObject("Separator", typeof(RectTransform), typeof(Image));
            separatorObject.transform.SetParent(_detailRoot, false);
            _separatorImage = separatorObject.GetComponent<Image>();
        }

        _separatorImage.color = _separatorColor;
        _separatorImage.raycastTarget = true;

        RectTransform separatorRect = _separatorImage.rectTransform;
        separatorRect.anchorMin = new Vector2(0f, 1f);
        separatorRect.anchorMax = new Vector2(0f, 1f);
        separatorRect.pivot = new Vector2(0f, 1f);
    }

    private void EnsureDetailTexts()
    {
        if (_detailTitleText == null)
        {
            Transform existingTitle = _detailRoot.Find("DetailTitle");

            if (existingTitle != null)
            {
                _detailTitleText = existingTitle.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                GameObject titleObject = new GameObject("DetailTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                titleObject.transform.SetParent(_detailRoot, false);
                _detailTitleText = titleObject.GetComponent<TextMeshProUGUI>();
            }
        }

        if (_detailDescriptionText == null)
        {
            Transform existingDescription = _detailRoot.Find("DetailDescription");

            if (existingDescription != null)
            {
                _detailDescriptionText = existingDescription.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                GameObject descriptionObject = new GameObject("DetailDescription", typeof(RectTransform), typeof(TextMeshProUGUI));
                descriptionObject.transform.SetParent(_detailRoot, false);
                _detailDescriptionText = descriptionObject.GetComponent<TextMeshProUGUI>();
            }
        }

        _detailTitleText.raycastTarget = true;
        _detailTitleText.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Top;
        _detailTitleText.textWrappingMode = TextWrappingModes.NoWrap;
        _detailTitleText.overflowMode = TextOverflowModes.Ellipsis;

        _detailDescriptionText.raycastTarget = true;
        _detailDescriptionText.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Top;
        _detailDescriptionText.textWrappingMode = TextWrappingModes.Normal;
        _detailDescriptionText.overflowMode = TextOverflowModes.Overflow;

        ApplyReferenceStyle(_detailTitleText, true);
        ApplyReferenceStyle(_detailDescriptionText, false);
    }

    private void EnsureRowCount(int count)
    {
        while (_rowTexts.Count < count)
        {
            CreateRow();
        }
    }

    private void CreateRow()
    {
        GameObject rowObject = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        rowObject.transform.SetParent(_listRoot, false);

        Image rowBackground = rowObject.GetComponent<Image>();
        rowBackground.raycastTarget = true;
        rowBackground.color = Color.clear;

        LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
        layoutElement.minHeight = _rowHeight;
        layoutElement.preferredHeight = _rowHeight;
        layoutElement.flexibleHeight = 0f;

        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(0f, 1f);
        rowRect.pivot = new Vector2(0f, 1f);

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(rowObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(8f, 0f);
        textRect.offsetMax = new Vector2(-8f, 0f);
        textRect.pivot = new Vector2(0f, 0.5f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = true;
        text.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        ApplyReferenceStyle(text, false);

        CodeCompletionPopupRowHandler rowHandler = rowObject.AddComponent<CodeCompletionPopupRowHandler>();
        rowHandler.Initialize(this, _rowTexts.Count);

        _rowBackgrounds.Add(rowBackground);
        _rowTexts.Add(text);
    }

    private void ApplyReferenceStyle(TMP_Text text, bool isTitle)
    {
        if (text == null)
        {
            return;
        }

        if (_fontAsset != null)
        {
            text.font = _fontAsset;
        }

        if (_fontMaterial != null)
        {
            text.fontSharedMaterial = _fontMaterial;
        }

        if (isTitle)
        {
            text.fontSize = Mathf.Max(14f, _fontSize * 0.9f);
            text.color = _detailTitleColor;
            text.fontStyle = FontStyles.Bold;
        }
        else
        {
            text.fontSize = Mathf.Max(13f, _fontSize * 0.82f);
            text.color = _textColor;
            text.fontStyle = FontStyles.Normal;
        }
    }

    private void RefreshSelectionVisuals()
    {
        for (int i = 0; i < _rowTexts.Count; i++)
        {
            if (!_rowTexts[i].transform.parent.gameObject.activeSelf)
            {
                continue;
            }

            bool selected = i == _selectedIndex;
            _rowBackgrounds[i].color = selected ? _selectedBackgroundColor : Color.clear;
            _rowTexts[i].color = selected ? _selectedTextColor : _textColor;
        }
    }

    private void RefreshDetailArea()
    {
        CodeCompletionItem item = GetSelectedItem();

        if (item == null || string.IsNullOrWhiteSpace(item.Description))
        {
            ClearDetailArea();
            return;
        }

        _detailRoot.gameObject.SetActive(true);
        _separatorImage.gameObject.SetActive(true);

        _detailTitleText.text = item.Label;
        _detailDescriptionText.text = item.Description;
        _detailTitleText.color = _detailTitleColor;
        _detailDescriptionText.color = _detailDescriptionColor;

        LayoutDetailArea();
    }

    private void ClearDetailArea()
    {
        if (_detailRoot != null)
        {
            _detailRoot.gameObject.SetActive(false);
        }

        if (_detailTitleText != null)
        {
            _detailTitleText.text = string.Empty;
        }

        if (_detailDescriptionText != null)
        {
            _detailDescriptionText.text = string.Empty;
        }
    }

    private void LayoutDetailArea()
    {
        float innerWidth = _width - (_padding.x * 2f);
        float rowAreaHeight = GetVisibleRowCount() * _rowHeight;

        _listRoot.anchoredPosition = new Vector2(_padding.x, -_padding.y);
        _listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, innerWidth);
        _listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowAreaHeight);

        for (int i = 0; i < _rowTexts.Count; i++)
        {
            RectTransform rowRect = _rowTexts[i].transform.parent as RectTransform;

            if (rowRect == null || !rowRect.gameObject.activeSelf)
            {
                continue;
            }

            rowRect.anchoredPosition = new Vector2(0f, -(i * _rowHeight));
            rowRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, innerWidth);
            rowRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _rowHeight);
        }

        if (!_detailRoot.gameObject.activeSelf)
        {
            return;
        }

        float detailTop = _padding.y + rowAreaHeight + _detailSpacing;

        RectTransform separatorRect = _separatorImage.rectTransform;
        separatorRect.anchoredPosition = new Vector2(0f, -detailTop);
        separatorRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, innerWidth);
        separatorRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1f);

        float titleTop = detailTop + 7f;
        RectTransform titleRect = _detailTitleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -titleTop);
        titleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, innerWidth);
        titleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _detailTitleHeight);

        float descriptionTop = titleTop + _detailTitleHeight + 2f;
        float descriptionHeight = CalculateDetailDescriptionHeight(innerWidth);

        RectTransform descriptionRect = _detailDescriptionText.rectTransform;
        descriptionRect.anchorMin = new Vector2(0f, 1f);
        descriptionRect.anchorMax = new Vector2(0f, 1f);
        descriptionRect.pivot = new Vector2(0f, 1f);
        descriptionRect.anchoredPosition = new Vector2(0f, -descriptionTop);
        descriptionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, innerWidth);
        descriptionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, descriptionHeight);

        _detailRoot.anchoredPosition = new Vector2(_padding.x, 0f);
        _detailRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, innerWidth);
        _detailRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, descriptionTop + descriptionHeight);
    }

    private float CalculatePopupHeight(int visibleRowCount)
    {
        float rowAreaHeight = visibleRowCount * _rowHeight;
        float totalHeight = (_padding.y * 2f) + rowAreaHeight;

        CodeCompletionItem item = GetSelectedItem();

        if (item != null && !string.IsNullOrWhiteSpace(item.Description))
        {
            float innerWidth = _width - (_padding.x * 2f);
            float descriptionHeight = CalculateDetailDescriptionHeight(innerWidth);
            totalHeight += _detailSpacing + 1f + 7f + _detailTitleHeight + 2f + descriptionHeight;
        }

        return totalHeight;
    }

    private float CalculateDetailDescriptionHeight(float width)
    {
        if (_detailDescriptionText == null)
        {
            return _detailDescriptionMinHeight;
        }

        string description = _detailDescriptionText.text;

        if (string.IsNullOrWhiteSpace(description))
        {
            return _detailDescriptionMinHeight;
        }

        Vector2 preferred = _detailDescriptionText.GetPreferredValues(description, width, 1000f);
        float height = preferred.y;

        if (height < _detailDescriptionMinHeight)
        {
            height = _detailDescriptionMinHeight;
        }

        if (height > _detailDescriptionMaxHeight)
        {
            height = _detailDescriptionMaxHeight;
        }

        return height;
    }

    private int GetVisibleRowCount()
    {
        int visibleRowCount = _items.Count;

        if (visibleRowCount > _maxVisibleRows)
        {
            visibleRowCount = _maxVisibleRows;
        }

        return visibleRowCount;
    }

    private void ClampToParentBounds()
    {
        RectTransform parentRect = transform.parent as RectTransform;

        if (parentRect == null)
        {
            return;
        }

        Vector2 anchoredPosition = _rectTransform.anchoredPosition;
        float width = _rectTransform.rect.width;
        float height = _rectTransform.rect.height;
        float parentWidth = parentRect.rect.width;
        float parentHeight = parentRect.rect.height;

        if (anchoredPosition.x + width > parentWidth)
        {
            anchoredPosition.x = parentWidth - width - 4f;
        }

        if (anchoredPosition.x < 0f)
        {
            anchoredPosition.x = 0f;
        }

        if (-anchoredPosition.y + height > parentHeight)
        {
            anchoredPosition.y = -(parentHeight - height - 4f);
        }

        _rectTransform.anchoredPosition = anchoredPosition;
    }
    private void HandleRowPointerEnter(int rowIndex)
    {
        if (!IsOpen)
        {
            return;
        }

        SetSelectedIndex(rowIndex);
    }

    private void HandleRowPointerClick(int rowIndex)
    {
        if (!IsOpen)
        {
            return;
        }

        SetSelectedIndex(rowIndex);

        if (ItemClicked != null)
        {
            ItemClicked(GetSelectedItem());
        }
    }

    private sealed class CodeCompletionPopupRowHandler : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        private CodeCompletionPopupUI _owner;
        private int _rowIndex;

        public void Initialize(CodeCompletionPopupUI owner, int rowIndex)
        {
            _owner = owner;
            _rowIndex = rowIndex;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_owner == null)
            {
                return;
            }

            _owner.HandleRowPointerEnter(_rowIndex);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_owner == null)
            {
                return;
            }

            if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _owner.HandleRowPointerClick(_rowIndex);
        }
    }
}
