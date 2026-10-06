using UnityEngine;

namespace GameMatch3.Gameplay.Board.Bot
{
    public enum PveBotDifficulty
    {
        FirstValidMove = 1,
        HighestScore = 2,
        CascadePriority = 3
    }

    public enum PveMoveTieBreak
    {
        RandomAmongBest = 0,
        FirstByScanOrder = 1
    }

    public readonly struct PveBotMove
    {
        public Vector2Int First { get; }
        public Vector2Int Second { get; }
        public int ImmediateScore { get; }
        public int TotalScore { get; }
        public bool HasCascade { get; }

        public PveBotMove(
            Vector2Int first,
            Vector2Int second,
            int immediateScore,
            int totalScore,
            bool hasCascade)
        {
            First = first;
            Second = second;
            ImmediateScore = immediateScore;
            TotalScore = totalScore;
            HasCascade = hasCascade;
        }
    }
}
