using System.Collections.Generic;
using Cards;
using Towers;

namespace Relics
{
    public sealed class RelicCardModificationContext
    {
        public RelicCardModificationContext(
            OwnedCard ownedCard,
            CardDef definition,
            int manaCost,
            int augmentSlots,
            List<CardEffectData> cardEffects,
            List<TowerStatModifierDef> towerModifiers,
            List<TowerAttackModifierData> towerAttackModifiers)
        {
            OwnedCard = ownedCard;
            Definition = definition;
            ManaCost = manaCost;
            AugmentSlots = augmentSlots;
            CardEffects = cardEffects;
            TowerModifiers = towerModifiers;
            TowerAttackModifiers = towerAttackModifiers;
        }

        public OwnedCard OwnedCard { get; }
        public CardDef Definition { get; }
        public int ManaCost { get; set; }
        public int AugmentSlots { get; set; }
        public List<CardEffectData> CardEffects { get; }
        public List<TowerStatModifierDef> TowerModifiers { get; }
        public List<TowerAttackModifierData> TowerAttackModifiers { get; }
        public List<SpellTriggeredEffect> AdditionalSpellEffects { get; } = new();
    }
}
