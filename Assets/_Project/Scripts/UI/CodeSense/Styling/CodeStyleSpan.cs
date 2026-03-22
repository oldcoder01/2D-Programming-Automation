public struct CodeStyleSpan
{
    public int StartIndex;
    public int Length;
    public CodeStyleKind StyleKind;

    public int EndIndex
    {
        get { return StartIndex + Length; }
    }

    public CodeStyleSpan(int startIndex, int length, CodeStyleKind styleKind)
    {
        StartIndex = startIndex;
        Length = length;
        StyleKind = styleKind;
    }
}