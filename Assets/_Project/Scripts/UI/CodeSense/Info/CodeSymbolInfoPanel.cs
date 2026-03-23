using TMPro;
using UnityEngine;

public sealed class CodeSymbolInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _bodyText;

    public void Show(CodeSymbolLookupResult lookupResult)
    {
        if (lookupResult == null)
        {
            Hide();
            return;
        }

        if (_panelRoot != null)
        {
            _panelRoot.SetActive(true);
        }

        if (_titleText != null)
        {
            _titleText.text = BuildTitle(lookupResult);
        }

        if (_bodyText != null)
        {
            _bodyText.text = BuildBody(lookupResult);
        }
    }

    public void Hide()
    {
        if (_panelRoot != null)
        {
            _panelRoot.SetActive(false);
        }
    }

    private string BuildTitle(CodeSymbolLookupResult lookupResult)
    {
        if (lookupResult == null || string.IsNullOrEmpty(lookupResult.SymbolText))
        {
            return "Symbol";
        }

        return lookupResult.SymbolText;
    }

    private string BuildBody(CodeSymbolLookupResult lookupResult)
    {
        if (lookupResult == null)
        {
            return string.Empty;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        builder.Append("Kind: ");
        builder.Append(GetKindLabel(lookupResult.Kind));
        builder.Append('\n');

        if (lookupResult.IsDefinitionName)
        {
            builder.Append("Usage: Function Definition");
            builder.Append('\n');
        }
        else if (lookupResult.IsCallSite)
        {
            builder.Append("Usage: Function Call");
            builder.Append('\n');
        }

        builder.Append("State: ");
        builder.Append(lookupResult.IsLocked ? "Locked" : "Available");
        builder.Append('\n');

        if (lookupResult.Definition != null && !string.IsNullOrEmpty(lookupResult.Definition.Description))
        {
            builder.Append('\n');
            builder.Append(lookupResult.Definition.Description);
        }
        else if (!lookupResult.IsResolved)
        {
            builder.Append('\n');
            builder.Append("Unknown symbol.");
        }

        if (lookupResult.Definition != null && lookupResult.Definition.IsLocked && !string.IsNullOrEmpty(lookupResult.Definition.RequiredUpgradeId))
        {
            builder.Append('\n');
            builder.Append("Requires: ");
            builder.Append(lookupResult.Definition.RequiredUpgradeId);
        }

        return builder.ToString().TrimEnd();
    }

    private string GetKindLabel(CodeSymbolKind kind)
    {
        switch (kind)
        {
            case CodeSymbolKind.BuiltInAction:
                return "Built-in Action";

            case CodeSymbolKind.BuiltInQuery:
                return "Built-in Query";

            case CodeSymbolKind.UserFunction:
                return "User Function";

            case CodeSymbolKind.Keyword:
                return "Keyword";

            case CodeSymbolKind.BooleanLiteral:
                return "Boolean Literal";

            case CodeSymbolKind.Operator:
                return "Operator";

            case CodeSymbolKind.Unknown:
                return "Unknown";

            default:
                return kind.ToString();
        }
    }
}