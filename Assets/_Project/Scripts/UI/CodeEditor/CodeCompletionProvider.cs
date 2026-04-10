using System.Collections.Generic;

public sealed class CodeCompletionProvider
{
    private readonly CodeLanguageRegistry _languageRegistry = new CodeLanguageRegistry();

    public List<CodeCompletionItem> GetSuggestions(CodeDocument document, int caretIndex)
    {
        List<CodeCompletionItem> results = new List<CodeCompletionItem>();

        if (document == null)
        {
            return results;
        }

        CodeCompletionContext context = BuildContext(document, caretIndex);

        AddKeywordSuggestions(results, context);
        AddBuiltInSuggestions(results, context);
        AddUserFunctionSuggestions(results, context, document);

        results.Sort((left, right) => CompareCompletionItems(left, right, context));
        return results;
    }

    public CodeCompletionContext BuildContext(CodeDocument document, int caretIndex)
    {
        CodeCompletionContext context = new CodeCompletionContext();
        context.CaretIndex = caretIndex;
        context.ReplaceStartIndex = caretIndex;
        context.ReplaceEndIndex = caretIndex;
        context.Prefix = string.Empty;
        context.LineTextBeforeCaret = string.Empty;

        if (document == null)
        {
            return context;
        }

        if (caretIndex < 0)
        {
            caretIndex = 0;
        }

        if (caretIndex > document.Length)
        {
            caretIndex = document.Length;
        }

        int lineIndex = document.GetLineIndexFromCharacterIndex(caretIndex);
        int lineStartIndex = document.GetLineStartIndex(lineIndex);
        string lineText = document.GetLineText(lineIndex);
        int caretColumn = caretIndex - lineStartIndex;

        if (caretColumn < 0)
        {
            caretColumn = 0;
        }

        if (caretColumn > lineText.Length)
        {
            caretColumn = lineText.Length;
        }

        context.LineTextBeforeCaret = lineText.Substring(0, caretColumn);

        int prefixStartColumn = caretColumn;

        while (prefixStartColumn > 0 && IsIdentifierCharacter(lineText[prefixStartColumn - 1]))
        {
            prefixStartColumn--;
        }

        context.Prefix = lineText.Substring(prefixStartColumn, caretColumn - prefixStartColumn);
        context.ReplaceStartIndex = lineStartIndex + prefixStartColumn;
        context.ReplaceEndIndex = caretIndex;

        string trimmedBeforeCaret = context.LineTextBeforeCaret.TrimStart();
        context.IsDefinitionNameContext = trimmedBeforeCaret == "def " || trimmedBeforeCaret.StartsWith("def ");
        context.IsExpressionContext = LooksLikeExpressionContext(context.LineTextBeforeCaret);

        return context;
    }

    private void AddKeywordSuggestions(List<CodeCompletionItem> results, CodeCompletionContext context)
    {
        IReadOnlyList<string> keywords = _languageRegistry.GetKeywords();

        for (int i = 0; i < keywords.Count; i++)
        {
            string keyword = keywords[i];

            if (!MatchesPrefix(keyword, context.Prefix))
            {
                continue;
            }

            CodeCompletionItem item = new CodeCompletionItem();
            item.Label = keyword;
            item.InsertText = keyword;
            item.Description = GetKeywordDescription(keyword);
            item.Kind = CodeSymbolKind.Keyword;
            item.SortScore = GetKeywordSortScore(keyword, context);
            item.SortScore += GetPrefixBonus(keyword, context.Prefix);
            results.Add(item);
        }
    }

    private void AddBuiltInSuggestions(List<CodeCompletionItem> results, CodeCompletionContext context)
    {
        if (context.IsDefinitionNameContext)
        {
            return;
        }

        IReadOnlyCollection<ScriptBuiltInDefinition> builtIns = _languageRegistry.GetBuiltIns();

        foreach (ScriptBuiltInDefinition definition in builtIns)
        {
            string baseName = definition.Name;

            if (!MatchesPrefix(baseName, context.Prefix))
            {
                continue;
            }

            CodeCompletionItem item = new CodeCompletionItem();
            item.Label = baseName;
            item.InsertText = baseName;
            item.Description = definition.Description;

            if (definition.Kind == ScriptBuiltInKind.Action)
            {
                item.Kind = CodeSymbolKind.Action;
                item.SortScore = context.IsExpressionContext ? 55 : 15;
            }
            else
            {
                item.Kind = CodeSymbolKind.Query;
                item.SortScore = context.IsExpressionContext ? 10 : 50;
            }

            item.SortScore += GetPrefixBonus(baseName, context.Prefix);
            results.Add(item);
        }
    }

    private void AddUserFunctionSuggestions(List<CodeCompletionItem> results, CodeCompletionContext context, CodeDocument document)
    {
        HashSet<string> addedNames = new HashSet<string>();
        List<string> functionNames = ExtractUserFunctionNames(document);

        for (int i = 0; i < functionNames.Count; i++)
        {
            string functionName = functionNames[i];

            if (!MatchesPrefix(functionName, context.Prefix))
            {
                continue;
            }

            if (!addedNames.Add(functionName))
            {
                continue;
            }

            CodeCompletionItem item = new CodeCompletionItem();
            item.Label = functionName;
            item.InsertText = functionName;
            item.Description = "User-defined function";
            item.Kind = CodeSymbolKind.UserFunction;
            item.SortScore = context.IsDefinitionNameContext ? 5 : 12;
            item.SortScore += GetPrefixBonus(functionName, context.Prefix);
            results.Add(item);
        }
    }

