using System.Collections.Generic;
using UnityEngine;

public sealed class GridManager : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int _width = 7;
    [SerializeField] private int _height = 5;
    [SerializeField] private float _cellSize = 1f;

    [Header("Scene References")]
    [SerializeField] private Transform _tileRoot;
    [SerializeField] private GridCellView _gridCellPrefab;

    private readonly Dictionary<Vector2Int, GridCellData> _gridData = new Dictionary<Vector2Int, GridCellData>();
    private readonly Dictionary<Vector2Int, GridCellView> _gridViews = new Dictionary<Vector2Int, GridCellView>();

    public int Width
    {
        get { return _width; }
    }

    public int Height
    {
        get { return _height; }
    }

    public float CellSize
    {
        get { return _cellSize; }
    }

    private void Start()
    {
        BuildPrototypeGrid();
    }

    public void BuildPrototypeGrid()
    {
        ClearGrid();

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                Vector2Int gridPosition = new Vector2Int(x, y);
                TileType tileType = GetPrototypeTileType(gridPosition);

                GridCellData cellData = new GridCellData();
                cellData.GridPosition = gridPosition;
                cellData.TileType = tileType;
                cellData.Walkable = tileType != TileType.Empty;

                _gridData.Add(gridPosition, cellData);

                Vector3 worldPosition = GetWorldPosition(gridPosition);
                GridCellView cellView = Instantiate(_gridCellPrefab, worldPosition, Quaternion.identity, _tileRoot);
                cellView.Initialize(gridPosition, tileType);

                _gridViews.Add(gridPosition, cellView);
            }
        }
    }

    public Vector3 GetWorldPosition(Vector2Int gridPosition)
    {
        float originOffsetX = -((_width - 1) * _cellSize) * 0.5f;
        float originOffsetY = -((_height - 1) * _cellSize) * 0.5f;

        float worldX = originOffsetX + (gridPosition.x * _cellSize);
        float worldY = originOffsetY + (gridPosition.y * _cellSize);

        return new Vector3(worldX, worldY, 0f);
    }

    public bool TryGetCellData(Vector2Int gridPosition, out GridCellData cellData)
    {
        return _gridData.TryGetValue(gridPosition, out cellData);
    }

    public bool IsWalkable(Vector2Int gridPosition)
    {
        if (!_gridData.TryGetValue(gridPosition, out GridCellData cellData))
        {
            return false;
        }

        return cellData.Walkable;
    }
    
    private TileType GetPrototypeTileType(Vector2Int gridPosition)
    {
        if (gridPosition == new Vector2Int(1, 3))
        {
            return TileType.Depot;
        }

        if (gridPosition == new Vector2Int(4, 3))
        {
            return TileType.Pickup;
        }

        if (gridPosition == new Vector2Int(4, 0))
        {
            return TileType.Dropoff;
        }

        bool isRoad =
            gridPosition == new Vector2Int(2, 3) ||
            gridPosition == new Vector2Int(3, 3) ||
            gridPosition == new Vector2Int(4, 2) ||
            gridPosition == new Vector2Int(4, 1);

        if (isRoad)
        {
            return TileType.Road;
        }

        return TileType.Empty;
    }

    private void ClearGrid()
    {
        _gridData.Clear();
        _gridViews.Clear();

        if (_tileRoot == null)
        {
            return;
        }

        for (int i = _tileRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(_tileRoot.GetChild(i).gameObject);
        }
    }
}