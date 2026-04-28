using System.Collections.Generic;
using Combat;
using UnityEngine;

namespace Towers
{
    public abstract class TowerAttackDef : ScriptableObject
    {
        [SerializeField] private List<TowerTargetFilterDef> targetFilters = new();
        [SerializeField] private DamageTypeDef damageType;

        public IReadOnlyList<TowerTargetFilterDef> TargetFilters => targetFilters;
        public DamageTypeDef DamageType => damageType;

        public abstract IAttackExecution CreateExecution(TowerAgent tower);
    }
}
