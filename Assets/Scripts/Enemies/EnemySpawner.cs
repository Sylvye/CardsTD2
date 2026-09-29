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
        private EnemyBuffRuntimeState enemyBuffRuntimeState;
        private EnemyDef currentBatchEnemyDef;

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
                    waitTimer = 0f;
                    isWaitingBetweenBatches = false;
                }

                return;
            }

            SpawnBatch currentBatch = spawnQueue[currentBatchIndex];

            if (currentBatch == null)
            {
                Debug.LogWarning($"EnemySpawner: invalid batch at index {currentBatchIndex}, skipping.");
                AdvanceToNextBatch();
                return;
            }

            EnemyDef enemyDef = currentBatchEnemyDef;
            if (enemyDef == null || enemyDef.prefab == null)
            {
                Debug.LogWarning($"EnemySpawner: invalid batch at index {currentBatchIndex}, skipping.");
                AdvanceToNextBatch();
                return;
            }

            spawnTimer -= deltaTime;

            if (spawnedInCurrentBatch < currentBatch.spawnCount && spawnTimer <= 0f)
            {
                SpawnEnemy(enemyDef);
                spawnedInCurrentBatch++;

                if (spawnedInCurrentBatch >= currentBatch.spawnCount)
                {
                    AdvanceToNextBatch();
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

        public void ConfigureEncounter(EncounterDef encounter, EnemyPath pathOverride, IPlayerEffects effectsOverride = null, EnemyBuffRuntimeState buffRuntimeState = null)
        {
            enemyBuffRuntimeState = buffRuntimeState;
            spawnQueue = BuildSpawnQueue(encounter != null ? encounter.spawnBatches : null, enemyBuffRuntimeState);
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
            currentBatchEnemyDef = null;
        }

        private void StartBatch(int batchIndex)
        {
            if (batchIndex < 0 || batchIndex >= spawnQueue.Count)
                return;

            SpawnBatch batch = spawnQueue[batchIndex];
            spawnedInCurrentBatch = 0;
            spawnTimer = 0f;
            waitTimer = batch != null ? Mathf.Max(0f, batch.waitTime) : 0f;
            isWaitingBetweenBatches = waitTimer > 0f;
            currentBatchEnemyDef = ResolveBatchEnemyDef(batchIndex, batch);
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

            enemy.Initialize(enemyManager, this, playerEffects, enemyPath, enemyDef, trackDistance, enemyBuffRuntimeState);
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

        private static List<SpawnBatch> BuildSpawnQueue(IReadOnlyList<SpawnBatch> source, EnemyBuffRuntimeState buffRuntimeState)
        {
            List<SpawnBatch> batches = new();
            AppendBatches(batches, source, buffRuntimeState);

            return batches;
        }

        private static void AppendBatches(List<SpawnBatch> destination, IReadOnlyList<SpawnBatch> source, EnemyBuffRuntimeState buffRuntimeState)
        {
            if (destination == null || source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                SpawnBatch batch = source[i];
                if (batch == null)
                    continue;

                destination.Add(new SpawnBatch
                {
                    mode = batch.mode,
                    enemyDef = batch.enemyDef,
                    spawnCount = buffRuntimeState != null
                        ? buffRuntimeState.ResolveSpawnCount(batch.spawnCount)
                        : Mathf.Max(1, batch.spawnCount),
                    spawnInterval = batch.spawnInterval,
                    waitTime = batch.waitTime
                });
            }
        }

        private EnemyDef ResolveBatchEnemyDef(int batchIndex, SpawnBatch batch)
        {
            if (batch == null)
                return null;

            if (batch.mode == SpawnBatchMode.ElitePoolEnemy)
                return ResolveEliteEnemyDef(batchIndex);

            return batch.enemyDef;
        }

        private EnemyDef ResolveEliteEnemyDef(int batchIndex)
        {
            IReadOnlyList<EnemyDef> unlockedEliteEnemies = enemyBuffRuntimeState?.UnlockedEliteEnemies;
            if (unlockedEliteEnemies == null || unlockedEliteEnemies.Count == 0)
                return null;

            int seed = enemyBuffRuntimeState != null ? enemyBuffRuntimeState.EliteSelectionSeed : 0;
            int selectedIndex = Mathf.Abs(seed + batchIndex) % unlockedEliteEnemies.Count;
            return unlockedEliteEnemies[selectedIndex];
        }
    }
}
