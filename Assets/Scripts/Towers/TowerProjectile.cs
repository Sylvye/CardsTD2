using System.Collections.Generic;
using Enemies;
using UnityEngine;

namespace Towers
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class TowerProjectile : MonoBehaviour
    {
        private TowerAgent ownerTower;
        private EnemyAgent target;
        private Rigidbody2D rb;
        private Collider2D projectileCollider;
        private Vector2 travelDirection;
        private float damage;
        private float speed;
        private float lifetimeRemaining;
        private int remainingHits;
        private readonly HashSet<EntityId> hitEnemyInstanceIds = new();
        private bool followTarget;
        private bool isInitialized;
        private bool isExpended;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            projectileCollider = GetComponent<Collider2D>();

            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (!projectileCollider.isTrigger)
                projectileCollider.isTrigger = true;
        }

        public void Initialize(
            TowerAgent sourceTower,
            EnemyAgent targetEnemy,
            Vector3 initialTravelDirection,
            float projectileDamage,
            float projectileSpeed,
            float lifetime,
            bool shouldFollowTarget,
            int pierceCount)
        {
            ownerTower = sourceTower;
            target = targetEnemy;
            travelDirection = initialTravelDirection.sqrMagnitude > 0.0001f
                ? ((Vector2)initialTravelDirection).normalized
                : Vector2.right;
            damage = projectileDamage;
            speed = Mathf.Max(0.01f, projectileSpeed);
            lifetimeRemaining = Mathf.Max(0.01f, lifetime);
            remainingHits = Mathf.Max(0, pierceCount) + 1;
            hitEnemyInstanceIds.Clear();
            followTarget = shouldFollowTarget;
            isInitialized = true;
            isExpended = false;

            if (projectileCollider != null)
                projectileCollider.enabled = true;
            if (rb != null)
                rb.simulated = true;
        }

        private void FixedUpdate()
        {
            if (!isInitialized)
                return;

            lifetimeRemaining -= Time.fixedDeltaTime;
            if (lifetimeRemaining <= 0f)
            {
                Expire();
                return;
            }

            if (followTarget && target != null && !target.IsDeadOrEscaped)
            {
                Vector2 updatedDirection = (Vector2)target.transform.position - rb.position;
                if (updatedDirection.sqrMagnitude > 0.0001f)
                    travelDirection = updatedDirection.normalized;
            }

            Vector2 nextPosition = rb.position + (travelDirection * (speed * Time.fixedDeltaTime));
            rb.MovePosition(nextPosition);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryApplyHit(other.GetComponentInParent<EnemyAgent>());
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryApplyHit(collision.collider.GetComponentInParent<EnemyAgent>());
        }

        private void TryApplyHit(EnemyAgent enemy)
        {
            if (!isInitialized || isExpended || remainingHits <= 0)
                return;
            if (enemy == null || enemy.IsDeadOrEscaped)
                return;
            if (!hitEnemyInstanceIds.Add(enemy.GetEntityId()))
                return;

            remainingHits--;
            if (remainingHits <= 0)
                Expire();

            bool wasAliveBeforeHit = !enemy.IsDeadOrEscaped;
            enemy.TakeDamage(damage);
            ownerTower?.ReportHit(enemy, damage, transform.position);
            if (wasAliveBeforeHit && enemy.IsDeadOrEscaped)
                ownerTower?.ReportKill(enemy, damage, transform.position);
        }

        private void Expire()
        {
            isExpended = true;

            if (projectileCollider != null)
                projectileCollider.enabled = false;
            if (rb != null)
                rb.simulated = false;

            Destroy(gameObject);
        }
    }
}
