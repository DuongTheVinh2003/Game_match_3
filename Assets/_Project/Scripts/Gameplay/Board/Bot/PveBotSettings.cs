using System;
using UnityEngine;

namespace GameMatch3.Gameplay.Board.Bot
{
    [Serializable]
    public sealed class PveBotSettings
    {
        [SerializeField] private PveBotDifficulty difficulty = PveBotDifficulty.FirstValidMove;
        [SerializeField, Min(0f)] private float thinkingTimeMin = 0.5f;
        [SerializeField, Min(0f)] private float thinkingTimeMax = 1.2f;
        [SerializeField] private PveMoveTieBreak moveTieBreak = PveMoveTieBreak.RandomAmongBest;

        public PveBotDifficulty Difficulty => difficulty;
        public float ThinkingTimeMin => Mathf.Max(0f, thinkingTimeMin);
        public float ThinkingTimeMax => Mathf.Max(ThinkingTimeMin, thinkingTimeMax);
        public PveMoveTieBreak MoveTieBreak => moveTieBreak;

        public void Configure(
            PveBotDifficulty newDifficulty,
            float newThinkingTimeMin,
            float newThinkingTimeMax,
            PveMoveTieBreak newMoveTieBreak)
        {
            difficulty = newDifficulty;
            thinkingTimeMin = newThinkingTimeMin;
            thinkingTimeMax = newThinkingTimeMax;
            moveTieBreak = newMoveTieBreak;
        }
    }
}
