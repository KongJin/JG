using Features.Lobby.Application.Ports;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Features.Lobby.Infrastructure
{
    public sealed class SceneLoaderAdapter : ISceneLoaderPort
    {
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[SceneLoader] Scene name is empty. Scene load request ignored.");
                return;
            }

            Debug.Log($"[SceneLoader] Loading scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
    }
}
