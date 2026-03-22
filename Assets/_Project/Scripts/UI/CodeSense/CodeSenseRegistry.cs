using System.Collections.Generic;

public sealed class CodeSenseRegistry
{
    private readonly Dictionary<string, CodeSymbolDefinition> _definitions = new Dictionary<string, CodeSymbolDefinition>();

    public IEnumerable<CodeSymbolDefinition> Definitions
    {
        get { return _definitions.Values; }
    }

    public void Register(CodeSymbolDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            return;
        }

        _definitions[definition.Name] = definition;
    }

    public bool TryGetDefinition(string name, out CodeSymbolDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            definition = null;
            return false;
        }

        return _definitions.TryGetValue(name, out definition);
    }

    public void ClearUserFunctions()
    {
        List<string> namesToRemove = new List<string>();

        foreach (KeyValuePair<string, CodeSymbolDefinition> pair in _definitions)
        {
            if (pair.Value != null && pair.Value.Kind == CodeSymbolKind.UserFunction)
            {
                namesToRemove.Add(pair.Key);
            }
        }

        for (int i = 0; i < namesToRemove.Count; i++)
        {
            _definitions.Remove(namesToRemove[i]);
        }
    }
}