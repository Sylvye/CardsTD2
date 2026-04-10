using System.Collections.Generic;
using UnityEngine;

namespace Towers
{
    public abstract class TowerAttackDef : ScriptableObject
    {
        [SerializeField] private List<TowerTargetFilterDef> targetFilters = new();

        public IReadOnlyList<TowerTargetFilterDef> TargetFilters => targetFilters;

        public abstract IAttackExecution CreateExecution(TowerAgent tower);
    }
}
