using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class GridCellView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public Vector2Int GridPosition { get; private set; }
    public TileType TileType { get; private set; }

    private void Reset()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(Vector2Int gridPosition, TileType tileType)
    {
        GridPosition = gridPosition;
        TileType = tileType;

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        _spriteRenderer.color = GetColorForTileType(tileType);
        gameObject.name = "Tile_" + gridPosition.x + "_" + gridPosition.y;
    }

    private Color GetColorForTileType(TileType tileType)
    {
        switch (tileType)
        {
            case TileType.Empty:
                return new Color(0.15f, 0.15f, 0.15f, 1f);

            case TileType.Road:
                return new Color(0.35f, 0.35f, 0.35f, 1f);

            case TileType.Depot:
                return new Color(0.2f, 0.5f, 1f, 1f);

            case TileType.Pickup:
                return new Color(1f, 0.85f, 0.2f, 1f);

            case TileType.Dropoff:
                return new Color(0.2f, 0.85f, 0.3f, 1f);

            default:
                return Color.magenta;
        }
    }
}