using UnityEngine;

namespace Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyManager enemyManager;
        [SerializeField] private EnemyPath enemyPath;
        [SerializeField] private EnemyAgent enemyPrefab;
        [SerializeField] private float spawnInterval = 1.5f;
        [SerializeField] private float enemySpeed = 2f;
        [SerializeField] private int spawnCount = 5;
        [SerializeField] private bool spawnOnStart = true;

        private int spawned;
        private float timer;

        private void Start()
        {
            if (spawnOnStart)
            {
                timer = spawnInterval;
            }
        }

        private void Update()
        {
            if (!spawnOnStart)
                return;

            if (spawned >= spawnCount)
                return;

            timer += Time.deltaTime;

            if (timer >= spawnInterval)
            {
                timer = 0f;
                SpawnEnemy();
            }
        }

        public void SpawnEnemy()
        {
            if (enemyPrefab is null || enemyPath is null || enemyManager is null)
                return;

            EnemyAgent enemy = Instantiate(
                enemyPrefab,
                transform.position,
                Quaternion.identity
            );

            enemy.Initialize(enemyManager, enemyPath, enemySpeed);
            spawned++;
        }
    }
}