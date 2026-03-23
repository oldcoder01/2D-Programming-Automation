using TMPro;
using UnityEngine;

public sealed class CodeCompletionPopupController : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private CodeEditorCodeSenseController _codeSenseController;
    [SerializeField] private CodeEditorHistoryController _historyController;
    [SerializeField] private GameObject _popupRoot;
    [SerializeField] private RectTransform _popupRect;
    [SerializeField] private TextMeshProUGUI _popupText;
    [SerializeField] private RectTransform _popupViewport;
    [SerializeField] private int _maxVisibleItems = 8;
    [SerializeField] private Vector2 _popupOffset = new Vector2(12f, -24f);
    [SerializeField] private float _minWidth = 220f;

    private CodeCompletionResult _currentResult;
    private int _selectedIndex;

    public bool IsOpen
    {
        get
        {
            return _popupRoot != null && _popupRoot.activeSelf;
        }
    }

    private void Awake()
    {
        if (_inputField == null)
        {
            _inputField = GetComponentInChildren<TMP_InputField>();
        }

        if (_codeSenseController == null)
        {
            _codeSenseController = GetComponent<CodeEditorCodeSenseController>();
        }

        if (_historyController == null)
        {
            _historyController = GetComponent<CodeEditorHistoryController>();
        }

        if (_popupRect == null && _popupRoot != null)
        {
            _popupRect = _popupRoot.GetComponent<RectTransform>();
        }

        HidePopup();
    }

    private void OnEnable()
    {
        if (_inputField != null)
        {
            _inputField.onValueChanged.AddListener(HandleValueChanged);
        }

        HidePopup();
    }

    private void OnDisable()
    {
        if (_inputField != null)
        {
            _inputField.onValueChanged.RemoveListener(HandleValueChanged);
        }
    }

    private void LateUpdate()
    {
        if (!IsOpen)
        {
            return;
        }

        UpdatePopupPosition();
    }

    public void RefreshPopup()
    {
        if (_codeSenseController == null)
        {
            HidePopup();
            return;
        }

        _currentResult = _codeSenseController.GetCompletionsAtCaret();
        _selectedIndex = 0;

        if (_currentResult == null || !_currentResult.HasItems)
        {
            HidePopup();
            return;
        }

        RebuildPopupText();
        ShowPopup();
        UpdatePopupPosition();
    }

    public void SelectNext()
    {
        if (_currentResult == null || !_currentResult.HasItems)
        {
            return;
        }

        _selectedIndex++;
        if (_selectedIndex >= _currentResult.Items.Count)
        {
            _selectedIndex = 0;
        }

        RebuildPopupText();
        UpdatePopupPosition();
    }

    public void SelectPrevious()
    {
        if (_currentResult == null || !_currentResult.HasItems)
        {
            return;
        }

        _selectedIndex--;
        if (_selectedIndex < 0)
        {
            _selectedIndex = _currentResult.Items.Count - 1;
        }

        RebuildPopupText();
        UpdatePopupPosition();
    }

    public void AcceptSelected()
    {
        if (_inputField == null || _currentResult == null || !_currentResult.HasItems)
        {
            return;
        }

        if (_selectedIndex < 0 || _selectedIndex >= _currentResult.Items.Count)
        {
            return;
        }

        CodeCompletionItem item = _currentResult.Items[_selectedIndex];

        if (_historyController != null)
        {
            _historyController.BreakTypingGroup();
            _historyController.BeginCompositeEdit();
        }

        ApplyCompletion(item);

        if (_historyController != null)
        {
            _historyController.EndCompositeEdit();
        }

        HidePopup();
    }

    public void HidePopup()
    {
        if (_popupRoot != null)
        {
            _popupRoot.SetActive(false);
        }
    }

    private void ShowPopup()
    {
        if (_popupRoot != null)
        {
            _popupRoot.SetActive(true);
        }
    }

    private void HandleValueChanged(string newValue)
    {
        RefreshPopup();
    }

    private void RebuildPopupText()
    {
        if (_popupText == null || _currentResult == null)
        {
            return;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        int visibleCount = _currentResult.Items.Count;
        if (visibleCount > _maxVisibleItems)
        {
            visibleCount = _maxVisibleItems;
        }

        for (int i = 0; i < visibleCount; i++)
        {
            CodeCompletionItem item = _currentResult.Items[i];

            if (i > 0)
            {
                builder.Append('\n');
            }

            if (i == _selectedIndex)
            {
                builder.Append("> ");
            }
            else
            {
                builder.Append("  ");
            }

            builder.Append(item.Label);

            if (item.IsLocked)
            {
                builder.Append(" [Locked]");
            }
        }

        _popupText.text = builder.ToString();
        _popupText.ForceMeshUpdate();

        if (_popupRect != null)
        {
            Vector2 size = _popupRect.sizeDelta;
            size.x = Mathf.Max(_minWidth, _popupText.preferredWidth + 24f);
            size.y = _popupText.preferredHeight + 20f;
            _popupRect.sizeDelta = size;
        }
    }

    private void UpdatePopupPosition()
    {
        if (_popupRect == null || _inputField == null || _inputField.textComponent == null)
        {
            return;
        }

        RectTransform textRect = _inputField.textComponent.rectTransform;
        RectTransform targetParent = _popupRect.parent as RectTransform;

        if (targetParent == null)
        {
            return;
        }

        Vector2 localCaretPosition = GetCaretLocalPosition();
        Vector3 worldCaretPosition = textRect.TransformPoint(localCaretPosition);
        Vector2 anchoredPosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetParent,
            RectTransformUtility.WorldToScreenPoint(null, worldCaretPosition),
            null,
            out anchoredPosition
        );

        anchoredPosition += _popupOffset;
        anchoredPosition = ClampToViewport(anchoredPosition);

        _popupRect.anchoredPosition = anchoredPosition;
    }

    private Vector2 GetCaretLocalPosition()
    {
        TMP_Text textComponent = _inputField.textComponent;
        string text = _inputField.text;

        if (textComponent == null)
        {
            return Vector2.zero;
        }

        if (text == null)
        {
            text = string.Empty;
        }

        textComponent.ForceMeshUpdate();

        int caretIndex = _inputField.stringPosition;
        if (caretIndex < 0)
        {
            caretIndex = 0;
        }

        if (caretIndex > text.Length)
        {
            caretIndex = text.Length;
        }

        TMP_TextInfo textInfo = textComponent.textInfo;
        if (textInfo == null || textInfo.characterCount == 0)
        {
            return new Vector2(0f, 0f);
        }

        if (caretIndex == text.Length)
        {
            int lastVisibleIndex = GetLastVisibleCharacterIndex(textInfo);
            if (lastVisibleIndex < 0)
            {
                return new Vector2(0f, 0f);
            }

            TMP_CharacterInfo lastCharacter = textInfo.characterInfo[lastVisibleIndex];
            return new Vector2(lastCharacter.topRight.x, lastCharacter.descender);
        }

        int characterIndex = GetCharacterIndexForCaret(textInfo, caretIndex);
        if (characterIndex < 0)
        {
            return new Vector2(0f, 0f);
        }

        TMP_CharacterInfo character = textInfo.characterInfo[characterIndex];
        return new Vector2(character.bottomLeft.x, character.descender);
    }

    private int GetLastVisibleCharacterIndex(TMP_TextInfo textInfo)
    {
        for (int i = textInfo.characterCount - 1; i >= 0; i--)
        {
            if (textInfo.characterInfo[i].isVisible)
            {
                return i;
            }
        }

        return -1;
    }

    private int GetCharacterIndexForCaret(TMP_TextInfo textInfo, int caretIndex)
    {
        if (caretIndex < 0)
        {
            return -1;
        }

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (textInfo.characterInfo[i].index >= caretIndex)
            {
                return i;
            }
        }

        return textInfo.characterCount - 1;
    }

    private Vector2 ClampToViewport(Vector2 anchoredPosition)
    {
        if (_popupViewport == null || _popupRect == null)
        {
            return anchoredPosition;
        }

        Rect viewportRect = _popupViewport.rect;
        Vector2 size = _popupRect.sizeDelta;

        float minX = viewportRect.xMin;
        float maxX = viewportRect.xMax - size.x;
        float minY = viewportRect.yMin + size.y;
        float maxY = viewportRect.yMax;

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

        return anchoredPosition;
    }

    private void ApplyCompletion(CodeCompletionItem item)
    {
        string source = _inputField.text;
        if (source == null)
        {
            source = string.Empty;
        }

        int start = _currentResult.ReplaceStartIndex;
        int length = _currentResult.ReplaceLength;

        if (start < 0)
        {
            start = 0;
        }

        if (start > source.Length)
        {
            start = source.Length;
        }

        if (length < 0)
        {
            length = 0;
        }

        if (start + length > source.Length)
        {
            length = source.Length - start;
        }

        string insertText = item.InsertText;
        string updated = source.Substring(0, start) + insertText + source.Substring(start + length);
        int newCaret = start + insertText.Length;

        _inputField.text = updated;
        _inputField.stringPosition = newCaret;
        _inputField.selectionStringAnchorPosition = newCaret;
        _inputField.selectionStringFocusPosition = newCaret;
        _inputField.caretPosition = newCaret;
        _inputField.selectionAnchorPosition = newCaret;
        _inputField.selectionFocusPosition = newCaret;
        _inputField.ForceLabelUpdate();
    }

    private bool ShouldPlaceCaretInsideParentheses(CodeCompletionItem item, string source, int replaceStart, int replaceLength)
    {
        if (item == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(item.InsertText))
        {
            return false;
        }

        if (!item.InsertText.EndsWith("()"))
        {
            return false;
        }

        if (source == null)
        {
            return true;
        }

        int nextIndex = replaceStart + replaceLength;
        if (nextIndex < 0 || nextIndex >= source.Length)
        {
            return true;
        }

        if (HasOpenParenImmediatelyAfter(source, nextIndex))
        {
            return false;
        }

        return true;
    }

    private bool HasOpenParenImmediatelyAfter(string source, int index)
    {
        if (string.IsNullOrEmpty(source))
        {
            return false;
        }

        int i = index;

        while (i < source.Length)
        {
            char c = source[i];

            if (c == ' ' || c == '\t')
            {
                i++;
                continue;
            }

            return c == '(';
        }

        return false;
    }

    private int GetCaretOffsetAfterInsert(string insertText, bool placeInsideParentheses)
    {
        if (string.IsNullOrEmpty(insertText))
        {
            return 0;
        }

        if (placeInsideParentheses && insertText.EndsWith("()"))
        {
            return insertText.Length - 1;
        }

        return insertText.Length;
    }
}