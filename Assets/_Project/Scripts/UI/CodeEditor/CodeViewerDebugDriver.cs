using UnityEngine;

public sealed class CodeViewerDebugDriver : MonoBehaviour
{
    [SerializeField] private CodeViewerPresenter _viewerPresenter;

    [Header("Debug Actions")]
    [SerializeField] private bool _insertHeaderLine;
    [SerializeField] private bool _appendFooterLine;
    [SerializeField] private bool _removeFirstLine;
    [SerializeField] private bool _replaceFirstWord;

    private void Update()
    {
        if (_viewerPresenter == null)
        {
            return;
        }

        if (_insertHeaderLine)
        {
            _insertHeaderLine = false;
            InsertHeaderLine();
        }

        if (_appendFooterLine)
        {
            _appendFooterLine = false;
            AppendFooterLine();
        }

        if (_removeFirstLine)
        {
            _removeFirstLine = false;
            RemoveFirstLine();
        }

        if (_replaceFirstWord)
        {
            _replaceFirstWord = false;
            ReplaceFirstWord();
        }
    }

    private void InsertHeaderLine()
    {
        CodeDocument document = _viewerPresenter.GetDocument();
        document.InsertText(0, "# inserted header line\n");
        _viewerPresenter.RebuildFromDocument(false);
    }

    private void AppendFooterLine()
    {
        CodeDocument document = _viewerPresenter.GetDocument();
        document.InsertText(document.Length, "\n# appended footer line");
        _viewerPresenter.RebuildFromDocument(false);
    }

    private void RemoveFirstLine()
    {
        CodeDocument document = _viewerPresenter.GetDocument();

        if (document.LineCount <= 0)
        {
            return;
        }

        int endIndex = document.GetLineEndIndexExclusive(0);

        if (endIndex < document.Length && document.Text[endIndex] == '\n')
        {
            endIndex++;
        }

        document.RemoveText(0, endIndex);
        _viewerPresenter.RebuildFromDocument(false);
    }

    private void ReplaceFirstWord()
    {
        CodeDocument document = _viewerPresenter.GetDocument();
        string text = document.Text;

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        int startIndex = -1;
        int endIndex = -1;

        for (int i = 0; i < text.Length; i++)
        {
            if (!char.IsWhiteSpace(text[i]))
            {
                startIndex = i;
                break;
            }
        }

        if (startIndex < 0)
        {
            return;
        }

        endIndex = startIndex;

        while (endIndex < text.Length && !char.IsWhiteSpace(text[endIndex]))
        {
            endIndex++;
        }

        document.ReplaceText(startIndex, endIndex - startIndex, "import");
        _viewerPresenter.RebuildFromDocument(false);
    }
}