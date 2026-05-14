using System.Collections.Generic;
using Combat;
using Enemies;
using UnityEngine;

namespace Towers
{
    public abstract class TowerAttackDef : ScriptableObject
    {
        [SerializeField] private List<TowerTargetFilterDef> targetFilters = new();
        [SerializeField] private DamageTypeDef damageType;
        [SerializeField] private List<EnemyStatusEffectApplication> onHitStatusEffects = new();
        [SerializeField, Min(0f)] private float splashRadius = 0f;
        [SerializeField, Min(0)] private int splashMaxTargets = 3;
        [SerializeField] private GameObject splashImpactEffectPrefab;

        public IReadOnlyList<TowerTargetFilterDef> TargetFilters => targetFilters;
        public DamageTypeDef DamageType => damageType;
        public IReadOnlyList<EnemyStatusEffectApplication> OnHitStatusEffects => onHitStatusEffects;
        public float SplashRadius => Mathf.Max(0f, splashRadius);
        public int SplashMaxTargets => Mathf.Max(0, splashMaxTargets);
        public GameObject SplashImpactEffectPrefab => splashImpactEffectPrefab;
        public virtual bool SupportsHitPointSplash => false;

        public abstract IAttackExecution CreateExecution(TowerAgent tower);

        internal void AdjustSplashRadius(float delta)
        {
            splashRadius = Mathf.Max(0f, splashRadius + delta);
        }

        protected virtual void OnValidate()
        {
            splashRadius = Mathf.Max(0f, splashRadius);
            splashMaxTargets = Mathf.Max(0, splashMaxTargets);
        }
    }
}
