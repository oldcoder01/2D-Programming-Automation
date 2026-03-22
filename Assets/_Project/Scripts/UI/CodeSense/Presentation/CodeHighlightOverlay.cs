using TMPro;
using UnityEngine;

public sealed class CodeHighlightOverlay : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private TextMeshProUGUI _highlightText;
    [SerializeField] private RectTransform _inputTextViewport;
    [SerializeField] private RectTransform _highlightViewport;

    private Vector2 _lastAnchoredPosition;
    private Vector2 _lastSizeDelta;

    private void Awake()
    {
        ValidateReferences();
        SyncVisualSettings();
        SyncViewport();
    }

    private void LateUpdate()
    {
        if (_inputField == null || _highlightText == null)
        {
            return;
        }

        SyncVisualSettings();
        SyncViewport();
        SyncScrollPosition();
    }

    public void SetHighlightedText(string richText)
    {
        if (_highlightText == null)
        {
            return;
        }

        _highlightText.text = richText;
    }

    public void Clear()
    {
        if (_highlightText == null)
        {
            return;
        }

        _highlightText.text = string.Empty;
    }

    private void ValidateReferences()
    {
        if (_inputField == null)
        {
            _inputField = GetComponentInChildren<TMP_InputField>();
        }
    }

    private void SyncVisualSettings()
    {
        if (_inputField == null || _inputField.textComponent == null || _highlightText == null)
        {
            return;
        }

        TMP_Text inputText = _inputField.textComponent;

        _highlightText.font = inputText.font;
        _highlightText.fontSharedMaterial = inputText.fontSharedMaterial;
        _highlightText.fontSize = inputText.fontSize;
        _highlightText.fontStyle = inputText.fontStyle;
        _highlightText.alignment = inputText.alignment;
        _highlightText.textWrappingMode = inputText.textWrappingMode;
        _highlightText.overflowMode = inputText.overflowMode;
        _highlightText.lineSpacing = inputText.lineSpacing;
        _highlightText.characterSpacing = inputText.characterSpacing;
        _highlightText.wordSpacing = inputText.wordSpacing;
        _highlightText.paragraphSpacing = inputText.paragraphSpacing;
        _highlightText.margin = inputText.margin;
        _highlightText.richText = true;
        _highlightText.raycastTarget = false;

        RectTransform inputRect = inputText.rectTransform;
        RectTransform highlightRect = _highlightText.rectTransform;

        highlightRect.anchorMin = inputRect.anchorMin;
        highlightRect.anchorMax = inputRect.anchorMax;
        highlightRect.pivot = inputRect.pivot;
        highlightRect.anchoredPosition = inputRect.anchoredPosition;
        highlightRect.sizeDelta = inputRect.sizeDelta;
        highlightRect.localRotation = inputRect.localRotation;
        highlightRect.localScale = inputRect.localScale;
    }

    private void SyncViewport()
    {
        if (_inputTextViewport == null || _highlightViewport == null)
        {
            return;
        }

        if (_highlightViewport.anchoredPosition != _inputTextViewport.anchoredPosition)
        {
            _highlightViewport.anchoredPosition = _inputTextViewport.anchoredPosition;
        }

        if (_highlightViewport.sizeDelta != _inputTextViewport.sizeDelta)
        {
            _highlightViewport.sizeDelta = _inputTextViewport.sizeDelta;
        }
    }

    private void SyncScrollPosition()
    {
        if (_inputField == null || _inputField.textComponent == null || _highlightText == null)
        {
            return;
        }

        RectTransform inputRect = _inputField.textComponent.rectTransform;
        RectTransform highlightRect = _highlightText.rectTransform;

        if (inputRect.anchoredPosition != _lastAnchoredPosition)
        {
            _lastAnchoredPosition = inputRect.anchoredPosition;
            highlightRect.anchoredPosition = inputRect.anchoredPosition;
        }

        if (inputRect.sizeDelta != _lastSizeDelta)
        {
            _lastSizeDelta = inputRect.sizeDelta;
            highlightRect.sizeDelta = inputRect.sizeDelta;
        }
    }
}