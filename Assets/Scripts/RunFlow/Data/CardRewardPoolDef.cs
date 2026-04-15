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
        public List<CardDef> cards = new();
        public int choiceCount = 3;

        public string PoolId => string.IsNullOrWhiteSpace(id) ? name : id;

        public List<CardDef> GetRandomChoices(int seed, string salt)
        {
            List<CardDef> availableCards = new();
            if (cards != null)
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    CardDef card = cards[i];
                    if (card != null)
                        availableCards.Add(card);
                }
            }

            int desiredCount = Mathf.Clamp(choiceCount, 0, availableCards.Count);
            int combinedSeed = seed ^ (salt != null ? salt.GetHashCode() : 0);
            System.Random random = new(combinedSeed);

            for (int i = availableCards.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (availableCards[i], availableCards[swapIndex]) = (availableCards[swapIndex], availableCards[i]);
            }

            if (availableCards.Count > desiredCount)
                availableCards.RemoveRange(desiredCount, availableCards.Count - desiredCount);

            return availableCards;
        }
    }
}
