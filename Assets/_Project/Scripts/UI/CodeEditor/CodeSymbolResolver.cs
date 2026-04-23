using System.Collections.Generic;

public sealed class CodeSymbolResolver
{
    private readonly CodeLanguageRegistry _languageRegistry = new CodeLanguageRegistry();

    public CodeSymbolInfo ResolveByName(CodeDocument document, string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            return null;
        }

        if (_languageRegistry.IsKeyword(identifier))
        {
            CodeSymbolInfo keywordInfo = new CodeSymbolInfo();
            keywordInfo.Name = identifier;
            keywordInfo.Title = "Keyword: " + identifier;
            keywordInfo.Description = _languageRegistry.GetKeywordDescription(identifier);
            keywordInfo.Summary = _languageRegistry.GetKeywordDescription(identifier);
            keywordInfo.Signature = identifier;
            keywordInfo.Category = "Keyword";
            keywordInfo.Kind = CodeSymbolKind.Keyword;
            return keywordInfo;
        }

        FunctionDefinitionInfo functionInfo;

        if (TryGetUserFunctionDefinition(document, identifier, out functionInfo))
        {
            CodeSymbolInfo userFunctionSymbol = new CodeSymbolInfo();
            userFunctionSymbol.Name = functionInfo.Name;
            userFunctionSymbol.Title = "Function: " + functionInfo.Signature;
            userFunctionSymbol.Description = BuildUserFunctionDescription(functionInfo);
            userFunctionSymbol.Summary = "User-defined function in this script.";
            userFunctionSymbol.Signature = functionInfo.Signature;
            userFunctionSymbol.Category = "User Function";
            userFunctionSymbol.Kind = CodeSymbolKind.UserFunction;
            return userFunctionSymbol;
        }

        ScriptBuiltInDefinition builtInDefinition;

        if (_languageRegistry.TryGetBuiltInDefinition(identifier, out builtInDefinition))
        {
            CodeSymbolInfo builtInSymbol = new CodeSymbolInfo();
            builtInSymbol.Name = builtInDefinition.Name;
            builtInSymbol.Title = builtInDefinition.GetDisplayTitle();
            builtInSymbol.Description = builtInDefinition.GetDetailDescription();
            builtInSymbol.Summary = builtInDefinition.GetShortDescription();
            builtInSymbol.Signature = builtInDefinition.Signature;
            builtInSymbol.Category = builtInDefinition.Category;
            builtInSymbol.Kind = builtInDefinition.Kind == ScriptBuiltInKind.Action ? CodeSymbolKind.Action : CodeSymbolKind.Query;
            return builtInSymbol;
        }

        return null;
    }

    public CodeSymbolInfo ResolveAtIndex(CodeDocument document, int index)
    {
        int startIndex;
        int endIndex;
        string identifier;

        if (!TryGetIdentifierAtIndex(document, index, out startIndex, out endIndex, out identifier))
        {
            return null;
        }

        return ResolveByName(document, identifier);
    }

    public List<string> ExtractUserFunctionNames(CodeDocument document)
    {
        List<string> results = new List<string>();
        List<FunctionDefinitionInfo> definitions = ExtractUserFunctionDefinitions(document);

        for (int i = 0; i < definitions.Count; i++)
        {
            results.Add(definitions[i].Name);
        }

        return results;
    }

    public bool TryGetIdentifierAtIndex(CodeDocument document, int index, out int startIndex, out int endIndex, out string identifier)
    {
        startIndex = 0;
        endIndex = 0;
        identifier = string.Empty;

        if (document == null || document.Length <= 0)
        {
            return false;
        }

        string text = document.Text;
        int safeIndex = document.ClampIndex(index);

        if (safeIndex >= text.Length)
        {
            safeIndex = text.Length - 1;
        }

        if (safeIndex < 0 || safeIndex >= text.Length)
        {
            return false;
        }

        if (!IsIdentifierCharacter(text[safeIndex]))
        {
            if (safeIndex > 0 && IsIdentifierCharacter(text[safeIndex - 1]))
            {
                safeIndex--;
            }
            else
            {
                return false;
            }
        }

        startIndex = safeIndex;
        endIndex = safeIndex + 1;

        while (startIndex > 0 && IsIdentifierCharacter(text[startIndex - 1]))
        {
            startIndex--;
        }

        while (endIndex < text.Length && IsIdentifierCharacter(text[endIndex]))
        {
            endIndex++;
        }

        identifier = text.Substring(startIndex, endIndex - startIndex);
        return !string.IsNullOrEmpty(identifier);
    }

    public bool TryGetUserFunctionDefinition(CodeDocument document, string functionName, out FunctionDefinitionInfo result)
    {
        List<FunctionDefinitionInfo> definitions = ExtractUserFunctionDefinitions(document);

        for (int i = 0; i < definitions.Count; i++)
        {
            if (definitions[i].Name == functionName)
            {
                result = definitions[i];
                return true;
            }
        }

        result = default(FunctionDefinitionInfo);
        return false;
    }

    private List<FunctionDefinitionInfo> ExtractUserFunctionDefinitions(CodeDocument document)
    {
        List<FunctionDefinitionInfo> results = new List<FunctionDefinitionInfo>();

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

            int closeParenIndex = trimmedLine.IndexOf(')', openParenIndex + 1);

            if (closeParenIndex < openParenIndex)
            {
                closeParenIndex = trimmedLine.Length - 1;
            }

            string functionName = trimmedLine.Substring(nameStartIndex, openParenIndex - nameStartIndex).Trim();

            if (string.IsNullOrEmpty(functionName))
            {
                continue;
            }

            string signature = functionName + trimmedLine.Substring(openParenIndex, closeParenIndex - openParenIndex + 1);

            FunctionDefinitionInfo info = new FunctionDefinitionInfo();
            info.Name = functionName;
            info.Signature = signature;
            results.Add(info);
        }

        return results;
    }

    private static string BuildUserFunctionDescription(FunctionDefinitionInfo functionInfo)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append("Category: User Function");
        builder.Append("\n\n");
        builder.Append("Defined in this script.");
        builder.Append("\n\n");
        builder.Append("Signature: ");
        builder.Append(functionInfo.Signature);
        return builder.ToString();
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

    public struct FunctionDefinitionInfo
    {
        public string Name;
        public string Signature;
    }
}