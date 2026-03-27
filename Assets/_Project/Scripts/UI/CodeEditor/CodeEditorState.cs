public sealed class CodeEditorState
{
    public int CaretIndex;
    public int SelectionAnchorIndex;
    public int SelectionFocusIndex;

    public void Clear()
    {
        CaretIndex = 0;
        SelectionAnchorIndex = 0;
        SelectionFocusIndex = 0;
    }

    public bool HasSelection()
    {
        return SelectionAnchorIndex != SelectionFocusIndex;
    }

    public int GetSelectionStart()
    {
        if (SelectionAnchorIndex < SelectionFocusIndex)
        {
            return SelectionAnchorIndex;
        }

        return SelectionFocusIndex;
    }

    public int GetSelectionEnd()
    {
        if (SelectionAnchorIndex > SelectionFocusIndex)
        {
            return SelectionAnchorIndex;
        }

        return SelectionFocusIndex;
    }

    public void SetCaret(int index)
    {
        CaretIndex = index;
        SelectionAnchorIndex = index;
        SelectionFocusIndex = index;
    }

    public void SetSelection(int anchorIndex, int focusIndex)
    {
        SelectionAnchorIndex = anchorIndex;
        SelectionFocusIndex = focusIndex;
        CaretIndex = focusIndex;
    }
}