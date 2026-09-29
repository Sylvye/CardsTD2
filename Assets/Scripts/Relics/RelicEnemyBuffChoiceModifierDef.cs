using RunFlow;
using UnityEngine;

namespace Relics
{
    [CreateAssetMenu(menuName = "Relics/Effects/Enemy Buff Choice Modifier", fileName = "RelicEnemyBuffChoiceModifier")]
    public class RelicEnemyBuffChoiceModifierDef : RelicEffectDef
    {
        public int choiceCountDelta;

        public override int ModifyEnemyBuffChoiceCount(EnemyBuffPoolDef pool, int currentChoiceCount)
        {
            return Mathf.Max(0, currentChoiceCount + choiceCountDelta);
        }
    }
}
