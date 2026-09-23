using UnityEngine;

namespace GameMatch3.Gameplay.Board
{
    // Du lieu gameplay cua mot tile cu the, tach khoi GameObject dung de hien thi.
    public sealed class Tile
    {
        public int InstanceId { get; }
        public TileTypeId TypeId { get; private set; }
        public Vector2Int Coordinate { get; private set; }
        public SpecialObjectType SpecialType { get; private set; }
        public StripedDirection StripedDirection { get; private set; }
        public bool CanMatchByType => SpecialType != SpecialObjectType.ColorBomb;

        public Tile(int instanceId, TileTypeId typeId, Vector2Int coordinate)
        {
            InstanceId = instanceId;
            TypeId = typeId;
            Coordinate = coordinate;
            SpecialType = SpecialObjectType.None;
            StripedDirection = StripedDirection.Horizontal;
        }

        public void SetCoordinate(Vector2Int coordinate)
        {
            Coordinate = coordinate;
        }

        public void SetTypeId(TileTypeId typeId)
        {
            TypeId = typeId;
        }

        public void SetSpecialObject(
            SpecialObjectType specialType,
            StripedDirection stripedDirection = StripedDirection.Horizontal)
        {
            SpecialType = specialType;
            StripedDirection = stripedDirection;
        }
    }
}
