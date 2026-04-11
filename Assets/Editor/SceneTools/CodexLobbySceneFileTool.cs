#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ProjectSD.EditorTools.SceneTools
{
    internal static class CodexLobbySceneFileTool
    {
        private const string ScenePath = "Assets/Scenes/CodexLobbyScene.unity";

        [MenuItem("Tools/Codex/Create Or Open Codex Lobby Scene")]
        private static void CreateOrOpen()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath, true);
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
    }
}
#endif
