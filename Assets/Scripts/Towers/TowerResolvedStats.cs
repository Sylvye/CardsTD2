using System;
using UnityEngine;

namespace Towers
{
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
}
