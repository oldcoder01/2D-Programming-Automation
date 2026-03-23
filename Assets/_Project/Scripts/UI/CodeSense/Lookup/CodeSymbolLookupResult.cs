public sealed class CodeSymbolLookupResult
{
    public string SymbolText;
    public CodeTextSpan Span;
    public CodeSymbolKind Kind;
    public bool IsResolved;
    public bool IsLocked;
    public bool IsCallSite;
    public bool IsDefinitionName;
    public CodeSymbolDefinition Definition;
}