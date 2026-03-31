using System;
using System.Collections;
using Features.Wave.Application.Events;
using Shared.Attributes;
using Shared.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Wave.Presentation
{
    public sealed class UpgradeResultView : MonoBehaviour
    {
        [Required, SerializeField] private GameObject panel;
        [Required, SerializeField] private Text resultText;
        [SerializeField] private float displayDuration = 2f;

        public void Initialize(IEventSubscriber subscriber)
        {
            subscriber.Subscribe(this, new Action<SkillSelectedEvent>(OnSkillSelected));
            panel.SetActive(false);
        }

        private void OnSkillSelected(SkillSelectedEvent e)
        {
            resultText.text = $"{e.DisplayName} 획득!";
            panel.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(displayDuration);
            panel.SetActive(false);
        }
    }
}
