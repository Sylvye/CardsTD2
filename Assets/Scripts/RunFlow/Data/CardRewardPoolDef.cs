using System;
using System.Collections.Generic;
using Cards;
using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Card Reward Pool", fileName = "CardRewardPool")]
    public class CardRewardPoolDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public List<WeightedCardRewardEntry> cards = new();
        public List<WeightedAugmentRewardEntry> augments = new();
        public int choiceCount = 3;

        public string PoolId => string.IsNullOrWhiteSpace(id) ? name : id;

        public List<PendingRewardEntry> GetRandomChoices(int seed, string salt)
        {
            return GetRandomChoices(seed, salt, null);
        }

        public List<PendingRewardEntry> GetRandomChoices(int seed, string salt, Func<CardDef, bool> canIncludeCard)
        {
            List<WeightedRewardCandidate> availableRewards = BuildCandidates(canIncludeCard);
            List<PendingRewardEntry> selectedRewards = new();

            int desiredCount = Mathf.Clamp(choiceCount, 0, availableRewards.Count);
            if (desiredCount <= 0)
                return selectedRewards;

            System.Random random = new(seed ^ (salt != null ? salt.GetHashCode() : 0));
            for (int i = 0; i < desiredCount; i++)
            {
                int selectedIndex = SelectWeightedIndex(availableRewards, random);
                if (selectedIndex < 0)
                    break;

                WeightedRewardCandidate selectedCandidate = availableRewards[selectedIndex];
                selectedRewards.Add(new PendingRewardEntry
                {
                    rewardType = selectedCandidate.rewardType,
                    contentId = selectedCandidate.contentId
                });
                availableRewards.RemoveAt(selectedIndex);
            }

            return selectedRewards;
        }

        private List<WeightedRewardCandidate> BuildCandidates(Func<CardDef, bool> canIncludeCard)
        {
            Dictionary<string, WeightedRewardCandidate> candidatesByKey = new();

            if (cards != null)
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    WeightedCardRewardEntry entry = cards[i];
                    if (entry?.card == null || (canIncludeCard != null && !canIncludeCard(entry.card)))
                        continue;

                    string contentId = GetCardId(entry?.card);
                    AddCandidate(candidatesByKey, RunRewardType.Card, contentId, entry?.weight ?? 0);
                }
            }

            if (augments != null)
            {
                for (int i = 0; i < augments.Count; i++)
                {
                    WeightedAugmentRewardEntry entry = augments[i];
                    string contentId = GetAugmentId(entry?.augment);
                    AddCandidate(candidatesByKey, RunRewardType.Augment, contentId, entry?.weight ?? 0);
                }
            }

            return new List<WeightedRewardCandidate>(candidatesByKey.Values);
        }

        private static void AddCandidate(
            Dictionary<string, WeightedRewardCandidate> candidatesByKey,
            RunRewardType rewardType,
            string contentId,
            int weight)
        {
            if (string.IsNullOrWhiteSpace(contentId) || weight <= 0)
                return;

            string key = $"{(int)rewardType}:{contentId}";
            if (candidatesByKey.TryGetValue(key, out WeightedRewardCandidate existing))
            {
                existing.weight += weight;
                return;
            }

            candidatesByKey[key] = new WeightedRewardCandidate
            {
                rewardType = rewardType,
                contentId = contentId,
                weight = weight
            };
        }

        private static int SelectWeightedIndex(List<WeightedRewardCandidate> candidates, System.Random random)
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

        private static string GetCardId(CardDef card)
        {
            return card == null ? null : string.IsNullOrWhiteSpace(card.id) ? card.name : card.id;
        }

        private static string GetAugmentId(CardAugmentDef augment)
        {
            return augment == null ? null : string.IsNullOrWhiteSpace(augment.id) ? augment.name : augment.id;
        }

        private sealed class WeightedRewardCandidate
        {
            public RunRewardType rewardType;
            public string contentId;
            public int weight;
        }
    }

    [Serializable]
    public class WeightedCardRewardEntry
    {
        public CardDef card;
        public int weight = 1;
    }

    [Serializable]
    public class WeightedAugmentRewardEntry
    {
        public CardAugmentDef augment;
        public int weight = 1;
    }
}
