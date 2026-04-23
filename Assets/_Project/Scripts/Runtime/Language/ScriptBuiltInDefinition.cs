public enum ScriptBuiltInKind
{
    Action,
    Query
}

public sealed class ScriptBuiltInDefinition
{
    public string Name;
    public ScriptBuiltInKind Kind;
    public bool UnlockedByDefault;
    public string RequiredUpgradeId;

    public string Signature;
    public string Category;
    public string Summary;
    public string ReturnDescription;
    public string UsageNotes;
    public string Example;

    public string Description;

    public string GetDisplayTitle()
    {
        if (!string.IsNullOrEmpty(Signature))
        {
            if (Kind == ScriptBuiltInKind.Action)
            {
                return "Action: " + Signature;
            }

            return "Query: " + Signature;
        }

        if (Kind == ScriptBuiltInKind.Action)
        {
            return "Action: " + Name;
        }

        return "Query: " + Name;
    }

    public string GetShortDescription()
    {
        if (!string.IsNullOrWhiteSpace(Summary))
        {
            return Summary;
        }

        if (!string.IsNullOrWhiteSpace(Description))
        {
            return Description;
        }

        return string.Empty;
    }

    public string GetDetailDescription()
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        if (!string.IsNullOrWhiteSpace(Category))
        {
            builder.Append("Category: ");
            builder.Append(Category);
        }

        if (!string.IsNullOrWhiteSpace(Summary))
        {
            AppendSection(builder);
            builder.Append(Summary);
        }

        if (!string.IsNullOrWhiteSpace(ReturnDescription))
        {
            AppendSection(builder);
            builder.Append("Returns: ");
            builder.Append(ReturnDescription);
        }

        if (!string.IsNullOrWhiteSpace(UsageNotes))
        {
            AppendSection(builder);
            builder.Append("Notes: ");
            builder.Append(UsageNotes);
        }

        if (!string.IsNullOrWhiteSpace(Example))
        {
            AppendSection(builder);
            builder.Append("Example: ");
            builder.Append(Example);
        }

        if (builder.Length == 0 && !string.IsNullOrWhiteSpace(Description))
        {
            builder.Append(Description);
        }

        return builder.ToString();
    }

    public void RefreshLegacyDescription()
    {
        Description = GetDetailDescription();
    }

    private static void AppendSection(System.Text.StringBuilder builder)
    {
        if (builder.Length > 0)
        {
            builder.Append("\n\n");
        }
    }
}