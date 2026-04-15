using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Encounter Pool", fileName = "EncounterPool")]
    public class EncounterPoolDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public List<WeightedEncounterEntry> encounters = new();

        public string PoolId => string.IsNullOrWhiteSpace(id) ? name : id;

        public List<WeightedEncounterEntry> GetValidEntries()
        {
            List<WeightedEncounterEntry> validEntries = new();
            if (encounters == null)
                return validEntries;

            for (int i = 0; i < encounters.Count; i++)
            {
                WeightedEncounterEntry entry = encounters[i];
                if (entry != null && entry.encounter != null && entry.weight > 0)
                    validEntries.Add(entry);
            }

            return validEntries;
        }
    }

    [Serializable]
    public class WeightedEncounterEntry
    {
        public EncounterDef encounter;
        public int weight = 1;
    }
}
