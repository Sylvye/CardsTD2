using System;
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
        [Min(0.01f)] public float tickInterval = 1f;

        [Header("Modifiers")]
        public TowerStatModifierDef towerModifier;
        public EnemyStatModifierDef enemyModifier;
        public SpellModifierApplicationMode modifierApplicationMode = SpellModifierApplicationMode.Persistent;
    }
}
