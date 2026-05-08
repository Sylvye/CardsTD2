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
        public bool exhaust;

        [Header("Progression")]
        public string cardFamilyId;
        public int baseTier = 1;
        public int upgradeTier = 1;
        public int baseAugmentSlots = 0;
        public CardDef nextUpgradeDef;

        [Header("World Use")]
        public SpawnableObjectDef spawnableObject;

        [Header("Support")]
        public SupportCardMode supportCardMode;
        public SupportSubtype supportSubtype;
        public SupportUpgradeTargetMask supportUpgradeTargets;
        [Min(0f)] public float supportConduitRangeIncrease;
        [Min(1)] public int supportEffectiveConnectionReduction = 1;

        [Header("Effects")]
        public List<CardEffectData> effects = new();

        public float GetPlacementRadius()
        {
            if (spawnableObject is TowerDef towerDef)
                return towerDef.placementRadius;

            if (spawnableObject is SupportDef supportDef)
                return supportDef.placementRadius;

            return -1f;
        }

        public float GetEffectRadius()
        {
            if (spawnableObject is TowerDef towerDef)
                return towerDef.baseStats.range;

            if (type == CardType.Spell)
                return SpawnableColliderUtility.GetPreviewRadius(spawnableObject);

            if (spawnableObject is SupportDef supportDef)
                return supportDef.supportRadius;

            return spawnableObject != null ? spawnableObject.effectRadius : 0f;
        }

        public string CardFamilyId => string.IsNullOrWhiteSpace(cardFamilyId) ? id : cardFamilyId;

        public int GetUpgradeTier()
        {
            int tier = upgradeTier > 0 ? upgradeTier : baseTier;
            return Mathf.Max(1, tier);
        }

        public int GetBaseAugmentSlots()
        {
            return Mathf.Max(0, baseAugmentSlots);
        }

        public SupportCardMode GetSupportCardMode()
        {
            if (type != CardType.Support)
                return SupportCardMode.None;

            if (supportCardMode != SupportCardMode.None)
                return supportCardMode;

            if (spawnableObject is SupportDef)
                return SupportCardMode.Spawnable;

            return supportSubtype is SupportSubtype.Amplifier or SupportSubtype.Capacitor
                ? SupportCardMode.Upgrade
                : SupportCardMode.None;
        }

        public SupportSubtype GetSupportSubtype()
        {
            if (type != CardType.Support)
                return SupportSubtype.None;

            if (supportSubtype != SupportSubtype.None)
                return supportSubtype;

            return spawnableObject is SupportDef supportDef
                ? supportDef.supportSubtype
                : SupportSubtype.None;
        }

        public bool CanUpgradeSupportSubtype(SupportSubtype targetSubtype)
        {
            if (GetSupportCardMode() != SupportCardMode.Upgrade)
                return false;

            SupportUpgradeTargetMask targetMask = targetSubtype switch
            {
                SupportSubtype.Beacon => SupportUpgradeTargetMask.Beacon,
                SupportSubtype.Conduit => SupportUpgradeTargetMask.Conduit,
                _ => SupportUpgradeTargetMask.None
            };

            return targetMask != SupportUpgradeTargetMask.None && (supportUpgradeTargets & targetMask) != 0;
        }
    }
}
