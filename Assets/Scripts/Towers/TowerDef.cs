using System;
using System.Collections.Generic;
using UnityEngine;

namespace Towers
{
    public enum TargetPriority
    {
        First,
        Last,
        Strong
    }

    [Serializable]
    public struct TowerBaseStats
    {
        [Min(1f)] public float maxHealth;
        [Min(0f)] public float range;
        [Min(0.01f)] public float fireInterval;
        [Min(0f)] public float damage;

        public TowerResolvedStats ToResolvedStats()
        {
            return new TowerResolvedStats(maxHealth, range, fireInterval, damage);
        }
    }

    [Serializable]
    public struct TowerResolvedStats
    {
        public float MaxHealth;
        public float Range;
        public float FireInterval;
        public float Damage;

        public TowerResolvedStats(float maxHealth, float range, float fireInterval, float damage)
        {
            MaxHealth = Mathf.Max(1f, maxHealth);
            Range = Mathf.Max(0f, range);
            FireInterval = Mathf.Max(0.01f, fireInterval);
            Damage = Mathf.Max(0f, damage);
        }

        public void Clamp()
        {
            MaxHealth = Mathf.Max(1f, MaxHealth);
            Range = Mathf.Max(0f, Range);
            FireInterval = Mathf.Max(0.01f, FireInterval);
            Damage = Mathf.Max(0f, Damage);
        }
    }

    [CreateAssetMenu(menuName = "Towers/Tower Definition", fileName = "New Tower Definition")]
    public class TowerDef : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;

        [Header("Spawn")]
        public TowerAgent prefab;
        [Min(0f)] public float placementRadius = 0.5f;

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
