using System.Collections.Generic;
using UnityEngine;

namespace Cards
{
    [CreateAssetMenu(menuName = "Cards/Card Definition", fileName = "New Card")]
    public class CardDef : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        public Sprite icon;

        [Header("Card Info")]
        public CardType type;
        [TextArea(3, 6)]
        public string description;
        public int baseManaCost = 1;

        [Header("Progression")]
        public int baseTier = 1;
        public int baseAugmentSlots = 0;

        [Header("Effects")]
        public List<CardEffectData> effects = new();
    }
}