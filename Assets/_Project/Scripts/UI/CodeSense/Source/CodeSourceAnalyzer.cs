using System.Collections.Generic;

public sealed class CodeSourceAnalyzer
{
    private static readonly HashSet<string> Keywords = new HashSet<string>
    {
        "if",
        "elif",
        "else",
        "while",
        "def"
    };

    private static readonly HashSet<string> BooleanLiterals = new HashSet<string>
    {
        "true",
        "false"
    };

    private static readonly HashSet<string> Operators = new HashSet<string>
    {
        "and",
        "or",
        "not"
    };

    public CodeAnalysisResult Analyze(string source, ScriptBuiltInRegistry builtInRegistry)
    {
        CodeAnalysisResult result = new CodeAnalysisResult();

        if (string.IsNullOrEmpty(source))
        {
            return result;
        }

        string normalized = source.Replace("\r\n", "\n").Replace("\r", "\n");

        ScanTokens(normalized, result);
        DiscoverFunctionDefinitions(result);
        ClassifyIdentifiers(result, builtInRegistry);

        return result;
    }

    private void ScanTokens(string source, CodeAnalysisResult result)
    {
        int index = 0;
        int line = 1;
        int column = 1;

        while (index < source.Length)
        {
            char current = source[index];

            if (current == '\n')
            {
                index++;
                line++;
                column = 1;
                continue;
            }

            if (current == ' ' || current == '\t')
            {
                index++;
                column++;
                continue;
            }

            if (current == '(' || current == ')' || current == ':')
            {
                CodeAnalysisToken punctuationToken = new CodeAnalysisToken();
                punctuationToken.Text = current.ToString();
                punctuationToken.Span = new CodeTextSpan(index, 1, line, column);
                punctuationToken.Kind = CodeSymbolKind.Punctuation;
                punctuationToken.IsResolved = true;
                result.Tokens.Add(punctuationToken);

                index++;
                column++;
                continue;
            }

            if (IsIdentifierStart(current))
            {
                int startIndex = index;
                int startColumn = column;

                index++;
                column++;

                while (index < source.Length && IsIdentifierPart(source[index]))
                {
                    index++;
                    column++;
                }

                string word = source.Substring(startIndex, index - startIndex);

                CodeAnalysisToken token = new CodeAnalysisToken();
                token.Text = word;
                token.Span = new CodeTextSpan(startIndex, word.Length, line, startColumn);
                token.Kind = GetInitialKind(word);
                token.IsResolved = token.Kind != CodeSymbolKind.Unknown;

                result.Tokens.Add(token);
                continue;
            }

            index++;
            column++;
        }
    }

    private void DiscoverFunctionDefinitions(CodeAnalysisResult result)
    {
        for (int i = 0; i < result.Tokens.Count - 4; i++)
        {
            CodeAnalysisToken token = result.Tokens[i];
            if (token.Text != "def")
            {
                continue;
            }

            CodeAnalysisToken nameToken = result.Tokens[i + 1];
            CodeAnalysisToken leftParenToken = result.Tokens[i + 2];
            CodeAnalysisToken rightParenToken = result.Tokens[i + 3];
            CodeAnalysisToken colonToken = result.Tokens[i + 4];

            if (nameToken.Kind != CodeSymbolKind.Unknown && nameToken.Kind != CodeSymbolKind.Punctuation)
            {
                continue;
            }

            if (leftParenToken.Text != "(" || rightParenToken.Text != ")" || colonToken.Text != ":")
            {
                continue;
            }

            nameToken.Kind = CodeSymbolKind.UserFunction;
            nameToken.IsDefinitionName = true;
            nameToken.IsResolved = true;

            CodeFunctionDefinition functionDefinition = new CodeFunctionDefinition();
            functionDefinition.Name = nameToken.Text;
            functionDefinition.NameSpan = nameToken.Span;
            functionDefinition.Line = nameToken.Span.Line;

            result.FunctionDefinitions.Add(functionDefinition);
        }
    }

