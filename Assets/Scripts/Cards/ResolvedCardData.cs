using System.Collections.Generic;
using Towers;
using UnityEngine;

namespace Cards
{
    public class ResolvedCardData
    {
        public CardDef Definition { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Sprite Icon { get; }
        public IReadOnlyList<Sprite> AugmentIcons { get; }
        public CardType Type { get; }
        public int ManaCost { get; }
        public int Tier { get; }
        public int AugmentSlots { get; }
        public IReadOnlyList<CardEffectData> Effects { get; }
        public SpawnableObjectDef SpawnableObject { get; }
        public TowerDef TowerDefinition { get; }
        public SpellDef SpellDefinition { get; }
        public IReadOnlyList<TowerStatModifierDef> AdditionalTowerModifiers { get; }
        public IReadOnlyList<TowerAttackDef> RuntimeTowerAttacks { get; }

        public ResolvedCardData(
            CardDef definition,
            string displayName,
            string description,
            Sprite icon,
            IReadOnlyList<Sprite> augmentIcons,
            CardType type,
            int manaCost,
            int tier,
            int augmentSlots,
            IReadOnlyList<CardEffectData> effects,
            SpawnableObjectDef spawnableObject,
            TowerDef towerDefinition,
            SpellDef spellDefinition,
            IReadOnlyList<TowerStatModifierDef> additionalTowerModifiers,
            IReadOnlyList<TowerAttackDef> runtimeTowerAttacks)
        {
            Definition = definition;
            DisplayName = displayName;
            Description = description;
            Icon = icon;
            AugmentIcons = augmentIcons;
            Type = type;
            ManaCost = manaCost;
            Tier = tier;
            AugmentSlots = augmentSlots;
            Effects = effects;
            SpawnableObject = spawnableObject;
            TowerDefinition = towerDefinition;
            SpellDefinition = spellDefinition;
            AdditionalTowerModifiers = additionalTowerModifiers;
            RuntimeTowerAttacks = runtimeTowerAttacks;
        }
    }
}
