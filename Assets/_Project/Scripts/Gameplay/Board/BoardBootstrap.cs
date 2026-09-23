using UnityEngine;

namespace GameMatch3.Gameplay.Board
{
    // Tạo board dự phòng cho Scene chưa có BoardController; Scene đã cấu hình thì giữ nguyên.
    public static class BoardBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateBoard()
        {
            // Tránh tạo board thứ hai khi Scene đã chứa Board_6x6.
            if (Object.FindObjectOfType<BoardController>() != null)
            {
                return;
            }

            GameObject boardObject = new GameObject("Board_6x6");
            boardObject.AddComponent<BoardController>();
        }
    }
}
