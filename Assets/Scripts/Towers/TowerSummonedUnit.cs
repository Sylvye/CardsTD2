using Enemies;
using UnityEngine;

namespace Towers
{
    /// <summary>
    /// Concrete summon archetype: autonomous turret summon.
    /// Uses live-linked parent tower stats while active.
    /// </summary>
    public class TowerSummonedUnit : MonoBehaviour, ITowerSummon
    {
        private TowerAgent parentTower;
        private EnemyManager enemyManager;
        private float lifetimeRemaining;
        private float fireTimer;
        private bool isInitialized;

        [Header("Turret Behavior")]
        [Min(0f)] public float baseRange = 2.5f;
        [Min(0f)] public float baseDamage = 1f;
        [Min(0.01f)] public float baseFireInterval = 0.75f;
        [Min(0f)] public float inheritedDamageMultiplier = 1f;

        public bool IsAlive => isInitialized && lifetimeRemaining > 0f;

        public void Initialize(TowerSummonContext context)
        {
            parentTower = context.ParentTower;
            enemyManager = context.RuntimeContext.EnemyManager;
            lifetimeRemaining = Mathf.Max(0.01f, context.LifetimeSeconds);
            fireTimer = 0f;
            isInitialized = true;
        }

        private void FixedUpdate()
        {
            if (!isInitialized)
                return;

            lifetimeRemaining -= Time.fixedDeltaTime;
            if (lifetimeRemaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            fireTimer -= Time.fixedDeltaTime;
            if (fireTimer > 0f || enemyManager == null)
                return;

            EnemyAgent target = GetFirstTargetInRange();
            if (target == null)
                return;

            float damage = Mathf.Max(0f, baseDamage + (GetInheritedDamage() * Mathf.Max(0f, inheritedDamageMultiplier)));
            target.TakeDamage(damage);
            fireTimer = Mathf.Max(0.01f, baseFireInterval);
        }

        private EnemyAgent GetFirstTargetInRange()
        {
            EnemyAgent bestTarget = null;
            float bestTrackDistance = float.MinValue;
            float range = Mathf.Max(0f, baseRange);

            foreach (EnemyAgent enemy in enemyManager.ActiveEnemies)
            {
                if (enemy == null || enemy.IsDeadOrEscaped)
                    continue;

                if (Vector2.Distance(transform.position, enemy.transform.position) > range)
                    continue;

                if (enemy.TrackDistance > bestTrackDistance)
                {
                    bestTarget = enemy;
                    bestTrackDistance = enemy.TrackDistance;
                }
            }

            return bestTarget;
        }

        private float GetInheritedDamage()
        {
            // Live-link behavior: mirrors parent tower modifiers/effects while summon is active.
            if (parentTower == null || parentTower.IsDead)
                return 0f;

            return Mathf.Max(0f, parentTower.GetResolvedStats().Damage);
        }
    }
}
