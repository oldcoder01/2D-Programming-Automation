using TMPro;
using UnityEngine;

public sealed class CodeEditorInputVisualHelper : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] [Range(0f, 1f)] private float _textAlpha = 0.05f;

    private void Awake()
    {
        if (_inputField == null)
        {
            _inputField = GetComponentInChildren<TMP_InputField>();
        }

        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    [ContextMenu("Apply Input Visual Settings")]
    public void Apply()
    {
        if (_inputField == null || _inputField.textComponent == null)
        {
            return;
        }

        Color color = _inputField.textComponent.color;
        color.a = _textAlpha;
        _inputField.textComponent.color = color;
    }
}