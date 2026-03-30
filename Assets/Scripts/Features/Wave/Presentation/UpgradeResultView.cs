using System;
using System.Collections;
using Features.Status.Domain;
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
            subscriber.Subscribe(this, new Action<UpgradeAppliedEvent>(OnUpgradeApplied));
            panel.SetActive(false);
        }

        private void OnUpgradeApplied(UpgradeAppliedEvent e)
        {
            var label = GetLabel(e.ChosenType);
            resultText.text = $"{label} Lv.{e.CurrentStacks}";
            panel.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(displayDuration);
            panel.SetActive(false);
        }

        private static string GetLabel(StatusType type)
        {
            switch (type)
            {
                case StatusType.Count: return "개수";
                case StatusType.Expand: return "범위";
                case StatusType.Extend: return "지속";
                default: return type.ToString();
            }
        }
    }
}
