using Combat;
using UnityEngine;

namespace Enemies
{
    [RequireComponent(typeof(PathFollower))]
    public class EnemyAgent : MonoBehaviour
    {
        private PathFollower pathFollower;
        private EnemyManager enemyManager;
        private EnemySpawner enemySpawner;
        private CombatSessionDriver combatSessionDriver;
        private EnemyDef enemyDef;
        private EnemyEffectResolver effectResolver;

        private float maxHealth;
        private float currentHealth;
        private int lifeDamage;

        private bool isInitialized;
        private bool isDeadOrEscaped;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public int LifeDamage => lifeDamage;
        public bool IsDeadOrEscaped => isDeadOrEscaped;
        public EnemyDef Definition => enemyDef;
        public float TrackDistance => pathFollower != null ? pathFollower.DistanceTravelled : 0f;

        private void Awake()
        {
            pathFollower = GetComponent<PathFollower>();
            effectResolver = new EnemyEffectResolver();
        }

        public void Initialize(
            EnemyManager manager,
            EnemySpawner spawner,
            CombatSessionDriver driver,
            EnemyPath path,
            EnemyDef def,
            float startingTrackDistance = 0f)
        {
            enemyManager = manager;
            enemySpawner = spawner;
            combatSessionDriver = driver;
            enemyDef = def;

            isDeadOrEscaped = false;
            isInitialized = true;

            maxHealth = def.maxHealth;
            currentHealth = maxHealth;
            lifeDamage = def.lifeDamage;

            if (pathFollower == null)
                pathFollower = GetComponent<PathFollower>();

            pathFollower.SetPath(path, startingTrackDistance);
            pathFollower.SetSpeed(def.moveSpeed);

            enemyManager?.RegisterEnemy(this);

            FireTrigger(EnemyTriggerType.OnSpawn);
        }

        private void Update()
        {
            if (!isInitialized || isDeadOrEscaped || pathFollower == null)
                return;

            if (pathFollower.ReachedEnd)
            {
                Escape();
            }
        }

        public void TakeDamage(float amount)
        {
            if (isDeadOrEscaped || amount <= 0f)
                return;

            currentHealth -= amount;

            FireTrigger(EnemyTriggerType.OnHit);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDeadOrEscaped)
                return;

            FireTrigger(EnemyTriggerType.OnDeath);

            isDeadOrEscaped = true;
            enemyManager?.UnregisterEnemy(this);
            Destroy(gameObject);
        }

        private void Escape()
        {
            if (isDeadOrEscaped)
                return;

            FireTrigger(EnemyTriggerType.OnExit);

            isDeadOrEscaped = true;
            enemyManager?.HandleEnemyEscaped(this);
            enemyManager?.UnregisterEnemy(this);
            Destroy(gameObject);
        }

        private void FireTrigger(EnemyTriggerType trigger)
        {
            if (enemyDef == null || effectResolver == null)
                return;

            EnemyEffectContext context = new(
                this,
                enemyManager,
                enemySpawner,
                combatSessionDriver,
                transform.position,
                TrackDistance
            );

            effectResolver.ResolveEffectsForTrigger(enemyDef, trigger, context);
        }
    }
}
