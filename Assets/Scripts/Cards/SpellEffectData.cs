using System;
using Enemies;
using Towers;
using UnityEngine;

namespace Cards
{
    [Serializable]
    public class SpellEffectData
    {
        public SpellEffectType effectType = SpellEffectType.None;
        public SpellTargetType targetType = SpellTargetType.None;
        public float amount = 0f;

        [Header("Modifiers")]
        public TowerStatModifierDef towerModifier;
        public EnemyStatModifierDef enemyModifier;
    }
}