    private List<string> ExtractUserFunctionNames(CodeDocument document)
    {
        List<string> results = new List<string>();

        if (document == null)
        {
            return results;
        }

        for (int lineIndex = 0; lineIndex < document.LineCount; lineIndex++)
        {
            string lineText = document.GetLineText(lineIndex);

            if (string.IsNullOrWhiteSpace(lineText))
            {
                continue;
            }

            string trimmedLine = lineText.TrimStart();

            if (!trimmedLine.StartsWith("def "))
            {
                continue;
            }

            int nameStartIndex = 4;
            int openParenIndex = trimmedLine.IndexOf('(', nameStartIndex);

            if (openParenIndex <= nameStartIndex)
            {
                continue;
            }

            string functionName = trimmedLine.Substring(nameStartIndex, openParenIndex - nameStartIndex).Trim();

            if (string.IsNullOrEmpty(functionName))
            {
                continue;
            }

            results.Add(functionName);
        }

        return results;
    }

    private static bool LooksLikeExpressionContext(string lineTextBeforeCaret)
    {
        if (string.IsNullOrWhiteSpace(lineTextBeforeCaret))
        {
            return false;
        }

        string trimmed = lineTextBeforeCaret.TrimStart();

        if (trimmed.StartsWith("if "))
        {
            return true;
        }

        if (trimmed.StartsWith("elif "))
        {
            return true;
        }

        if (trimmed.StartsWith("while "))
        {
            return true;
        }

        if (trimmed.EndsWith(" not"))
        {
            return true;
        }

        if (trimmed.EndsWith(" and"))
        {
            return true;
        }

        if (trimmed.EndsWith(" or"))
        {
            return true;
        }

        return false;
    }

    private static bool MatchesPrefix(string candidate, string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return true;
        }

        if (string.IsNullOrEmpty(candidate))
        {
            return false;
        }

        if (prefix.Length > candidate.Length)
        {
            return false;
        }

        for (int i = 0; i < prefix.Length; i++)
        {
            char prefixCharacter = char.ToLowerInvariant(prefix[i]);
            char candidateCharacter = char.ToLowerInvariant(candidate[i]);

            if (prefixCharacter != candidateCharacter)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsIdentifierCharacter(char character)
    {
        if (character >= 'a' && character <= 'z')
        {
            return true;
        }

        if (character >= 'A' && character <= 'Z')
        {
            return true;
        }

        if (character >= '0' && character <= '9')
        {
            return true;
        }

        return character == '_';
    }

    private static int CompareCompletionItems(CodeCompletionItem left, CodeCompletionItem right, CodeCompletionContext context)
    {
        int scoreComparison = left.SortScore.CompareTo(right.SortScore);

        if (scoreComparison != 0)
        {
            return scoreComparison;
        }

        string leftName = left.InsertText;
        string rightName = right.InsertText;

        int exactComparison = GetExactMatchRank(leftName, context.Prefix).CompareTo(GetExactMatchRank(rightName, context.Prefix));

        if (exactComparison != 0)
        {
            return exactComparison;
        }

        int lengthComparison = leftName.Length.CompareTo(rightName.Length);

        if (lengthComparison != 0)
        {
            return lengthComparison;
        }

        return string.CompareOrdinal(left.Label, right.Label);
    }

    private static int GetKeywordSortScore(string keyword, CodeCompletionContext context)
    {
        if (context.IsDefinitionNameContext)
        {
            if (keyword == "def")
            {
                return 0;
            }

            return 100;
        }

        if (context.IsExpressionContext)
        {
            if (keyword == "not" || keyword == "and" || keyword == "or" || keyword == "true" || keyword == "false")
            {
                return 15;
            }

            return 70;
        }

        if (keyword == "def" || keyword == "if" || keyword == "while" || keyword == "else" || keyword == "elif")
        {
            return 25;
        }

        return 80;
    }

    private static int GetPrefixBonus(string candidate, string prefix)
    {
        if (string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(candidate))
        {
            return 0;
        }

        if (StringsEqualIgnoreCase(candidate, prefix))
        {
            return -8;
        }

        if (candidate.Length == prefix.Length + 1)
        {
            return -4;
        }

        if (candidate.Length <= prefix.Length + 3)
        {
            return -2;
        }

        return 0;
    }

    private static int GetExactMatchRank(string candidate, string prefix)
    {
        if (StringsEqualIgnoreCase(candidate, prefix))
        {
            return 0;
        }

        return 1;
    }

    private static bool StringsEqualIgnoreCase(string left, string right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        if (left.Length != right.Length)
        {
            return false;
        }

        for (int i = 0; i < left.Length; i++)
        {
            if (char.ToLowerInvariant(left[i]) != char.ToLowerInvariant(right[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static string GetKeywordDescription(string keyword)
    {
        switch (keyword)
        {
            case "def":
                return "Defines a reusable function.";
            case "if":
                return "Runs a block when the condition is true.";
            case "elif":
                return "Adds another conditional branch.";
            case "else":
                return "Runs when no earlier condition matched.";
            case "while":
                return "Repeats a block while the condition stays true.";
            case "not":
                return "Negates a boolean expression.";
            case "and":
                return "Returns true only if both sides are true.";
            case "or":
                return "Returns true if either side is true.";
            case "true":
                return "Boolean true value.";
            case "false":
                return "Boolean false value.";
        }

        return string.Empty;
    }
}
