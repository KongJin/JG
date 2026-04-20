#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ProjectSD.EditorTools.SceneTools
{
    internal static class LobbySceneFileTool
    {
        private const string ScenePath = "Assets/Scenes/LobbyScene.unity";

        [MenuItem("Tools/Scene/Create Or Open Lobby Scene")]
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
