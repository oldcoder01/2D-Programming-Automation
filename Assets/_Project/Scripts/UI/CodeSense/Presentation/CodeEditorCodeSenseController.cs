using TMPro;
using UnityEngine;

public sealed class CodeEditorCodeSenseController : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private CodeHighlightOverlay _highlightOverlay;
    [SerializeField] private CodeStyleMap _styleMap = new CodeStyleMap();

    private CodeSourceAnalyzer _sourceAnalyzer;
    private CodeStyleResolver _styleResolver;
    private CodeHighlightRichTextBuilder _richTextBuilder;

    private string _lastSource;
    private ScriptBuiltInRegistry _builtInRegistry;
    private CodeAnalysisResult _latestAnalysisResult;
    private CodeStyleResult _latestStyleResult;

    public CodeAnalysisResult LatestAnalysisResult
    {
        get { return _latestAnalysisResult; }
    }

    public CodeStyleResult LatestStyleResult
    {
        get { return _latestStyleResult; }
    }

    private void Awake()
    {
        if (_inputField == null)
        {
            _inputField = GetComponentInChildren<TMP_InputField>();
        }

        _sourceAnalyzer = new CodeSourceAnalyzer();
        _styleResolver = new CodeStyleResolver();
        _richTextBuilder = new CodeHighlightRichTextBuilder();
        _builtInRegistry = new ScriptBuiltInRegistry();
    }

    private void OnEnable()
    {
        if (_inputField != null)
        {
            _inputField.onValueChanged.AddListener(HandleValueChanged);
        }

        RebuildNow();
    }

    private void OnDisable()
    {
        if (_inputField != null)
        {
            _inputField.onValueChanged.RemoveListener(HandleValueChanged);
        }
    }

    [ContextMenu("Rebuild Code Sense Now")]
    public void RebuildNow()
    {
        string source = GetCurrentSource();
        Rebuild(source);
    }

    private void HandleValueChanged(string newValue)
    {
        Rebuild(newValue);
    }

    private void Rebuild(string source)
    {
        if (_highlightOverlay == null)
        {
            return;
        }

        if (source == null)
        {
            source = string.Empty;
        }

        if (_lastSource == source)
        {
            return;
        }

        _lastSource = source;

        _latestAnalysisResult = _sourceAnalyzer.Analyze(source, _builtInRegistry);
        _latestStyleResult = _styleResolver.Resolve(source, _latestAnalysisResult);

        string richText = _richTextBuilder.Build(source, _latestStyleResult, _styleMap);
        _highlightOverlay.SetHighlightedText(richText);
    }

    private string GetCurrentSource()
    {
        if (_inputField == null)
        {
            return string.Empty;
        }

        return _inputField.text;
    }
}