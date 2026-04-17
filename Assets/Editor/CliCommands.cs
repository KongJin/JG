#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectSD.EditorTools
{
    public static class CliCommands
    {
        private const string ScreenshotPath = "Screenshots";

        /// <summary>
        /// CodexLobbyScene을 열고 Play Mode로 진입한 후 스크린샷을 캡처합니다.
        /// CLI: -executeMethod ProjectSD.EditorTools.CliCommands.CaptureCodexLobbyScene
        /// </summary>
        public static void CaptureCodexLobbyScene()
        {
            Debug.Log("[CLI] Starting CaptureCodexLobbyScene...");

            // 컴파일 대기
            while (EditorApplication.isCompiling)
            {
                System.Threading.Thread.Sleep(100);
            }

            // 씬 열기
            var scenePath = "Assets/Scenes/CodexLobbyScene.unity";
            var scene = SceneManager.GetSceneByPath(scenePath);
            Debug.Log($"[CLI] Opened scene: {scene.name}");

            // Play Mode 시작
            EditorApplication.EnterPlaymode();
            Debug.Log("[CLI] Entered play mode...");

            // Play Mode 안정화 대기
            System.Threading.Thread.Sleep(3000); // 3초 대기

            // 스크린샷 캡처
            EnsureScreenshotFolder();
            var timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = $"{ScreenshotPath}/CodexLobby_{timestamp}.png";
            ScreenCapture.CaptureScreenshot(filename);
            Debug.Log($"[CLI] Screenshot saved to: {filename}");

            // 대기 (캡처 완료까지)
            System.Threading.Thread.Sleep(1000);

            // Play Mode 종료
            EditorApplication.ExitPlaymode();
            Debug.Log("[CLI] Exited play mode");

            Debug.Log("[CLI] CaptureCodexLobbyScene completed.");
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
