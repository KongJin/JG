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

        public string EnemyId => enemyId;
        public string PrefabName => prefabName;

        public EnemySpec ToSpec()
        {
            return new EnemySpec(maxHp, defense, moveSpeed, contactDamage, contactCooldown);
        }
    }
}
