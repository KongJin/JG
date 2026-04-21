using System;
using Features.Lobby.Application.Events;
using Features.Lobby.Application.Ports;
using Shared.EventBus;

namespace Features.Lobby.Application
{
    public sealed class GameStartedSceneLoadEventHandler
    {
        private readonly ISceneLoaderPort _sceneLoader;
        private readonly string _sceneName;

        public GameStartedSceneLoadEventHandler(
            IEventSubscriber subscriber,
            ISceneLoaderPort sceneLoader,
            string sceneName)
        {
            _sceneLoader = sceneLoader;
            _sceneName = sceneName;
            subscriber.Subscribe(this, new Action<GameStartedEvent>(OnGameStarted));
        }

        private void OnGameStarted(GameStartedEvent _)
        {
            _sceneLoader.LoadScene(_sceneName);
        }
    }
}
