using UnityEngine;

namespace GameMatch3.Gameplay.Board
{
    public static class BoardBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateBoard()
        {
            if (Object.FindObjectOfType<BoardController>() != null)
            {
                return;
            }

            GameObject boardObject = new GameObject("Board_6x6");
            boardObject.AddComponent<BoardController>();
        }
    }
}
