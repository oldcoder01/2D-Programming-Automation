using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CodeHoverSymbolInfoController : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private CodeEditorCodeSenseController _codeSenseController;
    [SerializeField] private CodeSymbolInfoPanel _symbolInfoPanel;
    [SerializeField] private Camera _uiCamera;
    [SerializeField] private bool _logHoverDebug;

    private CodeSymbolLocator _symbolLocator;
    private int _lastHoveredSourceIndex = -1;

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

        if (_symbolLocator == null)
        {
            _symbolLocator = new CodeSymbolLocator();
        }
    }

    private void OnEnable()
    {
        HidePanel();
    }

    private void LateUpdate()
    {
        UpdateHoveredSymbol();
    }

    private void UpdateHoveredSymbol()
    {
        if (_inputField == null || _codeSenseController == null || _symbolInfoPanel == null)
        {
            return;
        }

        TMP_Text sourceText = _inputField.textComponent;
        if (sourceText == null)
        {
            HidePanel();
            return;
        }

        if (Mouse.current == null)
        {
            HidePanel();
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        if (!RectTransformUtility.RectangleContainsScreenPoint(sourceText.rectTransform, mousePosition, _uiCamera))
        {
            _lastHoveredSourceIndex = -1;
            HidePanel();
            return;
        }

        sourceText.ForceMeshUpdate();

        int characterIndex = TMP_TextUtilities.FindIntersectingCharacter(sourceText, mousePosition, _uiCamera, true);

        if (characterIndex < 0)
        {
            characterIndex = TMP_TextUtilities.FindNearestCharacter(sourceText, mousePosition, _uiCamera, true);
        }

        if (characterIndex < 0)
        {
            _lastHoveredSourceIndex = -1;
            HidePanel();
            return;
        }

        TMP_TextInfo textInfo = sourceText.textInfo;
        if (textInfo == null || characterIndex >= textInfo.characterCount)
        {
            _lastHoveredSourceIndex = -1;
            HidePanel();
            return;
        }

        TMP_CharacterInfo characterInfo = textInfo.characterInfo[characterIndex];

        if (!characterInfo.isVisible)
        {
            _lastHoveredSourceIndex = -1;
            HidePanel();
            return;
        }

        int sourceIndex = characterInfo.index;
        if (sourceIndex < 0)
        {
            _lastHoveredSourceIndex = -1;
            HidePanel();
            return;
        }

        if (sourceIndex == _lastHoveredSourceIndex)
        {
            return;
        }

        _lastHoveredSourceIndex = sourceIndex;

        CodeSymbolLookupResult lookupResult = _symbolLocator.FindSymbolAtSourceIndex(
            _codeSenseController.LatestAnalysisResult,
            _codeSenseController.LatestRegistry,
            sourceIndex
        );

        if (_logHoverDebug)
        {
            Debug.Log("Hover charIndex=" + characterIndex + " sourceIndex=" + sourceIndex + " symbol=" + (lookupResult != null ? lookupResult.SymbolText : "<none>"));
        }

        if (lookupResult == null)
        {
            HidePanel();
            return;
        }

        _symbolInfoPanel.Show(lookupResult);
    }

    private void HidePanel()
    {
        if (_symbolInfoPanel != null)
        {
            _symbolInfoPanel.Hide();
        }
    }
}