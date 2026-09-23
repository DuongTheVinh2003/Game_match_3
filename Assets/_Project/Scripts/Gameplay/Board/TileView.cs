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
            ApplyDefinition(definition, fallbackSprite);
            spriteRenderer.sortingOrder = 1;
            UpdateObjectName();
        }

        public void SetType(TileTypeId typeId, TilePoolEntry definition, Sprite fallbackSprite)
        {
            Tile.SetTypeId(typeId);
            ApplyDefinition(definition, fallbackSprite);
            UpdateObjectName();
        }

        private void ApplyDefinition(TilePoolEntry definition, Sprite fallbackSprite)
        {
            bool usesPrototypeVisual = definition.Sprite == null;
            spriteRenderer.sprite = usesPrototypeVisual ? fallbackSprite : definition.Sprite;
            spriteRenderer.color = usesPrototypeVisual ? definition.PrototypeColor : Color.white;
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
