using System;
using System.Collections.Generic;
using Cards;
using Relics;
using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Shop Inventory", fileName = "ShopInventory")]
    public class ShopInventoryDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public int choiceCount = 3;
        public List<ShopOfferData> offers = new();

        public string InventoryId => string.IsNullOrWhiteSpace(id) ? name : id;

        public List<ShopOfferData> GetRandomOffers(int seed, string salt)
        {
            return GetRandomOffers(seed, salt, null);
        }

        public List<ShopOfferData> GetRandomOffers(int seed, string salt, Func<ShopOfferData, bool> canIncludeOffer)
        {
            List<ShopOfferData> availableOffers = GetValidOffers(canIncludeOffer);
            List<ShopOfferData> selectedOffers = new();

            int desiredCount = Mathf.Clamp(choiceCount, 0, availableOffers.Count);
            if (desiredCount <= 0)
                return selectedOffers;

            System.Random random = new(seed ^ (salt != null ? salt.GetHashCode() : 0));
            for (int i = 0; i < desiredCount; i++)
            {
                int selectedIndex = SelectWeightedIndex(availableOffers, random);
                if (selectedIndex < 0)
                    break;

                selectedOffers.Add(availableOffers[selectedIndex]);
                availableOffers.RemoveAt(selectedIndex);
            }

            return selectedOffers;
        }

        private List<ShopOfferData> GetValidOffers(Func<ShopOfferData, bool> canIncludeOffer)
        {
            List<ShopOfferData> validOffers = new();
            if (offers == null)
                return validOffers;

            for (int i = 0; i < offers.Count; i++)
            {
                ShopOfferData offer = offers[i];
                if (offer != null &&
                    offer.weight > 0 &&
                    !string.IsNullOrWhiteSpace(offer.OfferId) &&
                    (canIncludeOffer == null || canIncludeOffer(offer)))
                {
                    validOffers.Add(offer);
                }
            }

            return validOffers;
        }

        private static int SelectWeightedIndex(List<ShopOfferData> offers, System.Random random)
        {
            int totalWeight = 0;
            for (int i = 0; i < offers.Count; i++)
                totalWeight += Mathf.Max(0, offers[i].weight);

            if (totalWeight <= 0)
                return -1;

            int selectedWeight = random.Next(totalWeight);
            int runningWeight = 0;
            for (int i = 0; i < offers.Count; i++)
            {
                runningWeight += Mathf.Max(0, offers[i].weight);
                if (selectedWeight < runningWeight)
                    return i;
            }

            return offers.Count - 1;
        }
    }

    [Serializable]
    public class ShopOfferData
    {
        public string id;
        public string displayName;
        public ShopOfferType offerType;
        public int price;
        public CardDef card;
        public CardAugmentDef augment;
        public RelicDef relic;
        public int healAmount;
        public int weight = 1;

        public string OfferId => string.IsNullOrWhiteSpace(id) ? displayName : id;

        public string GetDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;

            return offerType switch
            {
                ShopOfferType.Card when card != null => $"Buy {card.displayName}",
                ShopOfferType.Augment when augment != null => $"Buy {augment.displayName}",
                ShopOfferType.Relic when relic != null => $"Buy {relic.DisplayNameOrFallback}",
                ShopOfferType.Heal => $"Heal {healAmount}",
                _ => "Offer"
            };
        }
    }
}
