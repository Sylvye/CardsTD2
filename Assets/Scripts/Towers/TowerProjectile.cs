using Enemies;
using UnityEngine;

namespace Towers
{
    public class TowerProjectile : MonoBehaviour
    {
        private TowerAgent ownerTower;
        private EnemyAgent target;
        private Vector3 targetPosition;
        private Vector3 travelDirection;
        private float damage;
        private float speed;
        private float hitRadius;
        private float lifetimeRemaining;
        private bool followTarget;
        private bool isInitialized;

        public void Initialize(
            TowerAgent sourceTower,
            EnemyAgent targetEnemy,
            Vector3 initialTravelDirection,
            float projectileDamage,
            float projectileSpeed,
            float projectileHitRadius,
            float lifetime,
            bool shouldFollowTarget)
        {
            ownerTower = sourceTower;
            target = targetEnemy;
            targetPosition = targetEnemy != null ? targetEnemy.transform.position : transform.position;
            travelDirection = initialTravelDirection.sqrMagnitude > 0.0001f
                ? initialTravelDirection.normalized
                : Vector3.right;
            damage = projectileDamage;
            speed = Mathf.Max(0.01f, projectileSpeed);
            hitRadius = Mathf.Max(0.01f, projectileHitRadius);
            lifetimeRemaining = Mathf.Max(0.01f, lifetime);
            followTarget = shouldFollowTarget;
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

            if (followTarget && target != null && !target.IsDeadOrEscaped)
            {
                targetPosition = target.transform.position;
                Vector3 updatedDirection = targetPosition - transform.position;
                if (updatedDirection.sqrMagnitude > 0.0001f)
                    travelDirection = updatedDirection.normalized;
            }

            if (followTarget)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            }
            else
            {
                transform.position += travelDirection * (speed * Time.deltaTime);
            }

            if (Vector2.Distance(transform.position, targetPosition) > hitRadius)
                return;

            if (target != null && !target.IsDeadOrEscaped && Vector2.Distance(transform.position, target.transform.position) <= hitRadius)
            {
                bool wasAliveBeforeHit = !target.IsDeadOrEscaped;
                target.TakeDamage(damage);
                ownerTower?.ReportHit(target, damage, transform.position);
                if (wasAliveBeforeHit && target.IsDeadOrEscaped)
                    ownerTower?.ReportKill(target, damage, transform.position);
            }

            Destroy(gameObject);
        }
    }
}
