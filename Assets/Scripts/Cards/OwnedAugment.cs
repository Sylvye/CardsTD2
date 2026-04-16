using System;
using UnityEngine;

namespace Cards
{
    [Serializable]
    public class OwnedAugment
    {
        [SerializeField] private string uniqueId;
        [SerializeField] private CardAugmentDef definition;

        public string UniqueId
        {
            get
            {
                EnsureUniqueId();
                return uniqueId;
            }
        }

        public CardAugmentDef Definition => definition;

        public OwnedAugment()
        {
            EnsureUniqueId();
        }

        public OwnedAugment(CardAugmentDef augmentDefinition)
        {
            definition = augmentDefinition;
            EnsureUniqueId();
        }

        public OwnedAugment(CardAugmentDef augmentDefinition, string existingUniqueId)
        {
            definition = augmentDefinition;
            uniqueId = existingUniqueId;
            EnsureUniqueId();
        }

        public void EnsureUniqueId()
        {
            if (string.IsNullOrWhiteSpace(uniqueId))
                uniqueId = Guid.NewGuid().ToString("N");
        }
    }
}
