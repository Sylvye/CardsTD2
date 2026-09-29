using UnityEngine;

namespace RunFlow
{
    public abstract class EnemyBuffEffectDef : ScriptableObject
    {
        public abstract void Apply(EnemyBuffRuntimeState runtimeState, int stackCount);
    }
}
