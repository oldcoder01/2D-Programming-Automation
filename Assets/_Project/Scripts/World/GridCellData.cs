using UnityEngine;

[System.Serializable]
public sealed class GridCellData
{
    public Vector2Int GridPosition;
    public TileType TileType;
    public bool Walkable;
}