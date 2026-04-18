#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace ProjectSD.EditorTools.UnityMcp
{
    [Serializable]
    internal sealed class McpEditorStateSnapshot
    {
        public bool isPlaying;
        public bool isPlayingOrWillChange;
        public bool isPlayModeChanging;
        public bool rawIsPlayingOrWillChange;
        public bool isCompiling;
        public string activeScene;
        public string activeScenePath;
    }

    internal static class McpEditorState
    {
        public static McpEditorStateSnapshot Capture()
        {
            var scene = SceneManager.GetActiveScene();
            var isPlayModeChanging = UnityMcpBridge.IsPlayModeChanging();

            return new McpEditorStateSnapshot
            {
                isPlaying = EditorApplication.isPlaying,
                isPlayingOrWillChange = isPlayModeChanging,
                isPlayModeChanging = isPlayModeChanging,
                rawIsPlayingOrWillChange = EditorApplication.isPlayingOrWillChangePlaymode,
                isCompiling = EditorApplication.isCompiling,
                activeScene = scene.name,
                activeScenePath = scene.path
            };
        }
    }
}
#endif
