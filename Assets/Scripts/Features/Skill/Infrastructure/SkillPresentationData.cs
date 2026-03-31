using Shared.Attributes;
using UnityEngine;

namespace Features.Skill.Infrastructure
{
    /// <summary>ScriptableObject that holds a skill's presentation resources (UI, VFX, SFX).</summary>
    [CreateAssetMenu(fileName = "NewSkillPresentation", menuName = "Skill/SkillPresentationData")]
    public sealed class SkillPresentationData : ScriptableObject
    {
        [Header("UI")]
        [SerializeField] private string displayName;
        [TextArea(1, 3)]
        [SerializeField] private string description;
        [Required, SerializeField] private Sprite icon;

        [Header("Effects")]
        [Required, SerializeField] private GameObject castEffectPrefab;
        [SerializeField] private AudioClip castSound;

        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public GameObject CastEffectPrefab => castEffectPrefab;
        public AudioClip CastSound => castSound;
    }
}
