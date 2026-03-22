using System;
using System.Collections.Generic;

public sealed class ScriptLexer
{
    public List<ScriptToken> Tokenize(string source)
    {
        List<ScriptToken> tokens = new List<ScriptToken>();

        if (string.IsNullOrWhiteSpace(source))
        {
            tokens.Add(new ScriptToken(ScriptTokenType.EndOfFile, string.Empty, 1));
            return tokens;
        }

        string normalized = source.Replace("\r\n", "\n").Replace("\r", "\n");
        string[] lines = normalized.Split('\n');

        Stack<int> indentStack = new Stack<int>();
        indentStack.Push(0);

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string rawLine = lines[lineIndex];
            int lineNumber = lineIndex + 1;

            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            int leadingSpaces = CountLeadingSpaces(rawLine);
            if (leadingSpaces % 4 != 0)
            {
                throw new Exception("Line " + lineNumber + ": Indentation must use multiples of 4 spaces.");
            }

            int indentLevel = leadingSpaces / 4;
            int currentIndent = indentStack.Peek();

            if (indentLevel > currentIndent)
            {
                if (indentLevel != currentIndent + 1)
                {
                    throw new Exception("Line " + lineNumber + ": Indentation jumped too far.");
                }

                indentStack.Push(indentLevel);
                tokens.Add(new ScriptToken(ScriptTokenType.Indent, "<INDENT>", lineNumber));
            }
            else if (indentLevel < currentIndent)
            {
                while (indentStack.Count > 0 && indentStack.Peek() > indentLevel)
                {
                    indentStack.Pop();
                    tokens.Add(new ScriptToken(ScriptTokenType.Dedent, "<DEDENT>", lineNumber));
                }

                if (indentStack.Count == 0 || indentStack.Peek() != indentLevel)
                {
                    throw new Exception("Line " + lineNumber + ": Invalid dedent.");
                }
            }

            string line = rawLine.Trim();
            int index = 0;

            while (index < line.Length)
            {
                char current = line[index];

                if (current == ' ' || current == '\t')
                {
                    index++;
                    continue;
                }

                if (current == '(')
                {
                    tokens.Add(new ScriptToken(ScriptTokenType.LeftParen, "(", lineNumber));
                    index++;
                    continue;
                }

                if (current == ')')
                {
                    tokens.Add(new ScriptToken(ScriptTokenType.RightParen, ")", lineNumber));
                    index++;
                    continue;
                }

                if (current == ':')
                {
                    tokens.Add(new ScriptToken(ScriptTokenType.Colon, ":", lineNumber));
                    index++;
                    continue;
                }

                if (IsIdentifierStart(current))
                {
                    int start = index;
                    index++;

                    while (index < line.Length && IsIdentifierPart(line[index]))
                    {
                        index++;
                    }

                    string word = line.Substring(start, index - start);
                    tokens.Add(new ScriptToken(GetKeywordOrIdentifier(word), word, lineNumber));
                    continue;
                }

                throw new Exception("Line " + lineNumber + ": Unexpected character '" + current + "'.");
            }

            tokens.Add(new ScriptToken(ScriptTokenType.NewLine, "<NEWLINE>", lineNumber));
        }

        while (indentStack.Count > 1)
        {
            indentStack.Pop();
            tokens.Add(new ScriptToken(ScriptTokenType.Dedent, "<DEDENT>", lines.Length));
        }

        tokens.Add(new ScriptToken(ScriptTokenType.EndOfFile, string.Empty, lines.Length + 1));
        return tokens;
    }

    private int CountLeadingSpaces(string line)
    {
        int count = 0;

        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] != ' ')
                break;

            count++;
        }

        return count;
    }

    private bool IsIdentifierStart(char value)
    {
        return char.IsLetter(value) || value == '_';
    }

    private bool IsIdentifierPart(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }

    private ScriptTokenType GetKeywordOrIdentifier(string word)
    {
        switch (word)
        {
            case "if":
                return ScriptTokenType.KeywordIf;
            case "elif":
                return ScriptTokenType.KeywordElif;
            case "else":
                return ScriptTokenType.KeywordElse;
            case "while":
                return ScriptTokenType.KeywordWhile;
            case "def":
                return ScriptTokenType.KeywordDef;
            case "true":
            case "True":
                return ScriptTokenType.KeywordTrue;
            case "false":
            case "False":
                return ScriptTokenType.KeywordFalse;
            case "and":
                return ScriptTokenType.KeywordAnd;
            case "or":
                return ScriptTokenType.KeywordOr;
            case "not":
                return ScriptTokenType.KeywordNot;
            default:
                return ScriptTokenType.Identifier;
        }
    }
}