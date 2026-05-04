using Combat;
using UnityEngine;

namespace Enemies
{
    [CreateAssetMenu(menuName = "Enemies/Status Effect", fileName = "EnemyStatusEffect")]
    public class EnemyStatusEffectDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public EnemyStatusEffectBehaviorType behaviorType = EnemyStatusEffectBehaviorType.Slow;
        public EnemyStatusStackingMode stackingMode = EnemyStatusStackingMode.NonStackingStrongest;
        public string stackKey;

        [Header("Defaults")]
        [Min(0.01f)] public float defaultDuration = 1f;
        [Min(0f)] public float defaultStrength = 0f;
        [Min(0.01f)] public float defaultTickInterval = 1f;
        [Min(0f)] public float defaultTickDamage = 0f;
        public DamageTypeDef defaultDamageType;

        public string ResolvedStackKey => string.IsNullOrWhiteSpace(stackKey) ? id : stackKey;

        private void OnValidate()
        {
            defaultDuration = Mathf.Max(0.01f, defaultDuration);
            defaultStrength = Mathf.Max(0f, defaultStrength);
            defaultTickInterval = Mathf.Max(0.01f, defaultTickInterval);
            defaultTickDamage = Mathf.Max(0f, defaultTickDamage);
        }
    }
}
