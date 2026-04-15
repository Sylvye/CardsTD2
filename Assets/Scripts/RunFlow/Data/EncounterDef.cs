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
    }
}
