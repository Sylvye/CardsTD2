using Enemies;
using UnityEngine;

namespace Towers
{
    /// <summary>
    /// Concrete summon archetype: autonomous turret summon.
    /// Uses the parent tower stat snapshot captured at spawn.
    /// </summary>
    public class TowerSummonedUnit : MonoBehaviour, ITowerSummon
    {
        private EnemyManager enemyManager;
        private float lifetimeRemaining;
        private float inheritedDamageAtSpawn;
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
            enemyManager = context.RuntimeContext.EnemyManager;
            lifetimeRemaining = Mathf.Max(0.01f, context.LifetimeSeconds);
            inheritedDamageAtSpawn = Mathf.Max(0f, context.ParentStatsSnapshot.Damage);
            fireTimer = 0f;
            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized)
                return;

            lifetimeRemaining -= Time.deltaTime;
            if (lifetimeRemaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            fireTimer -= Time.deltaTime;
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
            // Snapshot behavior: uses parent damage captured at spawn; no live-link updates afterwards.
            return inheritedDamageAtSpawn;
        }
    }
}
