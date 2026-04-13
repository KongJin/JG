#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectSD.EditorTools.UnityMcp
{
    /// <summary>
    /// MCP 전역 설정. 정적 기본값을 사용하며 런타임 변경 가능.
    /// 파일 기반 설정은 도입하지 않고 메모리 상태로 관리한다.
    /// </summary>
    internal static class McpConfig
    {
        // 자동 씬 저장 옵션
        public static bool AutoSaveSceneOnPlayStop { get; set; } = false;
        public static bool AutoSaveSceneOnBuild { get; set; } = false;

        /// <summary>
        /// 현재 열려 있는 씬이 더티하면 자동으로 저장한다.
        /// PlayStop/Build 직전에 호출한다.
        /// </summary>
        public static bool TryAutoSaveScene(out string message)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded || !scene.isDirty || string.IsNullOrWhiteSpace(scene.path))
            {
                message = "No dirty scene to save.";
                return false;
            }

            if (EditorSceneManager.SaveScene(scene))
            {
                message = "Auto-saved: " + scene.path;
                return true;
            }

            message = "Failed to auto-save: " + scene.path;
            return false;
        }
    }
}
#endif
