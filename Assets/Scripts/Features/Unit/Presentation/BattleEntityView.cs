using Features.Unit.Application.Events;
using Features.Unit.Domain;
using Shared.Attributes;
using Shared.EventBus;
using Shared.Kernel;
using UnityEngine;

namespace Features.Unit.Presentation
{
    /// <summary>
    /// BattleEntity의 시각 표현.
    /// HP 바, 데미지 플래시, 사망 효과 등을 담당.
    /// </summary>
    public sealed class BattleEntityView : MonoBehaviour
    {
        [Header("References")]
        [Required, SerializeField] private EntityIdHolder _entityIdHolder;
        [Required, SerializeField] private Renderer _bodyRenderer;

        [Header("Effects")]
        [SerializeField] private Material _damageFlashMaterial;
        [SerializeField] private float _flashDuration = 0.15f;
        [SerializeField] private GameObject _deathEffectPrefab;

        private IEventSubscriber _eventBus;
        private BattleEntity _battleEntity;
        private Material _originalMaterial;
        private bool _isFlashing;

        public void Initialize(IEventSubscriber eventBus, BattleEntity battleEntity)
        {
            _eventBus = eventBus;
            _battleEntity = battleEntity;
            _originalMaterial = _bodyRenderer.material;

            // EntityIdHolder 설정
            _entityIdHolder.Set(battleEntity.Id);

            // 이벤트 구독
            _eventBus.Subscribe(this, new System.Action<DamageAppliedEvent>(OnDamageApplied));
            _eventBus.Subscribe(this, new System.Action<Application.Events.UnitDiedEvent>(OnUnitDied));
        }

        private void OnDestroy()
        {
            _eventBus?.UnsubscribeAll(this);
        }

        private void OnDamageApplied(DamageAppliedEvent e)
        {
            if (!e.TargetId.Equals(_battleEntity.Id))
                return;

            // 데미지 플래시 효과
            if (_damageFlashMaterial != null && !_isFlashing)
            {
                StartCoroutine(FlashDamage());
            }
        }

        private void OnUnitDied(UnitDiedEvent e)
        {
            if (!e.EntityId.Equals(_battleEntity.Id))
                return;

            // 사망 효과
            if (_deathEffectPrefab != null)
            {
                Instantiate(_deathEffectPrefab, transform.position, Quaternion.identity);
            }

            //GameObject 비활성화 (풀링을 위해 파괴 대신)
            gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator FlashDamage()
        {
            _isFlashing = true;
            _bodyRenderer.material = _damageFlashMaterial;
            yield return new WaitForSeconds(_flashDuration);
            _bodyRenderer.material = _originalMaterial;
            _isFlashing = false;
        }
    }
}