    private void ClassifyIdentifiers(CodeAnalysisResult result, ScriptBuiltInRegistry builtInRegistry)
    {
        for (int i = 0; i < result.Tokens.Count; i++)
        {
            CodeAnalysisToken token = result.Tokens[i];

            if (!IsIdentifierLike(token))
            {
                continue;
            }

            if (token.IsDefinitionName)
            {
                continue;
            }

            bool isCallSite = IsCallSite(result.Tokens, i);
            token.IsCallSite = isCallSite;

            if (TryResolveBuiltIn(token, builtInRegistry))
            {
                if (isCallSite)
                {
                    AddCallSite(result, token);
                }

                continue;
            }

            if (TryResolveUserFunction(token, result))
            {
                if (isCallSite)
                {
                    AddCallSite(result, token);
                }

                continue;
            }

            token.Kind = CodeSymbolKind.Unknown;
            token.IsResolved = false;
        }
    }

    private bool TryResolveBuiltIn(CodeAnalysisToken token, ScriptBuiltInRegistry builtInRegistry)
    {
        if (builtInRegistry == null)
        {
            return false;
        }

        ScriptBuiltInDefinition builtInDefinition;
        if (!builtInRegistry.TryGetDefinition(token.Text, out builtInDefinition))
        {
            return false;
        }

        if (builtInDefinition.Kind == ScriptBuiltInKind.Action)
        {
            token.Kind = CodeSymbolKind.BuiltInAction;
        }
        else
        {
            token.Kind = CodeSymbolKind.BuiltInQuery;
        }

        token.IsLocked = !builtInDefinition.UnlockedByDefault;
        token.IsResolved = true;
        return true;
    }

    private bool TryResolveUserFunction(CodeAnalysisToken token, CodeAnalysisResult result)
    {
        CodeFunctionDefinition definition;
        if (!result.TryGetFunctionDefinition(token.Text, out definition))
        {
            return false;
        }

        token.Kind = CodeSymbolKind.UserFunction;
        token.IsResolved = true;
        return true;
    }

    private void AddCallSite(CodeAnalysisResult result, CodeAnalysisToken token)
    {
        CodeCallSite callSite = new CodeCallSite();
        callSite.Name = token.Text;
        callSite.NameSpan = token.Span;
        callSite.Line = token.Span.Line;
        result.CallSites.Add(callSite);
    }

    private bool IsCallSite(List<CodeAnalysisToken> tokens, int tokenIndex)
    {
        if (tokenIndex + 2 >= tokens.Count)
        {
            return false;
        }

        CodeAnalysisToken nextToken = tokens[tokenIndex + 1];
        CodeAnalysisToken afterNextToken = tokens[tokenIndex + 2];

        return nextToken.Text == "(" && afterNextToken.Text == ")";
    }

    private bool IsIdentifierLike(CodeAnalysisToken token)
    {
        if (token == null)
        {
            return false;
        }

        if (token.Kind == CodeSymbolKind.Keyword ||
            token.Kind == CodeSymbolKind.BooleanLiteral ||
            token.Kind == CodeSymbolKind.Operator ||
            token.Kind == CodeSymbolKind.Punctuation)
        {
            return false;
        }

        return true;
    }

    private CodeSymbolKind GetInitialKind(string word)
    {
        if (Keywords.Contains(word))
        {
            return CodeSymbolKind.Keyword;
        }

        if (BooleanLiterals.Contains(word))
        {
            return CodeSymbolKind.BooleanLiteral;
        }

        if (Operators.Contains(word))
        {
            return CodeSymbolKind.Operator;
        }

        return CodeSymbolKind.Unknown;
    }

    private bool IsIdentifierStart(char value)
    {
        return char.IsLetter(value) || value == '_';
    }

    private bool IsIdentifierPart(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }
}