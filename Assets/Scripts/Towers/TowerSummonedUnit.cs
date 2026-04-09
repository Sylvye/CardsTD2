using Enemies;
using UnityEngine;

namespace Towers
{
    public class TowerSummonedUnit : MonoBehaviour
    {
        private EnemyManager enemyManager;
        private float lifetimeRemaining;
        private float range;
        private float damage;
        private float fireInterval;
        private float fireTimer;
        private bool isInitialized;

        public bool IsAlive => isInitialized && lifetimeRemaining > 0f;

        public void Initialize(EnemyManager manager, float lifetime, float attackRange, float attackDamage, float interval)
        {
            enemyManager = manager;
            lifetimeRemaining = Mathf.Max(0.01f, lifetime);
            range = Mathf.Max(0f, attackRange);
            damage = Mathf.Max(0f, attackDamage);
            fireInterval = Mathf.Max(0.01f, interval);
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

            target.TakeDamage(damage);
            fireTimer = fireInterval;
        }

        private EnemyAgent GetFirstTargetInRange()
        {
            EnemyAgent bestTarget = null;
            float bestTrackDistance = float.MinValue;

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
    }
}
