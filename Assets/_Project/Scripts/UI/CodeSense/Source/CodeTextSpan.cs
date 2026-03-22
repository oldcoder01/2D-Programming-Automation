public struct CodeTextSpan
{
    public int StartIndex;
    public int Length;
    public int Line;
    public int Column;

    public int EndIndex
    {
        get { return StartIndex + Length; }
    }

    public CodeTextSpan(int startIndex, int length, int line, int column)
    {
        StartIndex = startIndex;
        Length = length;
        Line = line;
        Column = column;
    }
}