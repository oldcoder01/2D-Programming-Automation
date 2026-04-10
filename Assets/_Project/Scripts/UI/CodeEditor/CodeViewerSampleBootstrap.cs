using UnityEngine;

public sealed class CodeViewerSampleBootstrap : MonoBehaviour
{
    [SerializeField] private CodeViewerPresenter _viewerPresenter;

    private void Start()
    {
        if (_viewerPresenter == null)
        {
            Debug.LogError("CodeViewerSampleBootstrap is missing the CodeViewerPresenter reference.");
            return;
        }

    }
}