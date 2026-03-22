using System.Text;

public sealed class CodeHighlightRichTextBuilder
{
    public string Build(string source, CodeStyleResult styleResult, CodeStyleMap styleMap)
    {
        if (string.IsNullOrEmpty(source))
        {
            return string.Empty;
        }

        if (styleResult == null || styleMap == null || styleResult.Spans.Count == 0)
        {
            return EscapeRichText(source);
        }

        StringBuilder builder = new StringBuilder(source.Length * 2);

        for (int i = 0; i < styleResult.Spans.Count; i++)
        {
            CodeStyleSpan span = styleResult.Spans[i];

            if (span.Length <= 0)
            {
                continue;
            }

            int start = span.StartIndex;
            int end = span.EndIndex;

            if (start < 0)
            {
                start = 0;
            }

            if (end > source.Length)
            {
                end = source.Length;
            }

            if (end <= start)
            {
                continue;
            }

            string segment = source.Substring(start, end - start);
            string escapedSegment = EscapeRichText(segment);
            string color = styleMap.GetColor(span.StyleKind);

            builder.Append("<color=");
            builder.Append(color);
            builder.Append(">");
            builder.Append(escapedSegment);
            builder.Append("</color>");
        }

        return builder.ToString();
    }

    private string EscapeRichText(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}