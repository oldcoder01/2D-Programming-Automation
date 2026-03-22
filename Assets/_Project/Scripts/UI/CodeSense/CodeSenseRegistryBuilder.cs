public static class CodeSenseRegistryBuilder
{
    public static CodeSenseRegistry Build(ScriptBuiltInRegistry builtInRegistry, CodeAnalysisResult analysisResult)
    {
        CodeSenseRegistry registry = new CodeSenseRegistry();

        RegisterKeywords(registry);
        RegisterBooleanLiterals(registry);
        RegisterOperators(registry);
        RegisterBuiltIns(registry, builtInRegistry);
        RegisterUserFunctions(registry, analysisResult);

        return registry;
    }

    private static void RegisterKeywords(CodeSenseRegistry registry)
    {
        RegisterSimple(registry, "if", CodeSymbolKind.Keyword, false);
        RegisterSimple(registry, "elif", CodeSymbolKind.Keyword, false);
        RegisterSimple(registry, "else", CodeSymbolKind.Keyword, false);
        RegisterSimple(registry, "while", CodeSymbolKind.Keyword, false);
        RegisterSimple(registry, "def", CodeSymbolKind.Keyword, false);
    }

    private static void RegisterBooleanLiterals(CodeSenseRegistry registry)
    {
        RegisterSimple(registry, "true", CodeSymbolKind.BooleanLiteral, false);
        RegisterSimple(registry, "false", CodeSymbolKind.BooleanLiteral, false);
    }

    private static void RegisterOperators(CodeSenseRegistry registry)
    {
        RegisterSimple(registry, "and", CodeSymbolKind.Operator, false);
        RegisterSimple(registry, "or", CodeSymbolKind.Operator, false);
        RegisterSimple(registry, "not", CodeSymbolKind.Operator, false);
    }

    private static void RegisterBuiltIns(CodeSenseRegistry registry, ScriptBuiltInRegistry builtInRegistry)
    {
        if (builtInRegistry == null)
        {
            return;
        }

        RegisterBuiltInIfPresent(registry, builtInRegistry, "move_up");
        RegisterBuiltInIfPresent(registry, builtInRegistry, "move_down");
        RegisterBuiltInIfPresent(registry, builtInRegistry, "move_left");
        RegisterBuiltInIfPresent(registry, builtInRegistry, "move_right");
        RegisterBuiltInIfPresent(registry, builtInRegistry, "pick_up");
        RegisterBuiltInIfPresent(registry, builtInRegistry, "drop_off");

        RegisterBuiltInIfPresent(registry, builtInRegistry, "package_here");
        RegisterBuiltInIfPresent(registry, builtInRegistry, "delivery_here");
        RegisterBuiltInIfPresent(registry, builtInRegistry, "carrying_package");
    }

    private static void RegisterBuiltInIfPresent(CodeSenseRegistry registry, ScriptBuiltInRegistry builtInRegistry, string name)
    {
        ScriptBuiltInDefinition builtInDefinition;
        if (!builtInRegistry.TryGetDefinition(name, out builtInDefinition))
        {
            return;
        }

        CodeSymbolDefinition definition = new CodeSymbolDefinition();
        definition.Name = builtInDefinition.Name;
        definition.Description = builtInDefinition.Description;
        definition.IsCallable = true;
        definition.IsLocked = !builtInDefinition.UnlockedByDefault;
        definition.RequiredUpgradeId = builtInDefinition.RequiredUpgradeId;

        if (builtInDefinition.Kind == ScriptBuiltInKind.Action)
        {
            definition.Kind = CodeSymbolKind.BuiltInAction;
        }
        else
        {
            definition.Kind = CodeSymbolKind.BuiltInQuery;
        }

        registry.Register(definition);
    }

    private static void RegisterUserFunctions(CodeSenseRegistry registry, CodeAnalysisResult analysisResult)
    {
        if (analysisResult == null)
        {
            return;
        }

        for (int i = 0; i < analysisResult.FunctionDefinitions.Count; i++)
        {
            CodeFunctionDefinition functionDefinition = analysisResult.FunctionDefinitions[i];

            CodeSymbolDefinition definition = new CodeSymbolDefinition();
            definition.Name = functionDefinition.Name;
            definition.Kind = CodeSymbolKind.UserFunction;
            definition.Description = "User-defined function.";
            definition.IsCallable = true;
            definition.IsLocked = false;
            definition.RequiredUpgradeId = null;

            registry.Register(definition);
        }
    }

    private static void RegisterSimple(CodeSenseRegistry registry, string name, CodeSymbolKind kind, bool isCallable)
    {
        CodeSymbolDefinition definition = new CodeSymbolDefinition();
        definition.Name = name;
        definition.Kind = kind;
        definition.Description = string.Empty;
        definition.IsCallable = isCallable;
        definition.IsLocked = false;
        definition.RequiredUpgradeId = null;

        registry.Register(definition);
    }
}