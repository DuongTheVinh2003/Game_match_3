using UnityEngine;

namespace GameMatch3.Gameplay.Board
{
    // Hien thi mot Tile; du lieu ID va toa do nam trong model Tile.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TileView : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;

        public Tile Tile { get; private set; }
        public int InstanceId => Tile.InstanceId;
        public TileTypeId TypeId => Tile.TypeId;
        public Vector2Int Coordinate => Tile.Coordinate;

        public void Initialize(Tile tile, TilePoolEntry definition, Sprite fallbackSprite)
        {
            Tile = tile;

            spriteRenderer = GetComponent<SpriteRenderer>();
            bool usesPrototypeVisual = definition.Sprite == null;
            spriteRenderer.sprite = usesPrototypeVisual ? fallbackSprite : definition.Sprite;
            spriteRenderer.color = usesPrototypeVisual ? definition.PrototypeColor : Color.white;
            spriteRenderer.sortingOrder = 1;

            UpdateObjectName();
        }

        public void SetCoordinate(Vector2Int coordinate)
        {
            Tile.SetCoordinate(coordinate);
            UpdateObjectName();
        }

        private void UpdateObjectName()
        {
            transform.name = $"Tile_{TypeId}_I{InstanceId:000000}_{Coordinate.x}_{Coordinate.y}";
        }
    }
}
