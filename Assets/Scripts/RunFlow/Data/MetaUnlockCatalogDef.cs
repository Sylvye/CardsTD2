using System;
using System.Collections.Generic;
using Cards;
using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Meta Unlock Catalog", fileName = "MetaUnlockCatalog")]
    public class MetaUnlockCatalogDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public List<MetaUnlockEntry> unlocks = new();

        public string CatalogId => string.IsNullOrWhiteSpace(id) ? name : id;

        private void OnValidate()
        {
            if (unlocks == null)
                unlocks = new List<MetaUnlockEntry>();

            for (int i = 0; i < unlocks.Count; i++)
            {
                MetaUnlockEntry unlock = unlocks[i];
                if (unlock == null)
                    continue;

                unlock.cost = Mathf.Max(0, unlock.cost);
                unlock.id = string.IsNullOrWhiteSpace(unlock.id) ? string.Empty : unlock.id.Trim();
                TrimIds(unlock.prerequisiteUnlockIds);

                if (unlock.contents == null)
                    unlock.contents = new List<MetaUnlockContent>();
            }
        }

        private static void TrimIds(List<string> ids)
        {
            if (ids == null)
                return;

            for (int i = 0; i < ids.Count; i++)
                ids[i] = string.IsNullOrWhiteSpace(ids[i]) ? string.Empty : ids[i].Trim();
        }
    }

    [Serializable]
    public class MetaUnlockEntry
    {
        public string id;
        public MetaUnlockType type;
        public int cost = 1;
        public string displayName;
        [TextArea(2, 5)]
        public string description;
        public Sprite icon;
        public CardDef card;
        public List<string> prerequisiteUnlockIds = new();
        public List<MetaUnlockContent> contents = new();

        public string UnlockId => string.IsNullOrWhiteSpace(id) ? GetFallbackId() : id;

        public string GetDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;

            return type switch
            {
                MetaUnlockType.Card when card != null => card.displayName,
                MetaUnlockType.Relic => "Relic Unlock",
                MetaUnlockType.UnlockGroup => "Unlock Group",
                _ => "Unlock"
            };
        }

        public string GetDescription()
        {
            if (!string.IsNullOrWhiteSpace(description))
                return description;

            return type switch
            {
                MetaUnlockType.Card when card != null => card.description,
                MetaUnlockType.Relic => "Relic unlocks are reserved for a future progression type.",
                MetaUnlockType.UnlockGroup => "Unlocks several related rewards.",
                _ => string.Empty
            };
        }

        public Sprite GetIcon()
        {
            if (icon != null)
                return icon;

            if (type == MetaUnlockType.UnlockGroup && contents != null)
            {
                for (int i = 0; i < contents.Count; i++)
                {
                    Sprite contentIcon = contents[i]?.GetIcon();
                    if (contentIcon != null)
                        return contentIcon;
                }
            }

            return type == MetaUnlockType.Card && card != null ? card.icon : null;
        }

        private string GetFallbackId()
        {
            return type switch
            {
                MetaUnlockType.Card when card != null => $"unlock.card.{GetCardId(card)}",
                MetaUnlockType.Relic => "unlock.relic.placeholder",
                MetaUnlockType.UnlockGroup => "unlock.group.placeholder",
                _ => null
            };
        }

        private static string GetCardId(CardDef card)
        {
            return card == null ? null : string.IsNullOrWhiteSpace(card.id) ? card.name : card.id;
        }
    }

    [Serializable]
    public class MetaUnlockContent
    {
        public MetaUnlockType type;
        public string displayName;
        [TextArea(2, 5)]
        public string description;
        public Sprite icon;
        public CardDef card;

        public string GetDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;

            return type switch
            {
                MetaUnlockType.Card when card != null => card.displayName,
                MetaUnlockType.Relic => "Relic Unlock",
                MetaUnlockType.UnlockGroup => "Unlock Group",
                _ => "Unlock"
            };
        }

        public string GetDescription()
        {
            if (!string.IsNullOrWhiteSpace(description))
                return description;

            return type switch
            {
                MetaUnlockType.Card when card != null => card.description,
                MetaUnlockType.Relic => "Relic unlocks are reserved for a future progression type.",
                MetaUnlockType.UnlockGroup => "Unlocks several related rewards.",
                _ => string.Empty
            };
        }

        public Sprite GetIcon()
        {
            if (icon != null)
                return icon;

            return type == MetaUnlockType.Card && card != null ? card.icon : null;
        }
    }
}
