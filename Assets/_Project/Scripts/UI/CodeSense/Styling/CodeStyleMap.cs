using UnityEngine;

[System.Serializable]
public sealed class CodeStyleMap
{
    [SerializeField] private string _defaultColor = "#D4D4D4";
    [SerializeField] private string _keywordColor = "#C586C0";
    [SerializeField] private string _builtInActionColor = "#4EC9B0";
    [SerializeField] private string _builtInQueryColor = "#569CD6";
    [SerializeField] private string _userFunctionColor = "#DCDCAA";
    [SerializeField] private string _booleanLiteralColor = "#569CD6";
    [SerializeField] private string _operatorColor = "#C586C0";
    [SerializeField] private string _punctuationColor = "#D4D4D4";
    [SerializeField] private string _lockedSymbolColor = "#D16969";
    [SerializeField] private string _unknownSymbolColor = "#F44747";

    public string GetColor(CodeStyleKind styleKind)
    {
        switch (styleKind)
        {
            case CodeStyleKind.Keyword:
                return _keywordColor;

            case CodeStyleKind.BuiltInAction:
                return _builtInActionColor;

            case CodeStyleKind.BuiltInQuery:
                return _builtInQueryColor;

            case CodeStyleKind.UserFunction:
                return _userFunctionColor;

            case CodeStyleKind.BooleanLiteral:
                return _booleanLiteralColor;

            case CodeStyleKind.Operator:
                return _operatorColor;

            case CodeStyleKind.Punctuation:
                return _punctuationColor;

            case CodeStyleKind.LockedSymbol:
                return _lockedSymbolColor;

            case CodeStyleKind.UnknownSymbol:
                return _unknownSymbolColor;

            default:
                return _defaultColor;
        }
    }
}