using UnityEngine;

namespace Towers
{
    /// <summary>
    /// Runtime contract for summon prefabs spawned by <see cref="SummonTowerAttackDef"/>.
    /// Summons use a snapshot of parent tower stats captured at spawn time.
    /// </summary>
    public interface ITowerSummon
    {
        bool IsAlive { get; }

        void Initialize(TowerSummonContext context);
    }

    public readonly struct TowerSummonContext
    {
        public TowerSummonContext(
            TowerAgent parentTower,
            TowerRuntimeContext runtimeContext,
            TowerResolvedStats parentStatsSnapshot,
            float lifetimeSeconds)
        {
            ParentTower = parentTower;
            RuntimeContext = runtimeContext;
            ParentStatsSnapshot = parentStatsSnapshot;
            LifetimeSeconds = Mathf.Max(0.01f, lifetimeSeconds);
        }

        public TowerAgent ParentTower { get; }
        public TowerRuntimeContext RuntimeContext { get; }
        public TowerResolvedStats ParentStatsSnapshot { get; }
        public float LifetimeSeconds { get; }
    }
}
