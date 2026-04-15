using System;
using System.Collections.Generic;
using Cards;
using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Shop Inventory", fileName = "ShopInventory")]
    public class ShopInventoryDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public List<ShopOfferData> offers = new();

        public string InventoryId => string.IsNullOrWhiteSpace(id) ? name : id;
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
        public int healAmount;

        public string OfferId => string.IsNullOrWhiteSpace(id) ? displayName : id;

        public string GetDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;

            return offerType switch
            {
                ShopOfferType.Card when card != null => $"Buy {card.displayName}",
                ShopOfferType.Augment when augment != null => $"Apply {augment.displayName}",
                ShopOfferType.Heal => $"Heal {healAmount}",
                _ => "Offer"
            };
        }
    }
}
