using TMPro;

public sealed class CodeEditorHistoryState
{
    public string Text;
    public int StringPosition;
    public int SelectionStringAnchorPosition;
    public int SelectionStringFocusPosition;
    public int CaretPosition;
    public int SelectionAnchorPosition;
    public int SelectionFocusPosition;

    public static CodeEditorHistoryState Capture(TMP_InputField inputField)
    {
        CodeEditorHistoryState state = new CodeEditorHistoryState();

        if (inputField == null)
        {
            state.Text = string.Empty;
            return state;
        }

        state.Text = inputField.text ?? string.Empty;
        state.StringPosition = inputField.stringPosition;
        state.SelectionStringAnchorPosition = inputField.selectionStringAnchorPosition;
        state.SelectionStringFocusPosition = inputField.selectionStringFocusPosition;
        state.CaretPosition = inputField.caretPosition;
        state.SelectionAnchorPosition = inputField.selectionAnchorPosition;
        state.SelectionFocusPosition = inputField.selectionFocusPosition;

        return state;
    }

    public void ApplyTo(TMP_InputField inputField)
    {
        if (inputField == null)
        {
            return;
        }

        string safeText = Text ?? string.Empty;
        int textLength = safeText.Length;

        inputField.text = safeText;

        inputField.stringPosition = ClampPosition(StringPosition, textLength);
        inputField.selectionStringAnchorPosition = ClampPosition(SelectionStringAnchorPosition, textLength);
        inputField.selectionStringFocusPosition = ClampPosition(SelectionStringFocusPosition, textLength);
        inputField.caretPosition = ClampPosition(CaretPosition, textLength);
        inputField.selectionAnchorPosition = ClampPosition(SelectionAnchorPosition, textLength);
        inputField.selectionFocusPosition = ClampPosition(SelectionFocusPosition, textLength);

        inputField.ForceLabelUpdate();
    }

    public bool ContentEquals(CodeEditorHistoryState other)
    {
        if (other == null)
        {
            return false;
        }

        return Text == other.Text
            && StringPosition == other.StringPosition
            && SelectionStringAnchorPosition == other.SelectionStringAnchorPosition
            && SelectionStringFocusPosition == other.SelectionStringFocusPosition
            && CaretPosition == other.CaretPosition
            && SelectionAnchorPosition == other.SelectionAnchorPosition
            && SelectionFocusPosition == other.SelectionFocusPosition;
    }

    private static int ClampPosition(int value, int max)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }
}