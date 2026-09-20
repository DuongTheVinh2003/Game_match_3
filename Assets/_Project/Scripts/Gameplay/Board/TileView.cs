using UnityEngine;

namespace GameMatch3.Gameplay.Board
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TileView : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;

        public Vector2Int Coordinate { get; private set; }
        public TileColor ColorType { get; private set; }

        public void Initialize(Vector2Int coordinate, TileColor colorType, Sprite sprite)
        {
            Coordinate = coordinate;
            ColorType = colorType;

            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = ToUnityColor(colorType);
            spriteRenderer.sortingOrder = 1;

            transform.name = $"Tile_{coordinate.x}_{coordinate.y}_{colorType}";
        }

        public void SetCoordinate(Vector2Int coordinate)
        {
            Coordinate = coordinate;
            transform.name = $"Tile_{coordinate.x}_{coordinate.y}_{ColorType}";
        }

        private static Color ToUnityColor(TileColor colorType)
        {
            switch (colorType)
            {
                case TileColor.Red:
                    return new Color(0.91f, 0.20f, 0.22f);
                case TileColor.Yellow:
                    return new Color(1.00f, 0.78f, 0.12f);
                case TileColor.Green:
                    return new Color(0.20f, 0.72f, 0.32f);
                case TileColor.Blue:
                    return new Color(0.16f, 0.48f, 0.90f);
                default:
                    return Color.white;
            }
        }
    }
}
