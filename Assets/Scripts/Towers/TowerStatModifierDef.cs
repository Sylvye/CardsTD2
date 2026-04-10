using UnityEngine;

namespace Towers
{
    public abstract class TowerStatModifierDef : ScriptableObject, IStatModifier
    {
        public abstract void ModifyStats(TowerAgent tower, ref TowerResolvedStats stats);
    }
}
