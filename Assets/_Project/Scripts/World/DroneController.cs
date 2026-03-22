using UnityEngine;

public sealed class DroneController : MonoBehaviour
{
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private Vector2Int _startGridPosition = new Vector2Int(1, 3);

    public Vector2Int CurrentGridPosition { get; private set; }
    public bool IsCarryingPackage { get; private set; }

    private void Start()
    {
        ResetToStart();
    }

    public void ResetToStart()
    {
        IsCarryingPackage = false;
        SnapToGridPosition(_startGridPosition);
    }

    public void SnapToGridPosition(Vector2Int gridPosition)
    {
        if (_gridManager == null)
        {
            Debug.LogError("DroneController is missing GridManager reference.");
            return;
        }

        CurrentGridPosition = gridPosition;

        Vector3 worldPosition = _gridManager.GetWorldPosition(gridPosition);
        transform.position = new Vector3(worldPosition.x, worldPosition.y, -1f);
    }

    public bool TryPickUpPackage()
    {
        if (IsCarryingPackage)
        {
            Debug.Log("Already carrying a package.");
            return false;
        }

        IsCarryingPackage = true;
        return true;
    }

    public bool TryDropOffPackage()
    {
        if (!IsCarryingPackage)
        {
            Debug.Log("Not carrying a package.");
            return false;
        }

        IsCarryingPackage = false;
        return true;
    }
}