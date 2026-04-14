namespace Cards
{
    using System;
    using UnityEngine;

    [Serializable]
    public class CardInstance
    {
        [SerializeField] private OwnedCard ownedCard;
        [SerializeField] private int currentManaCost;
        [SerializeField] private int uniqueRuntimeId;
        [NonSerialized] private ResolvedCardData resolvedData;

        // properties
        public OwnedCard OwnedCard => ownedCard;
        public CardDef Definition => resolvedData != null ? resolvedData.Definition : ownedCard != null ? ownedCard.CurrentDefinition : null;
        public ResolvedCardData ResolvedData => resolvedData;
        public int CurrentManaCost => currentManaCost;
        public int Tier => resolvedData != null ? resolvedData.Tier : 1;
        public int FilledAugmentSlots => ownedCard != null && ownedCard.AppliedAugments != null ? ownedCard.AppliedAugments.Count : 0;
        public int TotalAugmentSlots => resolvedData != null ? resolvedData.AugmentSlots : 0;
        public int UniqueRuntimeId => uniqueRuntimeId;
        
        
        public string DisplayName => resolvedData != null ? resolvedData.DisplayName : "NULL CARD";
        public CardType Type => resolvedData != null ? resolvedData.Type : default;
        public string Description => resolvedData != null ? resolvedData.Description : "";
        public Sprite Icon => resolvedData != null ? resolvedData.Icon : null;

        public CardInstance(CardDef definition, int runtimeId)
            : this(definition != null ? new OwnedCard(definition) : null, runtimeId)
        {
        }

        public CardInstance(OwnedCard ownedCard, int runtimeId)
        {
            this.ownedCard = ownedCard;
            uniqueRuntimeId = runtimeId;
            RefreshResolvedData();
        }

        public void SetManaCost(int newCost)
        {
            currentManaCost = Mathf.Max(0, newCost);
        }

        public void ModifyManaCost(int amount)
        {
            currentManaCost = Mathf.Max(0, currentManaCost + amount);
        }

        public bool TryUpgrade()
        {
            if (ownedCard == null || !ownedCard.TryUpgrade())
                return false;

            RefreshResolvedData();
            return true;
        }

        public bool TryApplyAugment(CardAugmentDef augment)
        {
            if (ownedCard == null || !ownedCard.TryApplyAugment(augment))
                return false;

            RefreshResolvedData();
            return true;
        }

        public void ResetForBattle()
        {
            if (resolvedData == null)
                RefreshResolvedData();

            currentManaCost = resolvedData != null ? resolvedData.ManaCost : 0;
        }

        public void RefreshResolvedData()
        {
            resolvedData = CardRuntimeResolver.Build(ownedCard);
            currentManaCost = resolvedData != null ? resolvedData.ManaCost : 0;
        }

        public override string ToString()
        {
            return $"{DisplayName} (Cost: {currentManaCost}, Tier: {Tier}, ID: {uniqueRuntimeId})";
        }
    }
}
