using UnityEngine;

namespace Enemies
{
    [RequireComponent(typeof(PathFollower))]
    public class EnemyAgent : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 10f;
        [SerializeField] private int lifeDamage = 1;
        
        private PathFollower pathFollower;
        private EnemyManager enemyManager;
        private float currentHealth;
        private bool isInitialized;
        private bool isDeadOrEscaped;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public int LifeDamage => lifeDamage;
        public bool IsDeadOrEscaped => isDeadOrEscaped;

        private void Awake()
        {
            pathFollower = GetComponent<PathFollower>();
            currentHealth = maxHealth;
        }

        public void Initialize(EnemyManager manager, EnemyPath path, float speed)
        {
            enemyManager = manager;
            currentHealth = maxHealth;
            isDeadOrEscaped = false;
            isInitialized = true;

            if (pathFollower == null)
                pathFollower = GetComponent<PathFollower>();

            pathFollower.SetPath(path, 0f);
            pathFollower.SetSpeed(speed);

            enemyManager?.RegisterEnemy(this);
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

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDeadOrEscaped)
                return;

            isDeadOrEscaped = true;
            enemyManager?.UnregisterEnemy(this);
            Destroy(gameObject);
        }

        private void Escape()
        {
            if (isDeadOrEscaped)
                return;

            isDeadOrEscaped = true;
            enemyManager?.HandleEnemyEscaped(this);
            enemyManager?.UnregisterEnemy(this);
            Destroy(gameObject);
        }

        private void SetSpeed(float speed)
        {
            var follower = GetComponent<PathFollower>();
            if (follower != null)
            {
                // temporary until PathFollower exposes a setter
                typeof(PathFollower)
                    .GetField("speed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(follower, speed);
            }
        }
    }
}