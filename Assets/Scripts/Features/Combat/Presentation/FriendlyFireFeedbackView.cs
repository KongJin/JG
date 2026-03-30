using System.Collections;
using Features.Combat.Application.Events;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using Shared.Math;
using Shared.Sound;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Combat.Presentation
{
    public sealed class FriendlyFireFeedbackView : MonoBehaviour
    {
        [Required, SerializeField] private GameObject _bannerPanel;
        [Required, SerializeField] private Text _bannerText;
        [SerializeField] private float _displayDuration = 1.5f;
        [SerializeField] private string _warningSoundKey = "friendly_fire_warning";

        private IEventSubscriber _eventSubscriber;
        private IEventPublisher _eventPublisher;
        private DomainEntityId _localPlayerId;

        public void Initialize(
            IEventSubscriber eventSubscriber,
            IEventPublisher eventPublisher,
            DomainEntityId localPlayerId
        )
        {
            _eventSubscriber = eventSubscriber;
            _eventPublisher = eventPublisher;
            _localPlayerId = localPlayerId;
            _bannerPanel.SetActive(false);
            _eventSubscriber.Subscribe(this, new System.Action<FriendlyFireAppliedEvent>(OnFriendlyFire));
        }

        private void OnDestroy()
        {
            _eventSubscriber?.UnsubscribeAll(this);
        }

        private void OnFriendlyFire(FriendlyFireAppliedEvent e)
        {
            if (e.AttackerId.Equals(_localPlayerId))
            {
                ShowBanner($"아군 피격! ({e.Damage:F0})");
                PlayWarningSound();
            }
            else if (e.TargetId.Equals(_localPlayerId))
            {
                ShowBanner($"아군에게 피격! ({e.Damage:F0})");
            }
        }

        private void ShowBanner(string message)
        {
            _bannerText.text = message;
            _bannerPanel.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(_displayDuration);
            _bannerPanel.SetActive(false);
        }

        private void PlayWarningSound()
        {
            if (string.IsNullOrEmpty(_warningSoundKey))
                return;

            _eventPublisher.Publish(new SoundRequestEvent(
                new SoundRequest(
                    _warningSoundKey,
                    Float3.Zero,
                    PlaybackPolicy.LocalOnly,
                    _localPlayerId.Value
                )
            ));
        }
    }
}
