using System.Collections.Generic;
using Combat;
using RunFlow;
using UnityEngine;

namespace Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyManager enemyManager;
        [SerializeField] private EnemyPath enemyPath;
        [SerializeField] private MonoBehaviour playerEffectsSource;
        [SerializeField] private Transform enemyParent;

        [Header("Wave Data")]
        [SerializeField] private List<SpawnBatch> spawnQueue = new();

        [Header("Runtime")]
        [SerializeField] private bool startOnPlay = true;

        private int currentBatchIndex = -1;
        private int spawnedInCurrentBatch = 0;
        private float spawnTimer = 0f;
        private float waitTimer = 0f;
        private bool isWaitingBetweenBatches = false;
        private bool isRunning = false;
        private IPlayerEffects playerEffects;

        public bool IsRunning => isRunning;
        public bool IsFinished => isRunning && currentBatchIndex >= spawnQueue.Count;
        public bool HasFinishedSpawning => isRunning && currentBatchIndex >= spawnQueue.Count;

        private void Start()
        {
            ResolvePlayerEffects();

            if (startOnPlay)
            {
                Begin();
            }
        }

        private void FixedUpdate()
        {
            if (!isRunning)
                return;

            if (currentBatchIndex >= spawnQueue.Count)
                return;

            float deltaTime = Time.fixedDeltaTime;

            if (isWaitingBetweenBatches)
            {
                waitTimer -= deltaTime;

                if (waitTimer <= 0f)
                {
                    AdvanceToNextBatch();
                }

                return;
            }

            SpawnBatch currentBatch = spawnQueue[currentBatchIndex];

            if (currentBatch == null || currentBatch.enemyDef == null || currentBatch.enemyDef.prefab == null)
            {
                Debug.LogWarning($"EnemySpawner: invalid batch at index {currentBatchIndex}, skipping.");
                FinishCurrentBatchAndWait(0f);
                return;
            }

            spawnTimer -= deltaTime;

            if (spawnedInCurrentBatch < currentBatch.spawnCount && spawnTimer <= 0f)
            {
                SpawnEnemy(currentBatch.enemyDef);
                spawnedInCurrentBatch++;

                if (spawnedInCurrentBatch >= currentBatch.spawnCount)
                {
                    FinishCurrentBatchAndWait(currentBatch.waitTime);
                }
                else
                {
                    spawnTimer = currentBatch.spawnInterval;
                }
            }
        }

        public void Begin()
        {
            ResolvePlayerEffects();

            if (spawnQueue.Count == 0)
            {
                Debug.LogWarning("EnemySpawner: spawn queue is empty.");
                return;
            }

            isRunning = true;
            currentBatchIndex = 0;
            StartBatch(currentBatchIndex);
        }

        public void ConfigureEncounter(EncounterDef encounter, EnemyPath pathOverride, IPlayerEffects effectsOverride = null)
        {
            spawnQueue = CloneSpawnQueue(encounter != null ? encounter.spawnBatches : null);
            enemyPath = pathOverride;
            if (effectsOverride != null)
                playerEffects = effectsOverride;

            ResetSpawner();
        }

        public void Stop()
        {
            isRunning = false;
        }

        public void ResetSpawner()
        {
            isRunning = false;
            currentBatchIndex = -1;
            spawnedInCurrentBatch = 0;
            spawnTimer = 0f;
            waitTimer = 0f;
            isWaitingBetweenBatches = false;
        }

        private void StartBatch(int batchIndex)
        {
            if (batchIndex < 0 || batchIndex >= spawnQueue.Count)
                return;

            spawnedInCurrentBatch = 0;
            spawnTimer = 0f;
            waitTimer = 0f;
            isWaitingBetweenBatches = false;
        }

        private void FinishCurrentBatchAndWait(float waitTime)
        {
            isWaitingBetweenBatches = true;
            waitTimer = waitTime;
        }

        private void AdvanceToNextBatch()
        {
            currentBatchIndex++;

            if (currentBatchIndex >= spawnQueue.Count)
                return;

            StartBatch(currentBatchIndex);
        }

        private void SpawnEnemy(EnemyDef enemyDef)
        {
            SpawnEnemyNow(enemyDef, 0f);
        }

        public void SpawnEnemyNow(EnemyDef enemyDef, float trackDistance)
        {
            if (enemyDef == null || enemyDef.prefab == null || enemyPath == null || enemyManager == null)
                return;

            EnemyAgent enemy = Instantiate(
                enemyDef.prefab,
                transform.position,
                Quaternion.identity,
                enemyParent
            );

            enemy.Initialize(enemyManager, this, playerEffects, enemyPath, enemyDef, trackDistance);
        }

        private void ResolvePlayerEffects()
        {
            if (playerEffects != null)
                return;

            playerEffects = playerEffectsSource as IPlayerEffects;

            if (playerEffectsSource != null && playerEffects == null)
            {
                Debug.LogError($"{nameof(EnemySpawner)} requires {nameof(playerEffectsSource)} to implement {nameof(IPlayerEffects)}.");
            }
        }

        private static List<SpawnBatch> CloneSpawnQueue(IReadOnlyList<SpawnBatch> source)
        {
            List<SpawnBatch> batches = new();
            if (source == null)
                return batches;

            for (int i = 0; i < source.Count; i++)
            {
                SpawnBatch batch = source[i];
                if (batch == null)
                    continue;

                batches.Add(new SpawnBatch
                {
                    enemyDef = batch.enemyDef,
                    spawnCount = batch.spawnCount,
                    spawnInterval = batch.spawnInterval,
                    waitTime = batch.waitTime
                });
            }

            return batches;
        }
    }
}
