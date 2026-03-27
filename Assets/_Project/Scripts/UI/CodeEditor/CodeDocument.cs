using System.Collections.Generic;

public sealed class CodeDocument
{
    private string _text = string.Empty;
    private readonly List<int> _lineStartIndices = new List<int>();

    public string Text
    {
        get { return _text; }
    }

    public int Length
    {
        get { return _text.Length; }
    }

    public int LineCount
    {
        get { return _lineStartIndices.Count; }
    }

    public CodeDocument()
    {
        RebuildLineStarts();
    }

    public void SetText(string text)
    {
        _text = NormalizeNewlines(text);
        RebuildLineStarts();
    }

    public int ClampIndex(int index)
    {
        if (index < 0)
        {
            return 0;
        }

        if (index > _text.Length)
        {
            return _text.Length;
        }

        return index;
    }

    public int GetLineStartIndex(int lineIndex)
    {
        if (_lineStartIndices.Count == 0)
        {
            return 0;
        }

        if (lineIndex < 0)
        {
            return _lineStartIndices[0];
        }

        if (lineIndex >= _lineStartIndices.Count)
        {
            return _lineStartIndices[_lineStartIndices.Count - 1];
        }

        return _lineStartIndices[lineIndex];
    }

    public int GetLineEndIndexExclusive(int lineIndex)
    {
        if (_lineStartIndices.Count == 0)
        {
            return 0;
        }

        if (lineIndex < 0)
        {
            lineIndex = 0;
        }

        if (lineIndex >= _lineStartIndices.Count)
        {
            lineIndex = _lineStartIndices.Count - 1;
        }

        if (lineIndex == _lineStartIndices.Count - 1)
        {
            return _text.Length;
        }

        return _lineStartIndices[lineIndex + 1] - 1;
    }

    public int GetLineLength(int lineIndex)
    {
        int startIndex = GetLineStartIndex(lineIndex);
        int endIndexExclusive = GetLineEndIndexExclusive(lineIndex);
        return endIndexExclusive - startIndex;
    }

    public int GetLineIndexFromCharacterIndex(int characterIndex)
    {
        int clampedIndex = ClampIndex(characterIndex);

        if (_lineStartIndices.Count == 0)
        {
            return 0;
        }

        int low = 0;
        int high = _lineStartIndices.Count - 1;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            int midStart = _lineStartIndices[mid];

            if (midStart == clampedIndex)
            {
                return mid;
            }

            if (midStart < clampedIndex)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        if (high < 0)
        {
            return 0;
        }

        return high;
    }

    public int GetColumnFromCharacterIndex(int characterIndex)
    {
        int clampedIndex = ClampIndex(characterIndex);
        int lineIndex = GetLineIndexFromCharacterIndex(clampedIndex);
        int lineStartIndex = GetLineStartIndex(lineIndex);
        return clampedIndex - lineStartIndex;
    }

    public int GetCharacterIndexFromLineAndColumn(int lineIndex, int columnIndex)
    {
        int safeLineIndex = lineIndex;

        if (safeLineIndex < 0)
        {
            safeLineIndex = 0;
        }

        if (safeLineIndex >= LineCount)
        {
            safeLineIndex = LineCount - 1;
        }

        if (safeLineIndex < 0)
        {
            return 0;
        }

        int lineStartIndex = GetLineStartIndex(safeLineIndex);
        int lineLength = GetLineLength(safeLineIndex);

        int safeColumnIndex = columnIndex;

        if (safeColumnIndex < 0)
        {
            safeColumnIndex = 0;
        }

        if (safeColumnIndex > lineLength)
        {
            safeColumnIndex = lineLength;
        }

        return lineStartIndex + safeColumnIndex;
    }

    public string GetLineText(int lineIndex)
    {
        if (LineCount == 0)
        {
            return string.Empty;
        }

        int startIndex = GetLineStartIndex(lineIndex);
        int endIndexExclusive = GetLineEndIndexExclusive(lineIndex);
        int length = endIndexExclusive - startIndex;

        if (length <= 0)
        {
            return string.Empty;
        }

        return _text.Substring(startIndex, length);
    }

    private void RebuildLineStarts()
    {
        _lineStartIndices.Clear();
        _lineStartIndices.Add(0);

        for (int i = 0; i < _text.Length; i++)
        {
            if (_text[i] == '\n')
            {
                _lineStartIndices.Add(i + 1);
            }
        }
    }

