using System.Collections.Generic;

public sealed class CodeLanguageRegistry
{
    private static readonly string[] _keywords =
    {
        "def",
        "if",
        "elif",
        "else",
        "while",
        "not",
        "and",
        "or",
        "true",
        "false"
    };

    private readonly ScriptBuiltInRegistry _builtInRegistry = new ScriptBuiltInRegistry();

    public IReadOnlyList<string> GetKeywords()
    {
        return _keywords;
    }

    public IReadOnlyCollection<ScriptBuiltInDefinition> GetBuiltIns()
    {
        return _builtInRegistry.GetDefinitions();
    }

    public bool IsKeyword(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        for (int i = 0; i < _keywords.Length; i++)
        {
            if (_keywords[i] == value)
            {
                return true;
            }
        }

        return false;
    }
}