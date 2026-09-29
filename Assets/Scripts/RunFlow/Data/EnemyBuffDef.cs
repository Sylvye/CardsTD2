using System.Collections.Generic;
using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Enemy Buff", fileName = "EnemyBuff")]
    public class EnemyBuffDef : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 5)] public string description;
        public Sprite icon;
        public List<EnemyBuffEffectDef> effects = new();

        public string BuffId => string.IsNullOrWhiteSpace(id) ? name : id;
        public string DisplayNameOrFallback => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        public void ApplyToRuntime(EnemyBuffRuntimeState runtimeState, int stackCount)
        {
            if (runtimeState == null || effects == null || stackCount <= 0)
                return;

            for (int i = 0; i < effects.Count; i++)
            {
                EnemyBuffEffectDef effect = effects[i];
                if (effect != null)
                    effect.Apply(runtimeState, stackCount);
            }
        }
    }
}
