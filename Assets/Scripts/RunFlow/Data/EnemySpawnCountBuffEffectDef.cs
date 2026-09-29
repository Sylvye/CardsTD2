using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Enemy Buff Effects/Spawn Count Multiplier", fileName = "EnemyBuffSpawnCountMultiplier")]
    public class EnemySpawnCountBuffEffectDef : EnemyBuffEffectDef
    {
        [Min(0f)] public float multiplier = 1f;

        public override void Apply(EnemyBuffRuntimeState runtimeState, int stackCount)
        {
            if (runtimeState == null || stackCount <= 0)
                return;

            runtimeState.SpawnCountMultiplier *= Mathf.Pow(Mathf.Max(0f, multiplier), stackCount);
        }
    }
}
