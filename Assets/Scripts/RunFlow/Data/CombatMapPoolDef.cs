using System;
using System.Collections.Generic;
using Enemies;
using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Enemy Path Pool", fileName = "EnemyPathPool")]
    public class CombatMapPoolDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public List<WeightedEnemyPathEntry> paths = new();

        public string PoolId => string.IsNullOrWhiteSpace(id) ? name : id;

        public List<WeightedEnemyPathEntry> GetValidEntries()
        {
            List<WeightedEnemyPathEntry> validEntries = new();
            if (paths == null)
                return validEntries;

            for (int i = 0; i < paths.Count; i++)
            {
                WeightedEnemyPathEntry entry = paths[i];
                if (entry != null && entry.pathPrefab != null && entry.weight > 0)
                    validEntries.Add(entry);
            }

            return validEntries;
        }
    }

    [Serializable]
    public class WeightedEnemyPathEntry
    {
        public EnemyPath pathPrefab;
        public int weight = 1;
    }
}
