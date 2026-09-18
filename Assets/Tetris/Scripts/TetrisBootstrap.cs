using UnityEngine;

namespace TetrisGame
{
    public static class TetrisBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindGame() != null)
            {
                return;
            }

            var root = new GameObject("Tetris");
            root.AddComponent<TetrisGame>();
        }

        static TetrisGame FindGame()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<TetrisGame>();
#else
            return Object.FindObjectOfType<TetrisGame>();
#endif
        }
    }
}
