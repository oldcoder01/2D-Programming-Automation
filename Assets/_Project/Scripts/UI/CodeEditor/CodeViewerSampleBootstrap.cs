using UnityEngine;

public sealed class CodeViewerSampleBootstrap : MonoBehaviour
{
    [SerializeField] private CodeViewerPresenter _viewerPresenter;

    [TextArea(10, 40)]
    [SerializeField] private string _sampleSource =
@"import routes
import cargo_tools

def run_delivery():
    package_count = 3
    max_weight = 12.5
    drone_name = ""Courier-7""

    # Go to the depot and collect cargo
    routes.go_to_depot()

    if cargo_tools.pick_up() == true:
        routes.turn_right()
        routes.move_forward()
        routes.move_forward()

        if package_count > 0:
            routes.go_to_customer()
            cargo_tools.drop_off()
            package_count = package_count - 1
        else:
            return

    while package_count > 0:
        routes.go_to_depot()

        if cargo_tools.pick_up() == false:
            break

        routes.go_to_customer()
        cargo_tools.drop_off()
        package_count = package_count - 1

    return

def idle_loop():
    while true:
        run_delivery()

idle_loop()
";

    private void Start()
    {
        if (_viewerPresenter == null)
        {
            Debug.LogError("CodeViewerSampleBootstrap is missing the CodeViewerPresenter reference.");
            return;
        }

        _viewerPresenter.SetSourceText(_sampleSource);
    }
}