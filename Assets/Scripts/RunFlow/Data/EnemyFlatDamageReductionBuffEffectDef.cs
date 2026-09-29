using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Enemy Buff Effects/Flat Damage Reduction", fileName = "EnemyBuffFlatDamageReduction")]
    public class EnemyFlatDamageReductionBuffEffectDef : EnemyBuffEffectDef
    {
        [Min(0f)] public float flatDamageReduction = 0f;

        public override void Apply(EnemyBuffRuntimeState runtimeState, int stackCount)
        {
            if (runtimeState == null || stackCount <= 0)
                return;

            runtimeState.FlatDamageReduction += Mathf.Max(0f, flatDamageReduction) * stackCount;
        }
    }
}
