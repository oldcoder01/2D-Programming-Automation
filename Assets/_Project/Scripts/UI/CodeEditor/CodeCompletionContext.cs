public sealed class CodeCompletionContext
{
    public int CaretIndex;
    public int ReplaceStartIndex;
    public int ReplaceEndIndex;
    public string Prefix;
    public string LineTextBeforeCaret;
    public bool IsExpressionContext;
    public bool IsDefinitionNameContext;
}