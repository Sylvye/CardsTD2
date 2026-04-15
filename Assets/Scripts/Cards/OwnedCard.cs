using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cards
{
    [Serializable]
    public class OwnedCard
    {
        [SerializeField] private string uniqueId;
        [SerializeField] private CardDef currentDefinition;
        [SerializeField] private List<CardAugmentDef> appliedAugments = new();

        public string UniqueId
        {
            get
            {
                EnsureUniqueId();
                return uniqueId;
            }
        }

        public CardDef CurrentDefinition => currentDefinition;
        public IReadOnlyList<CardAugmentDef> AppliedAugments
        {
            get
            {
                if (appliedAugments == null)
                    appliedAugments = new List<CardAugmentDef>();

                return appliedAugments;
            }
        }

        public OwnedCard()
        {
            EnsureUniqueId();
        }

        public OwnedCard(CardDef definition)
        {
            currentDefinition = definition;
            EnsureUniqueId();
        }

        public OwnedCard(CardDef definition, string existingUniqueId, IEnumerable<CardAugmentDef> augments)
        {
            currentDefinition = definition;
            uniqueId = existingUniqueId;
            EnsureUniqueId();
            appliedAugments = augments != null ? new List<CardAugmentDef>(augments) : new List<CardAugmentDef>();
        }

        public void EnsureUniqueId()
        {
            if (string.IsNullOrWhiteSpace(uniqueId))
                uniqueId = Guid.NewGuid().ToString("N");
        }

        public bool CanUpgrade()
        {
            return currentDefinition != null && currentDefinition.nextUpgradeDef != null;
        }

        public bool TryUpgrade()
        {
            if (!CanUpgrade())
                return false;

            currentDefinition = currentDefinition.nextUpgradeDef;
            return true;
        }

        public int GetTotalAugmentSlots()
        {
            if (appliedAugments == null)
                appliedAugments = new List<CardAugmentDef>();

            int totalSlots = currentDefinition != null ? currentDefinition.GetBaseAugmentSlots() : 0;

            for (int i = 0; i < appliedAugments.Count; i++)
            {
                CardAugmentDef augment = appliedAugments[i];
                if (augment != null)
                    totalSlots += augment.extraAugmentSlots;
            }

            return Mathf.Max(0, totalSlots);
        }

        public bool CanApplyAugment(CardAugmentDef augment)
        {
            if (appliedAugments == null)
                appliedAugments = new List<CardAugmentDef>();

            if (augment == null || currentDefinition == null)
                return false;

            if (!augment.IsCompatible(currentDefinition))
                return false;

            if (appliedAugments.Contains(augment))
                return false;

            return appliedAugments.Count < GetTotalAugmentSlots();
        }

        public bool TryApplyAugment(CardAugmentDef augment)
        {
            if (appliedAugments == null)
                appliedAugments = new List<CardAugmentDef>();

            if (!CanApplyAugment(augment))
                return false;

            appliedAugments.Add(augment);
            return true;
        }
    }
}
