public sealed class ScriptToken
{
    public ScriptTokenType Type;
    public string Lexeme;
    public int LineNumber;

    public ScriptToken(ScriptTokenType type, string lexeme, int lineNumber)
    {
        Type = type;
        Lexeme = lexeme;
        LineNumber = lineNumber;
    }
}