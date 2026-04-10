using Enemies;
using UnityEngine;

namespace Towers
{
    public class TowerEffectContext
    {
        public TowerAgent Tower { get; }
        public EnemyAgent TargetEnemy { get; }
        public Vector3 TowerPosition { get; }
        public Vector3 TargetPosition { get; }
        public Vector3 EffectPosition { get; }
        public float DamageAmount { get; }
        public bool WasKill { get; }
        public TowerRuntimeContext RuntimeContext { get; }

        public TowerEffectContext(
            TowerAgent tower,
            EnemyAgent targetEnemy,
            Vector3 towerPosition,
            Vector3 targetPosition,
            Vector3 effectPosition,
            float damageAmount,
            bool wasKill,
            TowerRuntimeContext runtimeContext)
        {
            Tower = tower;
            TargetEnemy = targetEnemy;
            TowerPosition = towerPosition;
            TargetPosition = targetPosition;
            EffectPosition = effectPosition;
            DamageAmount = damageAmount;
            WasKill = wasKill;
            RuntimeContext = runtimeContext;
        }
    }
}
