using System.Collections.Generic;
using Combat;
using Enemies;
using UnityEngine;

namespace Towers
{
    public abstract class TowerAttackDef : ScriptableObject
    {
        [SerializeField] private List<TowerTargetFilterDef> targetFilters = new();
        [SerializeField] private DamageTypeDef damageType;
        [SerializeField] private List<EnemyStatusEffectApplication> onHitStatusEffects = new();

        public IReadOnlyList<TowerTargetFilterDef> TargetFilters => targetFilters;
        public DamageTypeDef DamageType => damageType;
        public IReadOnlyList<EnemyStatusEffectApplication> OnHitStatusEffects => onHitStatusEffects;

        public abstract IAttackExecution CreateExecution(TowerAgent tower);
    }
}
