using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameMatch3.Gameplay.Board
{
    [Serializable]
    public sealed class LevelObjectSpawn
    {
        [SerializeField] private TileTypeId typeId;
        [SerializeField, Min(0.01f)] private float spawnWeight = 1f;

        public TileTypeId TypeId => typeId;
        public float SpawnWeight => Mathf.Max(0.01f, spawnWeight);

        public LevelObjectSpawn(TileTypeId typeId, float spawnWeight)
        {
            this.typeId = typeId;
            this.spawnWeight = spawnWeight;
        }
    }

    // Asset runtime cua mot level. Editor chi la cong cu tao va sua du lieu nay.
    [CreateAssetMenu(fileName = "Level_001", menuName = "Game Match 3/Level Data")]
    public sealed class LevelData : ScriptableObject
    {
        public const int DefaultBoardWidth = 5;
        public const int DefaultBoardHeight = 5;
        public const int DefaultObjectCount = 4;
        public const int DefaultMoves = 18;
        public const int DefaultTargetScore = 4000;

        [SerializeField, Min(1)] private int levelNumber = 1;
        [SerializeField] private int boardWidth = DefaultBoardWidth;
        [SerializeField] private int boardHeight = DefaultBoardHeight;
        [SerializeField] private int objectCount = DefaultObjectCount;
        [SerializeField] private List<LevelObjectSpawn> objects = CreateDefaultObjects();
        [SerializeField] private int startingMoves = DefaultMoves;
        [SerializeField] private int targetScore = DefaultTargetScore;

        public int LevelNumber => levelNumber;
        public int BoardWidth => boardWidth;
        public int BoardHeight => boardHeight;
        public int ObjectCount => objectCount;
        public IReadOnlyList<LevelObjectSpawn> Objects => objects;
        public int StartingMoves => startingMoves;
        public int TargetScore => targetScore;

        public void Configure(
            int newLevelNumber,
            int newBoardWidth,
            int newBoardHeight,
            int newObjectCount,
            IReadOnlyList<LevelObjectSpawn> newObjects,
            int newStartingMoves,
            int newTargetScore)
        {
            levelNumber = newLevelNumber;
            boardWidth = newBoardWidth;
            boardHeight = newBoardHeight;
            objectCount = newObjectCount;
            startingMoves = newStartingMoves;
            targetScore = newTargetScore;
            objects = new List<LevelObjectSpawn>(newObjects.Count);
            foreach (LevelObjectSpawn entry in newObjects)
            {
                objects.Add(new LevelObjectSpawn(entry.TypeId, entry.SpawnWeight));
            }
        }

        public void ResetToDefaults(int newLevelNumber)
        {
            Configure(
                newLevelNumber,
                DefaultBoardWidth,
                DefaultBoardHeight,
                DefaultObjectCount,
                CreateDefaultObjects(),
                DefaultMoves,
                DefaultTargetScore);
        }

        private static List<LevelObjectSpawn> CreateDefaultObjects()
        {
            return new List<LevelObjectSpawn>
            {
                new LevelObjectSpawn(TileTypeId.Tile01, 1f),
                new LevelObjectSpawn(TileTypeId.Tile02, 1f),
                new LevelObjectSpawn(TileTypeId.Tile03, 1f),
                new LevelObjectSpawn(TileTypeId.Tile04, 1f)
            };
        }
    }
}
