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

        public string EnemyId => enemyId;
        public string PrefabName => prefabName;
        public string ResourcesLoadPath => resourcesLoadPath;

        public EnemySpec ToSpec()
        {
            return new EnemySpec(maxHp, defense, moveSpeed, contactDamage, contactCooldown);
        }
    }
}
