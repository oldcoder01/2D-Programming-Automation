using System.Collections.Generic;

public sealed class ScriptBuiltInRegistry
{
    private readonly Dictionary<string, ScriptBuiltInDefinition> _definitions = new Dictionary<string, ScriptBuiltInDefinition>();

    public ScriptBuiltInRegistry()
    {
        RegisterAction(
            "move_up",
            "move_up()",
            "Movement",
            true,
            null,
            "Moves the drone up by one tile.",
            "Completes one movement step upward when the tile above is reachable.",
            "Fails if the drone cannot move upward from its current position.",
            "move_up()"
        );

        RegisterAction(
            "move_down",
            "move_down()",
            "Movement",
            true,
            null,
            "Moves the drone down by one tile.",
            "Completes one movement step downward when the tile below is reachable.",
            "Fails if the drone cannot move downward from its current position.",
            "move_down()"
        );

        RegisterAction(
            "move_left",
            "move_left()",
            "Movement",
            true,
            null,
            "Moves the drone left by one tile.",
            "Completes one movement step to the left when that tile is reachable.",
            "Fails if the drone cannot move left from its current position.",
            "move_left()"
        );

        RegisterAction(
            "move_right",
            "move_right()",
            "Movement",
            true,
            null,
            "Moves the drone right by one tile.",
            "Completes one movement step to the right when that tile is reachable.",
            "Fails if the drone cannot move right from its current position.",
            "move_right()"
        );

        RegisterAction(
            "pick_up",
            "pick_up()",
            "Package Handling",
            true,
            null,
            "Picks up a package on the current tile, if one is available.",
            "Attempts to collect the package at the drone's current position.",
            "Fails if there is no package here or if the drone is already carrying one.",
            "if package_here():\n    pick_up()"
        );

        RegisterAction(
            "drop_off",
            "drop_off()",
            "Package Handling",
            true,
            null,
            "Drops off the carried package on the current tile, if delivery is valid.",
            "Attempts to deliver the currently carried package at the drone's position.",
            "Fails if the drone is not carrying a package or if this is not a valid delivery tile.",
            "if delivery_here():\n    drop_off()"
        );

        RegisterQuery(
            "package_here",
            "package_here()",
            "Sensing",
            true,
            null,
            "Returns true if a package is on the drone's current tile.",
            "true or false",
            "Use this before pick_up() when checking the current tile.",
            "if package_here():\n    pick_up()"
        );

        RegisterQuery(
            "delivery_here",
            "delivery_here()",
            "Sensing",
            true,
            null,
            "Returns true if the current tile is a delivery destination.",
            "true or false",
            "Use this before drop_off() when checking the current tile.",
            "if delivery_here():\n    drop_off()"
        );

        RegisterQuery(
            "carrying_package",
            "carrying_package()",
            "State",
            true,
            null,
            "Returns true if the drone is currently carrying a package.",
            "true or false",
            "Useful for switching between pickup logic and delivery logic.",
            "if carrying_package():\n    move_right()"
        );

        RegisterQuery(
            "can_move_up",
            "can_move_up()",
            "Movement Check",
            true,
            null,
            "Returns true if the drone can move up from its current position.",
            "true or false",
            "Use before move_up() to avoid invalid movement.",
            "while can_move_up():\n    move_up()"
        );

        RegisterQuery(
            "can_move_down",
            "can_move_down()",
            "Movement Check",
            true,
            null,
            "Returns true if the drone can move down from its current position.",
            "true or false",
            "Use before move_down() to avoid invalid movement.",
            "while can_move_down():\n    move_down()"
        );

        RegisterQuery(
            "can_move_left",
            "can_move_left()",
            "Movement Check",
            true,
            null,
            "Returns true if the drone can move left from its current position.",
            "true or false",
            "Use before move_left() to avoid invalid movement.",
            "while can_move_left():\n    move_left()"
        );

        RegisterQuery(
            "can_move_right",
            "can_move_right()",
            "Movement Check",
            true,
            null,
            "Returns true if the drone can move right from its current position.",
            "true or false",
            "Use before move_right() to avoid invalid movement.",
            "while can_move_right():\n    move_right()"
        );
    }

    public bool TryGetDefinition(string name, out ScriptBuiltInDefinition definition)
    {
        return _definitions.TryGetValue(name, out definition);
    }

    public IReadOnlyCollection<ScriptBuiltInDefinition> GetDefinitions()
    {
        return _definitions.Values;
    }

    private void RegisterAction(
        string name,
        string signature,
        string category,
        bool unlockedByDefault,
        string requiredUpgradeId,
        string summary,
        string returnDescription,
        string usageNotes,
        string example)
    {
        ScriptBuiltInDefinition definition = new ScriptBuiltInDefinition();
        definition.Name = name;
        definition.Kind = ScriptBuiltInKind.Action;
        definition.Signature = signature;
        definition.Category = category;
        definition.UnlockedByDefault = unlockedByDefault;
        definition.RequiredUpgradeId = requiredUpgradeId;
        definition.Summary = summary;
        definition.ReturnDescription = returnDescription;
        definition.UsageNotes = usageNotes;
        definition.Example = example;
        definition.RefreshLegacyDescription();
        _definitions[name] = definition;
    }

    private void RegisterQuery(
        string name,
        string signature,
        string category,
        bool unlockedByDefault,
        string requiredUpgradeId,
        string summary,
        string returnDescription,
        string usageNotes,
        string example)
    {
        ScriptBuiltInDefinition definition = new ScriptBuiltInDefinition();
        definition.Name = name;
        definition.Kind = ScriptBuiltInKind.Query;
        definition.Signature = signature;
        definition.Category = category;
        definition.UnlockedByDefault = unlockedByDefault;
        definition.RequiredUpgradeId = requiredUpgradeId;
        definition.Summary = summary;
        definition.ReturnDescription = returnDescription;
        definition.UsageNotes = usageNotes;
        definition.Example = example;
        definition.RefreshLegacyDescription();
        _definitions[name] = definition;
    }
}