public sealed class CodeStyleResolver
{
    public CodeStyleResult Resolve(string source, CodeAnalysisResult analysisResult)
    {
        CodeStyleResult result = new CodeStyleResult();

        if (string.IsNullOrEmpty(source))
        {
            return result;
        }

        if (analysisResult == null)
        {
            result.Spans.Add(new CodeStyleSpan(0, source.Length, CodeStyleKind.Default));
            return result;
        }

        int currentIndex = 0;

        for (int i = 0; i < analysisResult.Tokens.Count; i++)
        {
            CodeAnalysisToken token = analysisResult.Tokens[i];
            if (token == null)
            {
                continue;
            }

            int tokenStart = token.Span.StartIndex;
            int tokenLength = token.Span.Length;

            if (tokenLength <= 0)
            {
                continue;
            }

            if (tokenStart > currentIndex)
            {
                result.Spans.Add(new CodeStyleSpan(currentIndex, tokenStart - currentIndex, CodeStyleKind.Default));
            }

            result.Spans.Add(new CodeStyleSpan(tokenStart, tokenLength, ResolveStyleKind(token)));
            currentIndex = tokenStart + tokenLength;
        }

        if (currentIndex < source.Length)
        {
            result.Spans.Add(new CodeStyleSpan(currentIndex, source.Length - currentIndex, CodeStyleKind.Default));
        }

        return result;
    }

    private CodeStyleKind ResolveStyleKind(CodeAnalysisToken token)
    {
        if (token.IsLocked)
        {
            return CodeStyleKind.LockedSymbol;
        }

        switch (token.Kind)
        {
            case CodeSymbolKind.Keyword:
                return CodeStyleKind.Keyword;

            case CodeSymbolKind.BuiltInAction:
                return CodeStyleKind.BuiltInAction;

            case CodeSymbolKind.BuiltInQuery:
                return CodeStyleKind.BuiltInQuery;

            case CodeSymbolKind.UserFunction:
                return CodeStyleKind.UserFunction;

            case CodeSymbolKind.BooleanLiteral:
                return CodeStyleKind.BooleanLiteral;

            case CodeSymbolKind.Operator:
                return CodeStyleKind.Operator;

            case CodeSymbolKind.Punctuation:
                return CodeStyleKind.Punctuation;

            case CodeSymbolKind.Unknown:
                return CodeStyleKind.UnknownSymbol;

            default:
                return CodeStyleKind.Default;
        }
    }
}