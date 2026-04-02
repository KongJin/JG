using System;
using System.Collections;
using Features.Skill.Domain;
using Features.Wave.Application.Events;
using Features.Wave.Application.Ports;
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
            if (e.CandidateType == CandidateType.NewSkill)
                resultText.text = $"{e.DisplayName} 획득!";
            else
                resultText.text = $"{e.DisplayName} 강화: {GetAxisLabel(e.Axis)} +1";

            panel.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(displayDuration);
            panel.SetActive(false);
        }

        private static string GetAxisLabel(GrowthAxis axis) => axis switch
        {
            GrowthAxis.Count => "발사 수",
            GrowthAxis.Range => "범위",
            GrowthAxis.Duration => "지속 시간",
            GrowthAxis.Safety => "아군 안전",
            _ => axis.ToString()
        };
    }
}
