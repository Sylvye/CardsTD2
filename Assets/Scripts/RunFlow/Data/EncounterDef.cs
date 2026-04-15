using System.Collections.Generic;
using Enemies;
using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Encounter", fileName = "Encounter")]
    public class EncounterDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public EncounterKind encounterKind;
        public GameObject pathPrefab;
        public List<SpawnBatch> spawnBatches = new();
        public CardRewardPoolDef rewardPool;
        public int goldReward = 10;
        public int metaCurrencyReward = 1;

        public string EncounterId => string.IsNullOrWhiteSpace(id) ? name : id;
        public string DisplayNameOrFallback => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        public int TotalSpawnCount
        {
            get
            {
                if (spawnBatches == null)
                    return 0;

                int total = 0;
                for (int i = 0; i < spawnBatches.Count; i++)
                {
                    SpawnBatch batch = spawnBatches[i];
                    if (batch == null)
                        continue;

                    total += Mathf.Max(0, batch.spawnCount);
                }

                return total;
            }
        }

        public float EstimatedDurationSeconds
        {
            get
            {
                if (spawnBatches == null)
                    return 0f;

                float duration = 0f;
                for (int i = 0; i < spawnBatches.Count; i++)
                {
                    SpawnBatch batch = spawnBatches[i];
                    if (batch == null)
                        continue;

                    int spawnCount = Mathf.Max(0, batch.spawnCount);
                    if (spawnCount > 1)
                        duration += (spawnCount - 1) * Mathf.Max(0f, batch.spawnInterval);

                    duration += Mathf.Max(0f, batch.waitTime);
                }

                return duration;
            }
        }

        private void OnValidate()
        {
            spawnBatches ??= new List<SpawnBatch>();

            for (int i = 0; i < spawnBatches.Count; i++)
            {
                SpawnBatch batch = spawnBatches[i];
                if (batch == null)
                    continue;

                batch.spawnCount = Mathf.Max(1, batch.spawnCount);
                batch.spawnInterval = Mathf.Max(0f, batch.spawnInterval);
                batch.waitTime = Mathf.Max(0f, batch.waitTime);
            }
        }
    }
}
