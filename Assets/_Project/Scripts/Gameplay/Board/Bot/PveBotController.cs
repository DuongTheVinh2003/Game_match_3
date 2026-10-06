using System.Collections;
using UnityEngine;

namespace GameMatch3.Gameplay.Board.Bot
{
    // Điều phối riêng lượt bot: fake thinking, phân tích board và yêu cầu board thực hiện move.
    public sealed class PveBotController : MonoBehaviour
    {
        private BoardController board;
        private Coroutine turnRoutine;

        public void Initialize(BoardController owner)
        {
            board = owner;
        }

        public void BeginTurn()
        {
            if (board == null || !board.CanBotAct || turnRoutine != null)
            {
                return;
            }

            turnRoutine = StartCoroutine(RunTurn());
        }

        public void CancelTurn()
        {
            if (turnRoutine == null)
            {
                return;
            }

            StopCoroutine(turnRoutine);
            turnRoutine = null;
        }

        private void OnDisable()
        {
            CancelTurn();
        }

        private IEnumerator RunTurn()
        {
            board.SetBotThinkingState(true);

            // Bảo đảm turnRoutine được gán kể cả khi thinking time được cấu hình bằng 0.
            yield return null;

            while (board.CanBotAct)
            {
                LevelData level = board.CurrentPveLevel;
                PveBotSettings settings = level.BotSettings;
                float wait = Mathf.Lerp(
                    settings.ThinkingTimeMin,
                    settings.ThinkingTimeMax,
                    (float)board.GameplayRandom.NextDouble());
                if (wait > 0f)
                {
                    yield return new WaitForSecondsRealtime(wait);
                }

                if (!board.CanBotAct)
                {
                    break;
                }

                if (PveBotMoveFinder.TryChooseMove(
                        board.CurrentTiles,
                        settings.Difficulty,
                        settings.MoveTieBreak,
                        level.BotRefillDirection,
                        board.ScoringSettings,
                        board.GameplayRandom,
                        out PveBotMove move))
                {
                    turnRoutine = null;
                    board.ExecuteBotMove(move);
                    yield break;
                }

                // Bot không có nước thường hợp lệ: reshuffle và vẫn giữ lượt bot.
                yield return board.ReshuffleForBot();
                board.SetBotThinkingState(true);
            }

            turnRoutine = null;
        }
    }
}
