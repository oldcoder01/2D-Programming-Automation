public sealed class CodeSymbolLocator
{
    public CodeSymbolLookupResult FindSymbolAtSourceIndex(CodeAnalysisResult analysisResult, CodeSenseRegistry registry, int sourceIndex)
    {
        if (analysisResult == null)
        {
            return null;
        }

        CodeAnalysisToken bestToken = null;

        for (int i = 0; i < analysisResult.Tokens.Count; i++)
        {
            CodeAnalysisToken token = analysisResult.Tokens[i];
            if (token == null)
            {
                continue;
            }

            int start = token.Span.StartIndex;
            int endExclusive = token.Span.StartIndex + token.Span.Length;

            if (sourceIndex < start || sourceIndex >= endExclusive)
            {
                continue;
            }

            if (!ShouldShowToken(token))
            {
                return null;
            }

            bestToken = token;
            break;
        }

        if (bestToken == null)
        {
            return null;
        }

        CodeSymbolLookupResult result = new CodeSymbolLookupResult();
        result.SymbolText = bestToken.Text;
        result.Span = bestToken.Span;
        result.Kind = bestToken.Kind;
        result.IsResolved = bestToken.IsResolved;
        result.IsLocked = bestToken.IsLocked;
        result.IsCallSite = bestToken.IsCallSite;
        result.IsDefinitionName = bestToken.IsDefinitionName;

        if (registry != null)
        {
            CodeSymbolDefinition definition;
            if (registry.TryGetDefinition(bestToken.Text, out definition))
            {
                result.Definition = definition;
            }
        }

        return result;
    }

    private bool ShouldShowToken(CodeAnalysisToken token)
    {
        if (token == null)
        {
            return false;
        }

        if (token.Kind == CodeSymbolKind.Punctuation)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(token.Text))
        {
            return false;
        }

        return true;
    }
}