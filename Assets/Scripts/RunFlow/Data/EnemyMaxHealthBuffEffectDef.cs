using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Enemy Buff Effects/Max Health Multiplier", fileName = "EnemyBuffMaxHealthMultiplier")]
    public class EnemyMaxHealthBuffEffectDef : EnemyBuffEffectDef
    {
        [Min(0f)] public float multiplier = 1f;

        public override void Apply(EnemyBuffRuntimeState runtimeState, int stackCount)
        {
            if (runtimeState == null || stackCount <= 0)
                return;

            runtimeState.MaxHealthMultiplier *= Mathf.Pow(Mathf.Max(0f, multiplier), stackCount);
        }
    }
}
