using System.Collections.Generic;

public sealed class CodeCompletionEngine
{
    public CodeCompletionRequest BuildRequest(string source, int caretPosition)
    {
        CodeCompletionRequest request = new CodeCompletionRequest();

        if (source == null)
        {
            source = string.Empty;
        }

        if (caretPosition < 0)
        {
            caretPosition = 0;
        }

        if (caretPosition > source.Length)
        {
            caretPosition = source.Length;
        }

        int start = caretPosition;

        while (start > 0)
        {
            char c = source[start - 1];
            if (!IsIdentifierPart(c))
            {
                break;
            }

            start--;
        }

        int length = caretPosition - start;
        string prefix = source.Substring(start, length);

        request.Source = source;
        request.CaretPosition = caretPosition;
        request.Prefix = prefix;
        request.PrefixStartIndex = start;
        request.PrefixLength = length;

        return request;
    }

    public CodeCompletionResult GetCompletions(CodeCompletionRequest request, CodeSenseRegistry registry)
    {
        CodeCompletionResult result = new CodeCompletionResult();

        if (request == null)
        {
            return result;
        }

        result.Prefix = request.Prefix;
        result.ReplaceStartIndex = request.PrefixStartIndex;
        result.ReplaceLength = request.PrefixLength;

        if (registry == null)
        {
            return result;
        }

        if (!ShouldOfferCompletions(request))
        {
            return result;
        }

        string prefix = request.Prefix;
        if (string.IsNullOrEmpty(prefix))
        {
            return result;
        }

        HashSet<string> added = new HashSet<string>();

        foreach (CodeSymbolDefinition definition in registry.Definitions)
        {
            if (definition == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(definition.Name))
            {
                continue;
            }

            if (!StartsWithIgnoreCase(definition.Name, prefix))
            {
                continue;
            }

            if (added.Contains(definition.Name))
            {
                continue;
            }

            CodeCompletionItem item = new CodeCompletionItem();
            item.Label = definition.Name;
            item.InsertText = BuildInsertText(definition);
            item.Detail = BuildDetail(definition);
            item.Kind = definition.Kind;
            item.IsLocked = definition.IsLocked;
            item.SortGroup = GetSortGroup(definition.Kind, definition.IsLocked);

            result.Items.Add(item);
            added.Add(definition.Name);
        }

        result.Items.Sort(CompareItems);
        return result;
    }

    private bool ShouldOfferCompletions(CodeCompletionRequest request)
    {
        if (request == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(request.Prefix))
        {
            return false;
        }

        string source = request.Source;
        int caretPosition = request.CaretPosition;

        if (source == null)
        {
            return false;
        }

        if (caretPosition < source.Length)
        {
            char next = source[caretPosition];

            if (IsIdentifierPart(next))
            {
                return false;
            }
        }

        if (request.PrefixStartIndex > 0)
        {
            char previous = source[request.PrefixStartIndex - 1];

            if (previous == '.')
            {
                return false;
            }
        }

        return true;
    }

    private string BuildInsertText(CodeSymbolDefinition definition)
    {
        if (definition == null)
        {
            return string.Empty;
        }

        if (definition.IsCallable)
        {
            return definition.Name + "()";
        }

        return definition.Name;
    }

    private int CompareItems(CodeCompletionItem left, CodeCompletionItem right)
    {
        if (left.SortGroup != right.SortGroup)
        {
            return left.SortGroup.CompareTo(right.SortGroup);
        }

        return string.Compare(left.Label, right.Label, System.StringComparison.OrdinalIgnoreCase);
    }

    private int GetSortGroup(CodeSymbolKind kind, bool isLocked)
    {
        if (isLocked)
        {
            return 4;
        }

        switch (kind)
        {
            case CodeSymbolKind.BuiltInAction:
                return 0;

            case CodeSymbolKind.BuiltInQuery:
                return 1;

            case CodeSymbolKind.UserFunction:
                return 2;

            case CodeSymbolKind.Keyword:
                return 3;

            default:
                return 5;
        }
    }

    private string BuildDetail(CodeSymbolDefinition definition)
    {
        if (definition == null)
        {
            return string.Empty;
        }

        string kindLabel = definition.Kind.ToString();

        if (definition.IsLocked && !string.IsNullOrEmpty(definition.RequiredUpgradeId))
        {
            return kindLabel + " - Locked (" + definition.RequiredUpgradeId + ")";
        }

        if (!string.IsNullOrEmpty(definition.Description))
        {
            return kindLabel + " - " + definition.Description;
        }

        return kindLabel;
    }

    private bool StartsWithIgnoreCase(string value, string prefix)
    {
        return value.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase);
    }

    private bool IsIdentifierPart(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }
}