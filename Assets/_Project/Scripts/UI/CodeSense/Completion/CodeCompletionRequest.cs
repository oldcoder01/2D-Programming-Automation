public sealed class CodeCompletionRequest
{
    public string Source;
    public int CaretPosition;
    public string Prefix;
    public int PrefixStartIndex;
    public int PrefixLength;
}