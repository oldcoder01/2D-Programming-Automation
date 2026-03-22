using System.Collections.Generic;

public sealed class ScriptBuiltInRegistry
{
    private readonly Dictionary<string, ScriptBuiltInDefinition> _definitions = new Dictionary<string, ScriptBuiltInDefinition>();

    public ScriptBuiltInRegistry()
    {
        RegisterAction("move_up", true, null, "Move the drone up by one tile.");
        RegisterAction("move_down", true, null, "Move the drone down by one tile.");
        RegisterAction("move_left", true, null, "Move the drone left by one tile.");
        RegisterAction("move_right", true, null, "Move the drone right by one tile.");
        RegisterAction("pick_up", true, null, "Pick up a package at the current tile.");
        RegisterAction("drop_off", true, null, "Drop off a package at the current tile.");

        RegisterQuery("package_here", true, null, "True when a package is available at the current tile.");
        RegisterQuery("delivery_here", true, null, "True when a valid delivery can be made here.");
        RegisterQuery("carrying_package", true, null, "True when the drone is carrying a package.");

        RegisterQuery("can_move_up", true, null, "True when the drone can move up.");
        RegisterQuery("can_move_down", true, null, "True when the drone can move down.");
        RegisterQuery("can_move_left", true, null, "True when the drone can move left.");
        RegisterQuery("can_move_right", true, null, "True when the drone can move right.");
    }

    public bool TryGetDefinition(string name, out ScriptBuiltInDefinition definition)
    {
        return _definitions.TryGetValue(name, out definition);
    }

    private void RegisterAction(string name, bool unlockedByDefault, string requiredUpgradeId, string description)
    {
        ScriptBuiltInDefinition definition = new ScriptBuiltInDefinition();
        definition.Name = name;
        definition.Kind = ScriptBuiltInKind.Action;
        definition.UnlockedByDefault = unlockedByDefault;
        definition.RequiredUpgradeId = requiredUpgradeId;
        definition.Description = description;
        _definitions[name] = definition;
    }

    private void RegisterQuery(string name, bool unlockedByDefault, string requiredUpgradeId, string description)
    {
        ScriptBuiltInDefinition definition = new ScriptBuiltInDefinition();
        definition.Name = name;
        definition.Kind = ScriptBuiltInKind.Query;
        definition.UnlockedByDefault = unlockedByDefault;
        definition.RequiredUpgradeId = requiredUpgradeId;
        definition.Description = description;
        _definitions[name] = definition;
    }
}