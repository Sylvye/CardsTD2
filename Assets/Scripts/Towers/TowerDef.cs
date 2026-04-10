using System;
using System.Collections.Generic;
using Cards;
using UnityEngine;

namespace Towers
{
    [CreateAssetMenu(menuName = "Towers/Tower Definition", fileName = "New Tower Definition")]
    public class TowerDef : SpawnableObjectDef
    {
        [Header("Identity")]
        public string id;
        public string displayName;

        [Header("Stats")]
        public TowerBaseStats baseStats = new()
        {
            maxHealth = 10f,
            range = 2f,
            fireInterval = 1f,
            damage = 1f
        };

        [Header("Targeting")]
        public TargetPriority defaultTargetPriority = TargetPriority.First;
        public List<string> tags = new();

        [Header("Modifiers")]
        public List<TowerStatModifierDef> defaultModifiers = new();

        [Header("Attacks")]
        public List<TowerAttackDef> attacks = new();

        public TowerResolvedStats GetBaseResolvedStats()
        {
            return baseStats.ToResolvedStats();
        }
    }
}
