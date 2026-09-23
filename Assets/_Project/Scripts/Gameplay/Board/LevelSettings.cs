using System;
using UnityEngine;

namespace GameMatch3.Gameplay.Board
{
    public enum LevelResult
    {
        InProgress,
        Won,
        Lost
    }

    // Du lieu level duoc serialize san de Level Editor co the gan lai sau nay.
    [Serializable]
    public sealed class LevelSettings
    {
        [Header("Identity and Goal")]
        [SerializeField, Min(1)] private int levelNumber = 1;
        [SerializeField, Min(1)] private int targetScore = 4000;
        [SerializeField, Min(1)] private int startingMoves = 18;

        [Header("Scoring")]
        [SerializeField, Min(0)] private int pointsPerClearedTile = 10;
        [SerializeField, Min(0)] private int stripedCreationBonus = 20;
        [SerializeField, Min(0)] private int wrappedCreationBonus = 40;
        [SerializeField, Min(0)] private int colorBombCreationBonus = 60;
        [SerializeField, Min(1)] private int maximumCascadeMultiplier = 5;

        public int LevelNumber => Mathf.Max(1, levelNumber);
        public int TargetScore => Mathf.Max(1, targetScore);
        public int StartingMoves => Mathf.Max(1, startingMoves);
        public int PointsPerClearedTile => Mathf.Max(0, pointsPerClearedTile);
        public int MaximumCascadeMultiplier => Mathf.Max(1, maximumCascadeMultiplier);

        public int GetCreationBonus(SpecialObjectType specialType)
        {
            switch (specialType)
            {
                case SpecialObjectType.Striped:
                    return Mathf.Max(0, stripedCreationBonus);
                case SpecialObjectType.Wrapped:
                    return Mathf.Max(0, wrappedCreationBonus);
                case SpecialObjectType.ColorBomb:
                    return Mathf.Max(0, colorBombCreationBonus);
                default:
                    return 0;
            }
        }
    }
}
