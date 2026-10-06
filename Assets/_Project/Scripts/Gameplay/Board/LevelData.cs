using System;
using System.Collections.Generic;
using GameMatch3.Gameplay.Board.Bot;
using UnityEngine;

namespace GameMatch3.Gameplay.Board
{
    public enum LevelMode
    {
        Classic = 0,
        Pve = 1
    }

    public enum PveRefillDirection
    {
        FromTop = 0,
        FromBottom = 1
    }

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
        public const int DefaultPveHealth = 1000;

        [SerializeField] private LevelMode mode = LevelMode.Classic;
        [SerializeField, Min(1)] private int levelNumber = 1;
        [SerializeField] private int boardWidth = DefaultBoardWidth;
        [SerializeField] private int boardHeight = DefaultBoardHeight;
        [SerializeField] private List<LevelObjectSpawn> objects = CreateDefaultObjects();
        [SerializeField] private int startingMoves = DefaultMoves;
        [SerializeField] private int targetScore = DefaultTargetScore;

        [Header("PVE")]
        [SerializeField, Min(1)] private int playerMaxHealth = DefaultPveHealth;
        [SerializeField, Min(1)] private int botMaxHealth = DefaultPveHealth;
        [SerializeField, Min(0.0001f)] private float scoreToDamageMultiplier = 1f;
        [SerializeField] private PveBotSettings botSettings = new PveBotSettings();
        [SerializeField] private PveRefillDirection playerRefillDirection = PveRefillDirection.FromTop;
        [SerializeField] private PveRefillDirection botRefillDirection = PveRefillDirection.FromBottom;

        public LevelMode Mode => mode;
        public int LevelNumber => levelNumber;
        public int BoardWidth => boardWidth;
        public int BoardHeight => boardHeight;
        public IReadOnlyList<LevelObjectSpawn> Objects => objects;
        public int StartingMoves => startingMoves;
        public int TargetScore => targetScore;
        public int PlayerMaxHealth => Mathf.Max(1, playerMaxHealth);
        public int BotMaxHealth => Mathf.Max(1, botMaxHealth);
        public float ScoreToDamageMultiplier => Mathf.Max(0.0001f, scoreToDamageMultiplier);
        public PveBotSettings BotSettings => botSettings ?? (botSettings = new PveBotSettings());
        public PveRefillDirection PlayerRefillDirection => playerRefillDirection;
        public PveRefillDirection BotRefillDirection => botRefillDirection;

        public void Configure(
            LevelMode newMode,
            int newLevelNumber,
            int newBoardWidth,
            int newBoardHeight,
            IReadOnlyList<LevelObjectSpawn> newObjects,
            int newStartingMoves,
            int newTargetScore,
            int newPlayerMaxHealth,
            int newBotMaxHealth,
            float newScoreToDamageMultiplier,
            PveBotDifficulty newBotDifficulty,
            float newBotThinkingTimeMin,
            float newBotThinkingTimeMax,
            PveMoveTieBreak newBotMoveTieBreak,
            PveRefillDirection newPlayerRefillDirection,
            PveRefillDirection newBotRefillDirection)
        {
            mode = newMode;
            levelNumber = newLevelNumber;
            boardWidth = newBoardWidth;
            boardHeight = newBoardHeight;
            startingMoves = newStartingMoves;
            targetScore = newTargetScore;
            playerMaxHealth = newPlayerMaxHealth;
            botMaxHealth = newBotMaxHealth;
            scoreToDamageMultiplier = newScoreToDamageMultiplier;
            BotSettings.Configure(
                newBotDifficulty,
                newBotThinkingTimeMin,
                newBotThinkingTimeMax,
                newBotMoveTieBreak);
            playerRefillDirection = newPlayerRefillDirection;
            botRefillDirection = newBotRefillDirection;
            objects = new List<LevelObjectSpawn>(newObjects.Count);
            foreach (LevelObjectSpawn entry in newObjects)
            {
                objects.Add(new LevelObjectSpawn(entry.TypeId, entry.SpawnWeight));
            }
        }

        public void ResetToDefaults(int newLevelNumber)
        {
            Configure(
                mode,
                newLevelNumber,
                DefaultBoardWidth,
                DefaultBoardHeight,
                CreateDefaultObjects(),
                DefaultMoves,
                DefaultTargetScore,
                DefaultPveHealth,
                DefaultPveHealth,
                1f,
                PveBotDifficulty.FirstValidMove,
                0.5f,
                1.2f,
                PveMoveTieBreak.RandomAmongBest,
                PveRefillDirection.FromTop,
                PveRefillDirection.FromBottom);
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
