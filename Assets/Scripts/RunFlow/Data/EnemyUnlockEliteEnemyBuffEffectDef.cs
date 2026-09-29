using Enemies;
using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Enemy Buff Effects/Unlock Elite Enemy", fileName = "EnemyBuffUnlockEliteEnemy")]
    public class EnemyUnlockEliteEnemyBuffEffectDef : EnemyBuffEffectDef
    {
        public EnemyDef eliteEnemyDef;

        public override void Apply(EnemyBuffRuntimeState runtimeState, int stackCount)
        {
            if (runtimeState == null || eliteEnemyDef == null || stackCount <= 0)
                return;

            RunContentRepository contentRepository = GameFlowRoot.Instance != null ? GameFlowRoot.Instance.ContentRepository : null;
            string enemyId = contentRepository != null ? contentRepository.GetEnemyId(eliteEnemyDef) : eliteEnemyDef.EnemyId;
            runtimeState.UnlockEliteEnemy(enemyId, eliteEnemyDef);
        }
    }
}
