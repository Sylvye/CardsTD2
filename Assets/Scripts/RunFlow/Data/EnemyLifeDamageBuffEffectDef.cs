using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Enemy Buff Effects/Life Damage Add", fileName = "EnemyBuffLifeDamageAdd")]
    public class EnemyLifeDamageBuffEffectDef : EnemyBuffEffectDef
    {
        public int lifeDamageAdd = 1;

        public override void Apply(EnemyBuffRuntimeState runtimeState, int stackCount)
        {
            if (runtimeState == null || stackCount <= 0)
                return;

            runtimeState.LifeDamageAdd += lifeDamageAdd * stackCount;
        }
    }
}
