using System.Collections.Generic;

public sealed class CodeAnalysisResult
{
    public readonly List<CodeAnalysisToken> Tokens = new List<CodeAnalysisToken>();
    public readonly List<CodeFunctionDefinition> FunctionDefinitions = new List<CodeFunctionDefinition>();
    public readonly List<CodeCallSite> CallSites = new List<CodeCallSite>();

    public bool TryGetFunctionDefinition(string name, out CodeFunctionDefinition definition)
    {
        for (int i = 0; i < FunctionDefinitions.Count; i++)
        {
            if (FunctionDefinitions[i].Name == name)
            {
                definition = FunctionDefinitions[i];
                return true;
            }
        }

        definition = null;
        return false;
    }
}

public sealed class CodeFunctionDefinition
{
    public string Name;
    public CodeTextSpan NameSpan;
    public int Line;
}

public sealed class CodeCallSite
{
    public string Name;
    public CodeTextSpan NameSpan;
    public int Line;
}