using System.Collections.Generic;

public sealed class CodeCompletionResult
{
    public readonly List<CodeCompletionItem> Items = new List<CodeCompletionItem>();

    public string Prefix;
    public int ReplaceStartIndex;
    public int ReplaceLength;

    public bool HasItems
    {
        get { return Items.Count > 0; }
    }
}