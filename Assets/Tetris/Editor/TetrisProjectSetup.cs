#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TetrisGame.Editor
{
    [InitializeOnLoad]
    public static class TetrisProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Tetris.unity";

        static TetrisProjectSetup()
        {
            EditorApplication.delayCall += EnsureBuildScene;
        }

        static void EnsureBuildScene()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes != null && scenes.Length > 0)
            {
                return;
            }

            if (!System.IO.File.Exists(ScenePath))
            {
                return;
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        [MenuItem("Tetris/Открыть сцену")]
        static void OpenScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
#endif
