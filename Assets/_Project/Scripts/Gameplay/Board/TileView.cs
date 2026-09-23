using UnityEngine;

namespace GameMatch3.Gameplay.Board
{
    // Hien thi mot Tile; du lieu ID va toa do nam trong model Tile.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TileView : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Sprite normalFallbackSprite;
        private Sprite specialSprite;
        private TilePoolEntry currentDefinition;

        public Tile Tile { get; private set; }
        public int InstanceId => Tile.InstanceId;
        public TileTypeId TypeId => Tile.TypeId;
        public Vector2Int Coordinate => Tile.Coordinate;
        public SpecialObjectType SpecialType => Tile.SpecialType;
        public StripedDirection StripedDirection => Tile.StripedDirection;
        public bool CanMatchByType => Tile.CanMatchByType;

        public void Initialize(Tile tile, TilePoolEntry definition, Sprite fallbackSprite)
        {
            Tile = tile;
            spriteRenderer = GetComponent<SpriteRenderer>();
            normalFallbackSprite = fallbackSprite;
            currentDefinition = definition;
            ApplyDefinition(definition, fallbackSprite);
            spriteRenderer.sortingOrder = 1;
            UpdateObjectName();
        }

        public void SetType(TileTypeId typeId, TilePoolEntry definition, Sprite fallbackSprite)
        {
            Tile.SetTypeId(typeId);
            normalFallbackSprite = fallbackSprite;
            currentDefinition = definition;
            ApplyCurrentVisual();
            UpdateObjectName();
        }

        public void SetSpecialObject(
            SpecialObjectType specialType,
            StripedDirection stripedDirection,
            Sprite modelSprite)
        {
            Tile.SetSpecialObject(specialType, stripedDirection);
            specialSprite = modelSprite;
            ApplyCurrentVisual();
            UpdateObjectName();
        }

        private void ApplyCurrentVisual()
        {
            if (SpecialType == SpecialObjectType.None)
            {
                ApplyDefinition(currentDefinition, normalFallbackSprite);
                transform.localRotation = Quaternion.identity;
                return;
            }

            spriteRenderer.sprite = specialSprite;
            spriteRenderer.color = SpecialType == SpecialObjectType.ColorBomb
                ? new Color(0.24f, 0.16f, 0.30f)
                : currentDefinition.PrototypeColor;
            transform.localRotation = SpecialType == SpecialObjectType.Striped
                && StripedDirection == StripedDirection.Vertical
                    ? Quaternion.Euler(0f, 0f, 90f)
                    : Quaternion.identity;
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
            string typeName = SpecialType == SpecialObjectType.ColorBomb
                ? SpecialType.ToString()
                : $"{TypeId}_{SpecialType}";
            transform.name = $"Tile_{typeName}_I{InstanceId:000000}_{Coordinate.x}_{Coordinate.y}";
        }
    }
}
