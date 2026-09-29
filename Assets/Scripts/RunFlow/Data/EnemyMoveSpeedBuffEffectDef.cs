using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Enemy Buff Effects/Move Speed Multiplier", fileName = "EnemyBuffMoveSpeedMultiplier")]
    public class EnemyMoveSpeedBuffEffectDef : EnemyBuffEffectDef
    {
        [Min(0f)] public float multiplier = 1f;

        public override void Apply(EnemyBuffRuntimeState runtimeState, int stackCount)
        {
            if (runtimeState == null || stackCount <= 0)
                return;

            runtimeState.MoveSpeedMultiplier *= Mathf.Pow(Mathf.Max(0f, multiplier), stackCount);
        }
    }
}
