using System;
using UnityEngine;

namespace Towers
{
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
}
