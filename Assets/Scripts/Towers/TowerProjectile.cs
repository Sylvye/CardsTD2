using System.Collections.Generic;
using Combat;
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
        private DamageTypeDef damageType;
        private float speed;
        private float lifetimeRemaining;
        private int remainingHits;
        private readonly HashSet<EntityId> hitEnemyInstanceIds = new();
        private bool followTarget;
        private bool isInitialized;
        private bool isExpended;
        private ProjectileTrailRuntime trailRuntime;
        private readonly List<RaycastHit2D> sweptHitResults = new();

        private void Awake()
        {
            EnsureComponents();
        }

        public void Initialize(
            TowerAgent sourceTower,
            EnemyAgent targetEnemy,
            Vector3 initialTravelDirection,
            float projectileDamage,
            DamageTypeDef projectileDamageType,
            float projectileSpeed,
            float lifetime,
            bool shouldFollowTarget,
            int pierceCount,
            ProjectileTrailSettings trail)
        {
            EnsureComponents();

            ownerTower = sourceTower;
            target = targetEnemy;
            travelDirection = initialTravelDirection.sqrMagnitude > 0.0001f
                ? ((Vector2)initialTravelDirection).normalized
                : Vector2.right;
            damage = projectileDamage;
            damageType = projectileDamageType;
            speed = Mathf.Max(0.01f, projectileSpeed);
            lifetimeRemaining = Mathf.Max(0.01f, lifetime);
            remainingHits = Mathf.Max(0, pierceCount) + 1;
            hitEnemyInstanceIds.Clear();
            followTarget = shouldFollowTarget;
            isInitialized = true;
            isExpended = false;

            ConfigureTrail(trail);

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
            SweepForHits(rb.position, nextPosition);
            if (isExpended)
                return;

            rb.MovePosition(nextPosition);
            trailRuntime?.RecordPosition(nextPosition);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryApplyHit(other.GetComponentInParent<EnemyAgent>(), transform.position);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryApplyHit(collision.collider.GetComponentInParent<EnemyAgent>(), transform.position);
        }

        private void OnDestroy()
        {
            if (trailRuntime == null)
                return;

            trailRuntime.Finish();
            trailRuntime = null;
        }

        private void TryApplyHit(EnemyAgent enemy, Vector3 hitPosition)
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
            enemy.TakeDamage(damage, damageType);
            ownerTower?.ReportHit(enemy, damage, hitPosition);
            if (wasAliveBeforeHit && enemy.IsDeadOrEscaped)
                ownerTower?.ReportKill(enemy, damage, hitPosition);
        }

        private void SweepForHits(Vector2 startPosition, Vector2 endPosition)
        {
            if (projectileCollider == null || !projectileCollider.enabled)
                return;

            Vector2 displacement = endPosition - startPosition;
            float distance = displacement.magnitude;
            if (distance <= 0.0001f)
                return;

            sweptHitResults.Clear();
            int hitCount = projectileCollider.Cast(displacement / distance, ContactFilter2D.noFilter, sweptHitResults, distance);
            if (hitCount <= 0)
                return;

            sweptHitResults.Sort((a, b) => a.distance.CompareTo(b.distance));
            Vector2 direction = displacement / distance;
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hitCollider = sweptHitResults[i].collider;
                if (hitCollider == null || hitCollider == projectileCollider)
                    continue;

                Vector3 hitPosition = startPosition + (direction * sweptHitResults[i].distance);
                trailRuntime?.RecordPosition(hitPosition);
                TryApplyHit(hitCollider.GetComponentInParent<EnemyAgent>(), hitPosition);
                if (isExpended)
                    return;
            }
        }

        private void ConfigureTrail(ProjectileTrailSettings trail)
        {
            if (trailRuntime != null)
            {
                trailRuntime.Finish();
                trailRuntime = null;
            }

            if (trail == null || !trail.enabled)
                return;

            GameObject trailObject = new($"{name} Trail");
            trailRuntime = trailObject.AddComponent<ProjectileTrailRuntime>();
            trailRuntime.Initialize(trail, transform.position);
        }

        private void Expire()
        {
            isExpended = true;

            if (trailRuntime != null)
            {
                trailRuntime.Finish();
                trailRuntime = null;
            }

            if (projectileCollider != null)
                projectileCollider.enabled = false;
            if (rb != null)
                rb.simulated = false;

            Destroy(gameObject);
        }

        private void EnsureComponents()
        {
            if (rb == null)
                rb = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();

            if (projectileCollider == null)
                projectileCollider = GetComponent<Collider2D>() ?? gameObject.AddComponent<CircleCollider2D>();

            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (!projectileCollider.isTrigger)
                projectileCollider.isTrigger = true;
        }
    }

    internal sealed class ProjectileTrailRuntime : MonoBehaviour
    {
        private readonly List<Vector3> positions = new();
        private readonly List<float> ages = new();
        private LineRenderer lineRenderer;
        private float duration;
        private int maxPoints;
        private float minPointDistanceSqr;
        private bool isFinished;
        private Vector3[] positionBuffer;
        private static Material defaultMaterial;

        public void Initialize(ProjectileTrailSettings settings, Vector3 initialPosition)
        {
            settings.ClampValues();

            duration = settings.duration;
            maxPoints = settings.maxPoints;
            minPointDistanceSqr = settings.minPointDistance * settings.minPointDistance;

            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = settings.startWidth;
            lineRenderer.endWidth = settings.endWidth;
            lineRenderer.startColor = settings.startColor;
            lineRenderer.endColor = settings.endColor;
            lineRenderer.positionCount = 0;
            lineRenderer.numCapVertices = 2;
            lineRenderer.numCornerVertices = 2;
            lineRenderer.sortingLayerName = settings.sortingLayerName;
            lineRenderer.sortingOrder = settings.sortingOrder;
            Material material = settings.material != null ? settings.material : DefaultMaterial;
            if (material != null)
                lineRenderer.material = material;

            RecordPosition(initialPosition);
        }

        public void RecordPosition(Vector3 position)
        {
            if (isFinished)
                return;

            if (positions.Count > 0 && (positions[^1] - position).sqrMagnitude < minPointDistanceSqr)
                return;

            positions.Add(position);
            ages.Add(0f);
            TrimToMaxPoints();
            ApplyPositions();
        }

        public void Finish()
        {
            isFinished = true;
        }

        private void Update()
        {
            if (lineRenderer == null)
                return;

            float deltaTime = Time.deltaTime;
            for (int i = 0; i < ages.Count; i++)
                ages[i] += deltaTime;

            RemoveExpiredPoints();
            ApplyPositions();

            if (isFinished && positions.Count == 0)
                Destroy(gameObject);
        }

        private void TrimToMaxPoints()
        {
            while (positions.Count > maxPoints)
            {
                positions.RemoveAt(0);
                ages.RemoveAt(0);
            }
        }

        private void RemoveExpiredPoints()
        {
            while (ages.Count > 0 && ages[0] >= duration)
            {
                ages.RemoveAt(0);
                positions.RemoveAt(0);
            }
        }

        private void ApplyPositions()
        {
            lineRenderer.positionCount = positions.Count;
            if (positions.Count == 0)
                return;

            if (positionBuffer == null || positionBuffer.Length != positions.Count)
                positionBuffer = new Vector3[positions.Count];

            for (int i = 0; i < positions.Count; i++)
                positionBuffer[i] = positions[positions.Count - 1 - i];

            lineRenderer.SetPositions(positionBuffer);
        }

        private static Material DefaultMaterial
        {
            get
            {
                if (defaultMaterial != null)
                    return defaultMaterial;

                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                    return null;

                defaultMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                return defaultMaterial;
            }
        }
    }
}
