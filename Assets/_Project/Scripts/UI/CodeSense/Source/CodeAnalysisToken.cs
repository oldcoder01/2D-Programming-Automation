public sealed class CodeAnalysisToken
{
    public string Text;
    public CodeTextSpan Span;
    public CodeSymbolKind Kind;
    public bool IsCallSite;
    public bool IsDefinitionName;
    public bool IsResolved;
    public bool IsLocked;
}