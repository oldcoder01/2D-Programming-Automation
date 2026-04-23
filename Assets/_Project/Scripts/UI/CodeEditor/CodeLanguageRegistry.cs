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

    private static readonly Dictionary<string, string> _keywordDescriptions = new Dictionary<string, string>()
    {
        { "def", "Defines a reusable function." },
        { "if", "Runs a block when the condition is true." },
        { "elif", "Adds another conditional branch." },
        { "else", "Runs when no earlier condition matched." },
        { "while", "Repeats a block while the condition stays true." },
        { "not", "Negates a boolean expression." },
        { "and", "Returns true only if both sides are true." },
        { "or", "Returns true if either side is true." },
        { "true", "Boolean true value." },
        { "false", "Boolean false value." }
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

    public string GetKeywordDescription(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string description;

        if (_keywordDescriptions.TryGetValue(value, out description))
        {
            return description;
        }

        return string.Empty;
    }

    public bool TryGetBuiltInDefinition(string value, out ScriptBuiltInDefinition definition)
    {
        return _builtInRegistry.TryGetDefinition(value, out definition);
    }
}