    private static string NormalizeNewlines(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string normalized = value.Replace("\r\n", "\n");
        normalized = normalized.Replace('\r', '\n');
        return normalized;
    }

    public void InsertText(int index, string value)
    {
        int safeIndex = ClampIndex(index);
        string normalizedValue = NormalizeNewlines(value);

        _text = _text.Insert(safeIndex, normalizedValue);
        RebuildLineStarts();
    }

    public void RemoveText(int startIndex, int length)
    {
        int safeStartIndex = ClampIndex(startIndex);

        if (length <= 0)
        {
            return;
        }

        if (safeStartIndex >= _text.Length)
        {
            return;
        }

        int safeLength = length;

        if (safeStartIndex + safeLength > _text.Length)
        {
            safeLength = _text.Length - safeStartIndex;
        }

        if (safeLength <= 0)
        {
            return;
        }

        _text = _text.Remove(safeStartIndex, safeLength);
        RebuildLineStarts();
    }

    public void ReplaceText(int startIndex, int length, string value)
    {
        RemoveText(startIndex, length);
        InsertText(startIndex, value);
    }

    public int GetPreviousWordBoundary(int index)
    {
        int safeIndex = ClampIndex(index);

        if (safeIndex <= 0)
        {
            return 0;
        }

        int i = safeIndex;

        while (i > 0 && !IsWordCharacter(_text[i - 1]))
        {
            i--;
        }

        while (i > 0 && IsWordCharacter(_text[i - 1]))
        {
            i--;
        }

        return i;
    }

    public int GetNextWordBoundary(int index)
    {
        int safeIndex = ClampIndex(index);

        if (safeIndex >= _text.Length)
        {
            return _text.Length;
        }

        int i = safeIndex;

        while (i < _text.Length && !IsWordCharacter(_text[i]))
        {
            i++;
        }

        while (i < _text.Length && IsWordCharacter(_text[i]))
        {
            i++;
        }

        return i;
    }

    public int GetLineIndexContainingSelectionStart(int index)
    {
        return GetLineIndexFromCharacterIndex(index);
    }

    public int GetLineIndexContainingSelectionEnd(int index)
    {
        int safeIndex = ClampIndex(index);

        if (safeIndex > 0 && safeIndex == _text.Length)
        {
            return GetLineIndexFromCharacterIndex(safeIndex - 1);
        }

        if (safeIndex > 0 && safeIndex <= _text.Length && _text[safeIndex - 1] == '\n')
        {
            return GetLineIndexFromCharacterIndex(safeIndex - 1);
        }

        return GetLineIndexFromCharacterIndex(safeIndex);
    }

    private static bool IsWordCharacter(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }

    private static string EscapeRichText(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return "<noparse>" + value + "</noparse>";
    }

    public string GetLeadingWhitespace(int lineIndex)
    {
        string lineText = GetLineText(lineIndex);

        if (string.IsNullOrEmpty(lineText))
        {
            return string.Empty;
        }

        int index = 0;

        while (index < lineText.Length && char.IsWhiteSpace(lineText[index]) && lineText[index] != '\n')
        {
            index++;
        }

        if (index <= 0)
        {
            return string.Empty;
        }

        return lineText.Substring(0, index);
    }

    public bool LineStartsWithOnlyWhitespaceBeforeIndex(int lineIndex, int characterIndex)
    {
        int lineStartIndex = GetLineStartIndex(lineIndex);
        int safeCharacterIndex = ClampIndex(characterIndex);

        for (int i = lineStartIndex; i < safeCharacterIndex && i < _text.Length; i++)
        {
            if (!char.IsWhiteSpace(_text[i]))
            {
                return false;
            }
        }

        return true;
    }

    public int GetFirstNonWhitespaceIndexInLine(int lineIndex)
    {
        int lineStartIndex = GetLineStartIndex(lineIndex);
        int lineEndIndexExclusive = GetLineEndIndexExclusive(lineIndex);

        for (int i = lineStartIndex; i < lineEndIndexExclusive && i < _text.Length; i++)
        {
            if (!char.IsWhiteSpace(_text[i]))
            {
                return i;
            }
        }

        return lineEndIndexExclusive;
    }
}