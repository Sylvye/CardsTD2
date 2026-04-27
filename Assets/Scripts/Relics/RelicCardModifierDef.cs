using System.Collections.Generic;
using Cards;
using Towers;
using UnityEngine;

namespace Relics
{
    [CreateAssetMenu(menuName = "Relics/Effects/Card Modifier", fileName = "RelicCardModifier")]
    public class RelicCardModifierDef : RelicEffectDef
    {
        [Header("Restrictions")]
        public List<CardType> allowedCardTypes = new();
        public List<string> allowedFamilyIds = new();

        [Header("Card Modifiers")]
        public int manaCostDelta;
        public int extraAugmentSlots;
        public List<CardEffectData> additionalCardEffects = new();

        [Header("Tower Modifiers")]
        public List<TowerStatModifierDef> additionalTowerModifiers = new();
        public List<TowerAttackModifierData> towerAttackModifiers = new();

        [Header("Spell Modifiers")]
        public List<SpellTriggeredEffect> additionalSpellEffects = new();

        public override void ModifyCard(RelicCardModificationContext context)
        {
            if (context == null || !IsCompatible(context.Definition))
                return;

            context.ManaCost = Mathf.Max(0, context.ManaCost + manaCostDelta);
            context.AugmentSlots = Mathf.Max(0, context.AugmentSlots + extraAugmentSlots);
            AppendCardEffects(context.CardEffects, additionalCardEffects);
            AppendTowerModifiers(context.TowerModifiers, additionalTowerModifiers);
            AppendAttackModifiers(context.TowerAttackModifiers, towerAttackModifiers);
            AppendSpellEffects(context.AdditionalSpellEffects, additionalSpellEffects);
        }

        private bool IsCompatible(CardDef cardDef)
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

        private static void AppendCardEffects(List<CardEffectData> target, IReadOnlyList<CardEffectData> source)
        {
            if (target == null || source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                CardEffectData effect = source[i];
                if (effect != null)
                    target.Add(effect.Clone());
            }
        }

        private static void AppendTowerModifiers(List<TowerStatModifierDef> target, IReadOnlyList<TowerStatModifierDef> source)
        {
            if (target == null || source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                TowerStatModifierDef modifier = source[i];
                if (modifier != null)
                    target.Add(modifier);
            }
        }

        private static void AppendAttackModifiers(List<TowerAttackModifierData> target, IReadOnlyList<TowerAttackModifierData> source)
        {
            if (target == null || source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                TowerAttackModifierData modifier = source[i];
                if (modifier != null)
                    target.Add(modifier);
            }
        }

        private static void AppendSpellEffects(List<SpellTriggeredEffect> target, IReadOnlyList<SpellTriggeredEffect> source)
        {
            if (target == null || source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                SpellTriggeredEffect effect = source[i];
                if (effect != null)
                    target.Add(effect);
            }
        }
    }
}
