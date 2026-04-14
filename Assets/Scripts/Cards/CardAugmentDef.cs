using System.Collections.Generic;
using Towers;
using UnityEngine;

namespace Cards
{
    [CreateAssetMenu(menuName = "Cards/Card Augment", fileName = "New Card Augment")]
    public class CardAugmentDef : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        [TextArea(2, 5)] public string description;
        public Sprite icon;

        [Header("Card Modifiers")]
        public int manaCostDelta;
        public int extraAugmentSlots;
        public List<CardEffectData> additionalCardEffects = new();

        [Header("Tower Modifiers")]
        public List<TowerStatModifierDef> additionalTowerModifiers = new();
        public List<TowerAttackModifierData> towerAttackModifiers = new();

        [Header("Spell Modifiers")]
        public List<SpellTriggeredEffect> additionalSpellEffects = new();

        [Header("Restrictions")]
        public List<CardType> allowedCardTypes = new();
        public List<string> allowedFamilyIds = new();

        public bool IsCompatible(CardDef cardDef)
        {
            if (cardDef == null)
                return false;

            if (allowedCardTypes != null && allowedCardTypes.Count > 0 && !allowedCardTypes.Contains(cardDef.type))
                return false;

            if (allowedFamilyIds != null && allowedFamilyIds.Count > 0)
            {
                string familyId = cardDef.CardFamilyId;
                if (string.IsNullOrWhiteSpace(familyId) || !allowedFamilyIds.Contains(familyId))
                    return false;
            }

            return true;
        }
    }
}
