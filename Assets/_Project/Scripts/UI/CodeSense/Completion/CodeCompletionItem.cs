public sealed class CodeCompletionItem
{
    public string Label;
    public string InsertText;
    public string Detail;
    public CodeSymbolKind Kind;
    public bool IsLocked;
    public int SortGroup;
}