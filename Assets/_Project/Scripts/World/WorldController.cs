using UnityEngine;

public sealed class WorldController : MonoBehaviour
{
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private DroneController _droneController;

    private bool _pickupAvailable = true;
    private int _deliveredCount;

    public GridManager GridManager
    {
        get { return _gridManager; }
    }

    public DroneController DroneController
    {
        get { return _droneController; }
    }

    public int DeliveredCount
    {
        get { return _deliveredCount; }
    }

    public Vector2Int CurrentDroneGridPosition
    {
        get
        {
            if (_droneController == null)
            {
                return Vector2Int.zero;
            }

            return _droneController.CurrentGridPosition;
        }
    }

    public bool IsDroneAtPickup
    {
        get
        {
            if (_gridManager == null || _droneController == null)
            {
                return false;
            }

            Vector2Int currentGridPosition = _droneController.CurrentGridPosition;

            if (!_gridManager.TryGetCellData(currentGridPosition, out GridCellData cellData))
            {
                return false;
            }

            return cellData.TileType == TileType.Pickup;
        }
    }

    public void ResetWorldState()
    {
        _pickupAvailable = true;
        _deliveredCount = 0;

        if (_droneController != null)
        {
            _droneController.ResetToStart();
        }
    }

    public ScriptActionResult TryMoveDrone(Vector2Int direction, string commandName)
    {
        if (_gridManager == null)
        {
            return ScriptActionResult.Failed(commandName + " failed. GridManager is missing.");
        }

        if (_droneController == null)
        {
            return ScriptActionResult.Failed(commandName + " failed. DroneController is missing.");
        }

        Vector2Int currentGridPosition = _droneController.CurrentGridPosition;
        Vector2Int targetGridPosition = currentGridPosition + direction;

        if (!_gridManager.TryGetCellData(targetGridPosition, out GridCellData cellData))
        {
            return ScriptActionResult.Failed(commandName + " failed. That move is outside the grid.");
        }

        if (!cellData.Walkable)
        {
            return ScriptActionResult.Failed(commandName + " failed. That tile is not walkable.");
        }

        _droneController.SnapToGridPosition(targetGridPosition);
        return ScriptActionResult.Succeeded();
    }

    public ScriptActionResult TryMoveUp()
    {
        return TryMoveDrone(Vector2Int.up, "move_up()");
    }

    public ScriptActionResult TryMoveDown()
    {
        return TryMoveDrone(Vector2Int.down, "move_down()");
    }

    public ScriptActionResult TryMoveLeft()
    {
        return TryMoveDrone(Vector2Int.left, "move_left()");
    }

    public ScriptActionResult TryMoveRight()
    {
        return TryMoveDrone(Vector2Int.right, "move_right()");
    }

    public ScriptActionResult TryPickUp()
    {
        if (_gridManager == null || _droneController == null)
        {
            return ScriptActionResult.Failed("pick_up() failed. World references are missing.");
        }

        if (_droneController.IsCarryingPackage)
        {
            return ScriptActionResult.Failed("pick_up() failed. Already carrying a package.");
        }

        Vector2Int currentGridPosition = _droneController.CurrentGridPosition;

        if (!_gridManager.TryGetCellData(currentGridPosition, out GridCellData cellData))
        {
            return ScriptActionResult.Failed("pick_up() failed. Current tile could not be read.");
        }

        if (cellData.TileType != TileType.Pickup)
        {
            return ScriptActionResult.Failed("pick_up() failed. No package is here.");
        }

        if (!_pickupAvailable)
        {
            return ScriptActionResult.Failed("pick_up() failed. No package is available.");
        }

        bool success = _droneController.TryPickUpPackage();
        if (!success)
        {
            return ScriptActionResult.Failed("pick_up() failed. The drone could not pick up the package.");
        }

        _pickupAvailable = false;
        return ScriptActionResult.Succeeded("Picked up package.");
    }

    public ScriptActionResult TryDropOff()
    {
        if (_gridManager == null || _droneController == null)
        {
            return ScriptActionResult.Failed("drop_off() failed. World references are missing.");
        }

        if (!_droneController.IsCarryingPackage)
        {
            return ScriptActionResult.Failed("drop_off() failed. No package is being carried.");
        }

        Vector2Int currentGridPosition = _droneController.CurrentGridPosition;

        if (!_gridManager.TryGetCellData(currentGridPosition, out GridCellData cellData))
        {
            return ScriptActionResult.Failed("drop_off() failed. Current tile could not be read.");
        }

        if (cellData.TileType != TileType.Dropoff)
        {
            return ScriptActionResult.Failed("drop_off() failed. No delivery target is here.");
        }

        bool success = _droneController.TryDropOffPackage();
        if (!success)
        {
            return ScriptActionResult.Failed("drop_off() failed. The drone could not drop off the package.");
        }

        _deliveredCount += 1;
        _pickupAvailable = true;

        return ScriptActionResult.Succeeded("Delivered package.");
    }

    public bool CanMoveUp()
    {
        return CanMove(Vector2Int.up);
    }

    public bool CanMoveDown()
    {
        return CanMove(Vector2Int.down);
    }

    public bool CanMoveLeft()
    {
        return CanMove(Vector2Int.left);
    }

    public bool CanMoveRight()
    {
        return CanMove(Vector2Int.right);
    }

    private bool CanMove(Vector2Int direction)
    {
        if (_gridManager == null || _droneController == null)
        {
            return false;
        }

        Vector2Int currentGridPosition = _droneController.CurrentGridPosition;
        Vector2Int targetGridPosition = currentGridPosition + direction;

        if (!_gridManager.TryGetCellData(targetGridPosition, out GridCellData cellData))
        {
            return false;
        }

        return cellData.Walkable;
    }

    public bool IsPackageHere()
    {
        if (_gridManager == null || _droneController == null)
        {
            return false;
        }

        Vector2Int currentGridPosition = _droneController.CurrentGridPosition;

        if (!_gridManager.TryGetCellData(currentGridPosition, out GridCellData cellData))
        {
            return false;
        }

        return cellData.TileType == TileType.Pickup && _pickupAvailable;
    }

    public bool IsDeliveryHere()
    {
        if (_gridManager == null || _droneController == null)
        {
            return false;
        }

        Vector2Int currentGridPosition = _droneController.CurrentGridPosition;

        if (!_gridManager.TryGetCellData(currentGridPosition, out GridCellData cellData))
        {
            return false;
        }

        if (cellData.TileType != TileType.Dropoff)
        {
            return false;
        }

        return _droneController.IsCarryingPackage;
    }

    public bool IsCarryingPackage()
    {
        if (_droneController == null)
        {
            return false;
        }

        return _droneController.IsCarryingPackage;
    }
}