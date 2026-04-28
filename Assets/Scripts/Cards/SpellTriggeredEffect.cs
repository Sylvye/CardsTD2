using System;
using Combat;
using Enemies;
using Towers;
using UnityEngine;

namespace Cards
{
    [Serializable]
    public class SpellTriggeredEffect
    {
        public SpellTriggerType trigger = SpellTriggerType.OnEnemyEnter;
        public SpellEffectType effectType = SpellEffectType.None;
        public SpellTargetType targetType = SpellTargetType.None;
        public float amount = 0f;
        public DamageTypeDef damageType;
        [Min(0.01f)] public float tickInterval = 1f;

        [Header("Modifiers")]
        public TowerStatModifierDef towerModifier;
        public EnemyStatModifierDef enemyModifier;
        public SpellModifierApplicationMode modifierApplicationMode = SpellModifierApplicationMode.Persistent;

        public SpellTriggeredEffect Clone()
        {
            return new SpellTriggeredEffect
            {
                trigger = trigger,
                effectType = effectType,
                targetType = targetType,
                amount = amount,
                damageType = damageType,
                tickInterval = tickInterval,
                towerModifier = towerModifier,
                enemyModifier = enemyModifier,
                modifierApplicationMode = modifierApplicationMode
            };
        }
    }
}
