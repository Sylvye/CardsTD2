using UnityEngine;

namespace Enemies
{
    public abstract class EnemyStatModifierDef : ScriptableObject, IEnemyStatModifier
    {
        public abstract void ModifyStats(EnemyAgent enemy, ref EnemyResolvedStats stats);
    }
}
