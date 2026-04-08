using Shared.Attributes;
using System;
using Features.Player.Application.Events;
using Features.Wave.Application.Events;
using Features.Wave.Domain;
using Shared.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Wave.Presentation
{
    public sealed class WaveEndView : MonoBehaviour
    {
        [Required, SerializeField] private GameObject panel;
        [Required, SerializeField] private Text resultText;

        private IEventPublisher _publisher;

        public void Initialize(IEventSubscriber subscriber, IEventPublisher publisher)
        {
            _publisher = publisher;

            subscriber.Subscribe(this, new Action<WaveVictoryEvent>(OnVictory));
            subscriber.Subscribe(this, new Action<WaveDefeatEvent>(OnDefeat));
            subscriber.Subscribe(this, new Action<WaveHydratedEvent>(OnWaveHydrated));

            panel.SetActive(false);
        }

        private void OnVictory(WaveVictoryEvent e)
        {
            Show("Victory!");
            _publisher.Publish(new GameEndEvent(
                default(Shared.Kernel.DomainEntityId),
                default(Shared.Kernel.DomainEntityId),
                isLocalPlayerDead: false,
                "Victory!"));
        }

        private void OnDefeat(WaveDefeatEvent e)
        {
            Show("Defeat!");
            _publisher.Publish(new GameEndEvent(
                default(Shared.Kernel.DomainEntityId),
                default(Shared.Kernel.DomainEntityId),
                isLocalPlayerDead: true,
                "Defeat!"));
        }

        private void OnWaveHydrated(WaveHydratedEvent e)
        {
            switch (e.State)
            {
                case WaveState.Victory:
                    Show("Victory!");
                    _publisher.Publish(new GameEndEvent(
                        default(Shared.Kernel.DomainEntityId),
                        default(Shared.Kernel.DomainEntityId),
                        isLocalPlayerDead: false,
                        "Victory!"));
                    break;
                case WaveState.Defeat:
                    Show("Defeat!");
                    _publisher.Publish(new GameEndEvent(
                        default(Shared.Kernel.DomainEntityId),
                        default(Shared.Kernel.DomainEntityId),
                        isLocalPlayerDead: true,
                        "Defeat!"));
                    break;
            }
        }

        private void Show(string message)
        {
            if (panel != null) panel.SetActive(true);
            if (resultText != null) resultText.text = message;
        }
    }
}
