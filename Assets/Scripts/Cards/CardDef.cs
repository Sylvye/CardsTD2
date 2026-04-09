using System.Collections.Generic;
using Combat;
using Towers;
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

        [Header("World Use")]
        public TowerDef towerDefinition;
        public SpawnableObjectDef spawnableObject;

        [Header("Effects")]
        public List<CardEffectData> effects = new();

        public TowerDef TowerDefinition => towerDefinition;

        public float GetPlacementRadius()
        {
            if (type == CardType.Tower && towerDefinition != null)
                return towerDefinition.placementRadius;

            return spawnableObject != null ? spawnableObject.placementRadius : -1f;
        }

        public float GetEffectRadius()
        {
            if (type == CardType.Tower && towerDefinition != null)
                return towerDefinition.baseStats.range;

            return spawnableObject != null ? spawnableObject.effectRadius : 0f;
        }
    }
}
