using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Enemy Buff Pool", fileName = "EnemyBuffPool")]
    public class EnemyBuffPoolDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public int choiceCount = 3;
        public List<WeightedEnemyBuffEntry> buffs = new();

        public string PoolId => string.IsNullOrWhiteSpace(id) ? name : id;

        public List<PendingEnemyBuffChoiceEntry> GetRandomChoices(int seed, string salt, int overrideChoiceCount = -1)
        {
            List<WeightedEnemyBuffEntry> availableBuffs = BuildCandidates();
            List<PendingEnemyBuffChoiceEntry> selectedBuffs = new();

            int desiredCount = Mathf.Clamp(overrideChoiceCount >= 0 ? overrideChoiceCount : choiceCount, 0, availableBuffs.Count);
            if (desiredCount <= 0)
                return selectedBuffs;

            System.Random random = new(seed ^ (salt != null ? salt.GetHashCode() : 0));
            for (int i = 0; i < desiredCount; i++)
            {
                int selectedIndex = SelectWeightedIndex(availableBuffs, random);
                if (selectedIndex < 0)
                    break;

                WeightedEnemyBuffEntry selectedEntry = availableBuffs[selectedIndex];
                selectedBuffs.Add(new PendingEnemyBuffChoiceEntry
                {
                    buffId = GetBuffId(selectedEntry.buff)
                });
                availableBuffs.RemoveAt(selectedIndex);
            }

            return selectedBuffs;
        }

        private List<WeightedEnemyBuffEntry> BuildCandidates()
        {
            Dictionary<string, WeightedEnemyBuffEntry> candidatesById = new();
            if (buffs == null)
                return new List<WeightedEnemyBuffEntry>();

            for (int i = 0; i < buffs.Count; i++)
            {
                WeightedEnemyBuffEntry entry = buffs[i];
                if (entry?.buff == null || entry.weight <= 0)
                    continue;

                string buffId = GetBuffId(entry.buff);
                if (string.IsNullOrWhiteSpace(buffId))
                    continue;

                if (candidatesById.TryGetValue(buffId, out WeightedEnemyBuffEntry existing))
                {
                    existing.weight += entry.weight;
                    continue;
                }

                candidatesById[buffId] = new WeightedEnemyBuffEntry
                {
                    buff = entry.buff,
                    weight = entry.weight
                };
            }

            return new List<WeightedEnemyBuffEntry>(candidatesById.Values);
        }

        private static int SelectWeightedIndex(List<WeightedEnemyBuffEntry> candidates, System.Random random)
        {
            int totalWeight = 0;
            for (int i = 0; i < candidates.Count; i++)
                totalWeight += Mathf.Max(0, candidates[i].weight);

            if (totalWeight <= 0)
                return -1;

            int selectedWeight = random.Next(totalWeight);
            int runningWeight = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                runningWeight += Mathf.Max(0, candidates[i].weight);
                if (selectedWeight < runningWeight)
                    return i;
            }

            return candidates.Count - 1;
        }

        private static string GetBuffId(EnemyBuffDef buff)
        {
            return buff == null ? null : buff.BuffId;
        }
    }

    [Serializable]
    public class WeightedEnemyBuffEntry
    {
        public EnemyBuffDef buff;
        public int weight = 1;
    }
}
