namespace Cards
{
    using System;
    using UnityEngine;

    [Serializable]
    public class CardInstance
    {
        [SerializeField] private CardDef definition;
        [SerializeField] private int currentManaCost;
        [SerializeField] private int tier;
        [SerializeField] private int filledAugmentSlots;
        [SerializeField] private int uniqueRuntimeId;

        // properties
        public CardDef Definition => definition;
        public int CurrentManaCost => currentManaCost;
        public int Tier => tier;
        public int FilledAugmentSlots => filledAugmentSlots;
        public int UniqueRuntimeId => uniqueRuntimeId;
        
        
        public string DisplayName => definition != null ? definition.displayName : "NULL CARD";
        public CardType Type => definition != null ? definition.type : default;
        public string Description => definition != null ? definition.description : "";
        public Sprite Icon => definition != null ? definition.icon : null;

        public CardInstance(CardDef definition, int runtimeId)
        {
            this.definition = definition;
            uniqueRuntimeId = runtimeId;

            if (definition != null)
            {
                currentManaCost = definition.baseManaCost;
                tier = definition.baseTier;
                filledAugmentSlots = 0;
            }
        }

        public void SetManaCost(int newCost)
        {
            currentManaCost = Mathf.Max(0, newCost);
        }

        public void ModifyManaCost(int amount)
        {
            currentManaCost = Mathf.Max(0, currentManaCost + amount);
        }

        public void SetTier(int newTier)
        {
            tier = Mathf.Max(1, newTier);
        }

        public void IncreaseTier(int amount = 1)
        {
            tier = Mathf.Max(1, tier + amount);
        }

        public void FillAugmentSlot(int amount = 1)
        {
            filledAugmentSlots = Mathf.Max(0, filledAugmentSlots + amount);
        }

        public void ResetForBattle()
        {
            if (definition == null) return;

            currentManaCost = definition.baseManaCost;
        }

        public override string ToString()
        {
            return $"{DisplayName} (Cost: {currentManaCost}, Tier: {tier}, ID: {uniqueRuntimeId})";
        }
    }
}