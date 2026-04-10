using System.Collections.Generic;
using UnityEngine;

namespace Towers
{
    public class SummonedTowerAgent : TowerAgent, ITowerSummon
    {
        [Header("Fallback Stats")]
        [Min(0f)] public float fallbackPlacementRadius = 0.25f;
        public TowerBaseStats fallbackStats = new()
        {
            maxHealth = 10f,
            range = 2f,
            fireInterval = 1f,
            damage = 1f
        };

        private readonly List<TowerAttackDef> configuredAttacks = new();

        private TowerDef configuredTowerDef;
        private TowerAgent parentTower;
        private float lifetimeRemaining;
        private bool summonInitialized;

        public bool IsAlive => summonInitialized && !IsDead && lifetimeRemaining > 0f;

        public void ConfigureSummon(TowerDef summonTowerDef, IReadOnlyList<TowerAttackDef> summonAttacks)
        {
            configuredTowerDef = summonTowerDef;
            configuredAttacks.Clear();

            if (summonAttacks == null)
                return;

            for (int i = 0; i < summonAttacks.Count; i++)
            {
                TowerAttackDef attackDef = summonAttacks[i];
                if (attackDef != null)
                    configuredAttacks.Add(attackDef);
            }
        }

        public void Initialize(TowerSummonContext context)
        {
            parentTower = context.ParentTower;
            lifetimeRemaining = Mathf.Max(0.01f, context.LifetimeSeconds);

            if (configuredTowerDef != null)
            {
                Initialize(configuredTowerDef, context.RuntimeContext);
            }
            else
            {
                Initialize(
                    fallbackPlacementRadius,
                    fallbackStats.ToResolvedStats(),
                    context.RuntimeContext
                );
            }

            if (configuredAttacks.Count > 0)
                SetRuntimeAttackDefinitions(configuredAttacks);

            if (parentTower != null)
                InheritModifiersFrom(parentTower, append: true);

            summonInitialized = true;
        }

        protected override void Update()
        {
            if (!summonInitialized)
                return;

            if (parentTower == null || parentTower.IsDead)
            {
                Destroy(gameObject);
                return;
            }

            lifetimeRemaining -= Time.deltaTime;
            if (lifetimeRemaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            base.Update();
        }
    }
}
