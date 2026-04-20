#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectSD.EditorTools
{
    public static class CliCommands
    {
        private const string ScreenshotPath = "Screenshots";
        private const string LobbyScenePath = "Assets/Scenes/LobbyScene.unity";

        /// <summary>
        /// LobbyScene을 열고 Play Mode로 진입한 후 스크린샷을 캡처합니다.
        /// CLI: -executeMethod ProjectSD.EditorTools.CliCommands.CaptureLobbyScene
        /// </summary>
        public static void CaptureLobbyScene()
        {
            Debug.Log("[CLI] Starting CaptureLobbyScene...");

            // 컴파일 대기
            while (EditorApplication.isCompiling)
            {
                System.Threading.Thread.Sleep(100);
            }

            var scenePath = LobbyScenePath;
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogError("[CLI] CaptureLobbyScene is a historical workflow. LobbyScene.unity does not exist. Use the prefab-first reset route instead.");
                EditorApplication.Exit(1);
                return;
            }

            // 씬 열기
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log($"[CLI] Opened scene: {scene.name}");

            // Play Mode 시작
            EditorApplication.EnterPlaymode();
            Debug.Log("[CLI] Entered play mode...");

            // Play Mode 안정화 대기
            System.Threading.Thread.Sleep(3000); // 3초 대기

            // 스크린샷 캡처
            EnsureScreenshotFolder();
            var timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = $"{ScreenshotPath}/Lobby_{timestamp}.png";
            ScreenCapture.CaptureScreenshot(filename);
            Debug.Log($"[CLI] Screenshot saved to: {filename}");

            // 대기 (캡처 완료까지)
            System.Threading.Thread.Sleep(1000);

            // Play Mode 종료
            EditorApplication.ExitPlaymode();
            Debug.Log("[CLI] Exited play mode");

            Debug.Log("[CLI] CaptureLobbyScene completed.");
            EditorApplication.Exit(0);
        }

        private static void EnsureScreenshotFolder()
        {
            if (!System.IO.Directory.Exists(ScreenshotPath))
            {
                System.IO.Directory.CreateDirectory(ScreenshotPath);
            }
        }
    }
}
#endif
