using System;
using Combat;
using UnityEngine;

namespace Enemies
{
    [Serializable]
    public class EnemyStatusEffectApplication
    {
        public EnemyStatusEffectDef effect;

        [Tooltip("Values at or below zero use the status effect default.")]
        public float duration = 0f;
        [Tooltip("Values below zero use the status effect default.")]
        public float strength = -1f;
        [Tooltip("Values at or below zero use the status effect default.")]
        public float tickInterval = 0f;
        [Tooltip("Values below zero use the status effect default.")]
        public float tickDamage = -1f;
        public DamageTypeDef damageType;

        public bool IsValid => effect != null;

        internal float ResolveDuration()
        {
            return Mathf.Max(0.01f, duration > 0f ? duration : effect.defaultDuration);
        }

        internal float ResolveStrength()
        {
            return Mathf.Max(0f, strength >= 0f ? strength : effect.defaultStrength);
        }

        internal float ResolveTickInterval()
        {
            return Mathf.Max(0.01f, tickInterval > 0f ? tickInterval : effect.defaultTickInterval);
        }

        internal float ResolveTickDamage()
        {
            return Mathf.Max(0f, tickDamage >= 0f ? tickDamage : effect.defaultTickDamage);
        }

        internal DamageTypeDef ResolveDamageType()
        {
            return damageType != null ? damageType : effect.defaultDamageType;
        }
    }
}
