using System.Collections.Generic;

public sealed class ScriptBuiltInRegistry
{
    private readonly Dictionary<string, ScriptBuiltInDefinition> _definitions = new Dictionary<string, ScriptBuiltInDefinition>();

    public ScriptBuiltInRegistry()
    {
        RegisterAction("move_up", true, null, "Moves the drone up by one tile.");
        RegisterAction("move_down", true, null, "Moves the drone down by one tile.");
        RegisterAction("move_left", true, null, "Moves the drone left by one tile.");
        RegisterAction("move_right", true, null, "Moves the drone right by one tile.");
        RegisterAction("pick_up", true, null, "Picks up a package on the current tile, if one is available.");
        RegisterAction("drop_off", true, null, "Drops off the carried package on the current tile, if delivery is valid.");

        RegisterQuery("package_here", true, null, "Returns true if a package is on the drone's current tile.");
        RegisterQuery("delivery_here", true, null, "Returns true if the current tile is a delivery destination.");
        RegisterQuery("carrying_package", true, null, "Returns true if the drone is currently carrying a package.");

        RegisterQuery("can_move_up", true, null, "Returns true if the drone can move up from its current position.");
        RegisterQuery("can_move_down", true, null, "Returns true if the drone can move down from its current position.");
        RegisterQuery("can_move_left", true, null, "Returns true if the drone can move left from its current position.");
        RegisterQuery("can_move_right", true, null, "Returns true if the drone can move right from its current position.");
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