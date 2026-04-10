using Enemies;
using UnityEngine;

namespace Towers
{
    public abstract class TowerTargetFilterDef : ScriptableObject, ITargetFilter
    {
        public abstract bool IsTargetValid(TowerAgent tower, EnemyAgent enemy);
    }
}
