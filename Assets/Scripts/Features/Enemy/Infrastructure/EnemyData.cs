using Features.Enemy.Domain;
using UnityEngine;

namespace Features.Enemy.Infrastructure
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Enemy/EnemyData")]
    public sealed class EnemyData : ScriptableObject
    {
        [SerializeField] private string enemyId = "basic";
        [SerializeField] private float maxHp = 50f;
        [SerializeField] private float defense;
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float contactDamage = 10f;
        [SerializeField] private float contactCooldown = 1f;
        [SerializeField] private string prefabName = "EnemyCharacter";
        [Tooltip("Resources 폴더 기준 경로 (확장자 없음). Photon Instantiate 시 비-Master가 동일 스펙으로 로드한다.")]
        [SerializeField] private string resourcesLoadPath = "Enemy/BasicEnemy";
        [SerializeField] private EnemyTargetMode targetMode = EnemyTargetMode.ChaseNearestPlayer;
        [Tooltip("ChaseCoreAggroPlayerInRadius 전용. XZ 평면 반경.")]
        [SerializeField] private float aggroRadius;
        [Tooltip("코어를 추적 중일 때만 적용되는 정지 거리. 코어 트리거와 겹쳐 접촉 피해가 나가도록 보통 코어 반경 + 적 반경 수준으로 맞춘다. 플레이어를 추적 중일 때는 접촉 피해를 위해 기존처럼 거의 겹칠 때까지 전진한다.")]
        [SerializeField] private float stopDistance = 1.5f;

        public string EnemyId => enemyId;
        public string PrefabName => prefabName;
        public string ResourcesLoadPath => resourcesLoadPath;

        public EnemySpec ToSpec()
        {
            return new EnemySpec(maxHp, defense, moveSpeed, contactDamage, contactCooldown, targetMode, aggroRadius, stopDistance);
        }
    }
}
