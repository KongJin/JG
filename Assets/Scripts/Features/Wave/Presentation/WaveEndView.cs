using Shared.Attributes;
using System;
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

        public void Initialize(IEventSubscriber subscriber)
        {
            subscriber.Subscribe(this, new Action<WaveVictoryEvent>(OnVictory));
            subscriber.Subscribe(this, new Action<WaveDefeatEvent>(OnDefeat));
            subscriber.Subscribe(this, new Action<WaveHydratedEvent>(OnWaveHydrated));

            panel.SetActive(false);
        }

        private void OnVictory(WaveVictoryEvent e)
        {
            Show("Victory!");
        }

        private void OnDefeat(WaveDefeatEvent e)
        {
            Show("Defeat!");
        }

        private void OnWaveHydrated(WaveHydratedEvent e)
        {
            switch (e.State)
            {
                case WaveState.Victory:
                    Show("Victory!");
                    break;
                case WaveState.Defeat:
                    Show("Defeat!");
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
