using System.Collections.Generic;
using Relics;
using Towers;
using UnityEngine;

namespace Cards
{
    public static class CardRuntimeResolver
    {
        public static ResolvedCardData Build(OwnedCard ownedCard)
        {
            return Build(ownedCard, null);
        }

        public static ResolvedCardData Build(OwnedCard ownedCard, IReadOnlyList<OwnedRelic> activeRelics)
        {
            CardDef definition = ownedCard != null ? ownedCard.CurrentDefinition : null;
            if (definition == null)
                return null;

            int manaCost = definition.baseManaCost;
            int augmentSlots = definition.GetBaseAugmentSlots();
            List<CardEffectData> effects = CloneCardEffects(definition.effects);
            List<Sprite> augmentIcons = new();
            List<TowerStatModifierDef> towerModifiers = new();
            List<TowerAttackModifierData> towerAttackModifiers = new();
            List<SpellTriggeredEffect> spellEffects = null;

            IReadOnlyList<CardAugmentDef> augments = ownedCard.AppliedAugments;
            for (int i = 0; i < augments.Count; i++)
            {
                CardAugmentDef augment = augments[i];
                if (augment == null)
                    continue;

                if (augment.icon != null)
                    augmentIcons.Add(augment.icon);

                manaCost += augment.manaCostDelta;
                augmentSlots += augment.extraAugmentSlots;
                AppendCardEffects(effects, augment.additionalCardEffects);
                AppendTowerModifiers(towerModifiers, augment.additionalTowerModifiers);
                AppendAttackModifiers(towerAttackModifiers, augment.towerAttackModifiers);

                if (augment.additionalSpellEffects != null && augment.additionalSpellEffects.Count > 0)
                {
                    if (spellEffects == null)
                        spellEffects = CloneSpellEffects((definition.spawnableObject as SpellDef)?.triggeredEffects);

                    AppendSpellEffects(spellEffects, augment.additionalSpellEffects);
                }
            }

            RelicCardModificationContext relicContext = new(
                ownedCard,
                definition,
                manaCost,
                augmentSlots,
                effects,
                towerModifiers,
                towerAttackModifiers);
            RelicResolver.ModifyCard(activeRelics, relicContext);
            manaCost = relicContext.ManaCost;
            augmentSlots = relicContext.AugmentSlots;

            if (relicContext.AdditionalSpellEffects.Count > 0)
            {
                if (spellEffects == null)
                    spellEffects = CloneSpellEffects((definition.spawnableObject as SpellDef)?.triggeredEffects);

                AppendSpellEffects(spellEffects, relicContext.AdditionalSpellEffects);
            }

            manaCost = Mathf.Max(0, manaCost);
            augmentSlots = Mathf.Max(0, augmentSlots);

            SpawnableObjectDef resolvedSpawnable = definition.spawnableObject;
            TowerDef resolvedTowerDef = null;
            SpellDef resolvedSpellDef = null;
            IReadOnlyList<TowerAttackDef> runtimeTowerAttacks = null;

            if (definition.spawnableObject is TowerDef towerDef)
            {
                resolvedTowerDef = towerDef;

                if (towerAttackModifiers.Count > 0)
                    runtimeTowerAttacks = BuildRuntimeTowerAttacks(towerDef, towerAttackModifiers);
            }
            else if (definition.spawnableObject is SpellDef spellDef)
            {
                if (spellEffects != null)
                {
                    resolvedSpellDef = Object.Instantiate(spellDef);
                    resolvedSpellDef.triggeredEffects = spellEffects;
                    resolvedSpawnable = resolvedSpellDef;
                }
                else
                {
                    resolvedSpellDef = spellDef;
                }
            }

            return new ResolvedCardData(
                definition,
                definition.displayName,
                definition.description,
                definition.icon,
                augmentIcons,
                definition.type,
                manaCost,
                definition.GetUpgradeTier(),
                augmentSlots,
                effects,
                resolvedSpawnable,
                resolvedTowerDef,
                resolvedSpellDef,
                towerModifiers,
                runtimeTowerAttacks
            );
        }

        private static List<CardEffectData> CloneCardEffects(IReadOnlyList<CardEffectData> source)
        {
            List<CardEffectData> cloned = new();
            AppendCardEffects(cloned, source);
            return cloned;
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

        private static List<TowerAttackDef> BuildRuntimeTowerAttacks(TowerDef towerDef, IReadOnlyList<TowerAttackModifierData> modifiers)
        {
            if (towerDef == null || towerDef.attacks == null || towerDef.attacks.Count == 0)
                return null;

            List<TowerAttackDef> runtimeAttacks = new();
            for (int i = 0; i < towerDef.attacks.Count; i++)
            {
                TowerAttackDef attackDef = towerDef.attacks[i];
                if (attackDef == null)
                    continue;

                TowerAttackDef runtimeAttack = Object.Instantiate(attackDef);
                for (int modifierIndex = 0; modifierIndex < modifiers.Count; modifierIndex++)
                {
                    TowerAttackModifierData modifier = modifiers[modifierIndex];
                    if (modifier != null && modifier.Matches(runtimeAttack))
                        modifier.ApplyTo(runtimeAttack);
                }

                runtimeAttacks.Add(runtimeAttack);
            }

            return runtimeAttacks;
        }

        private static List<SpellTriggeredEffect> CloneSpellEffects(IReadOnlyList<SpellTriggeredEffect> source)
        {
            List<SpellTriggeredEffect> cloned = new();
            AppendSpellEffects(cloned, source);
            return cloned;
        }

        private static void AppendSpellEffects(List<SpellTriggeredEffect> target, IReadOnlyList<SpellTriggeredEffect> source)
        {
            if (target == null || source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                SpellTriggeredEffect effect = source[i];
                if (effect != null)
                    target.Add(effect.Clone());
            }
        }
    }
}
