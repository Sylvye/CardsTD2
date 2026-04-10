using UnityEngine;

namespace Towers
{
    /// <summary>
    /// Runtime contract for summon prefabs spawned by <see cref="SummonTowerAttackDef"/>.
    /// Summons can use ParentTower for live-linked inherited stats/effects.
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
            float lifetimeSeconds)
        {
            ParentTower = parentTower;
            RuntimeContext = runtimeContext;
            LifetimeSeconds = Mathf.Max(0.01f, lifetimeSeconds);
        }

        public TowerAgent ParentTower { get; }
        public TowerRuntimeContext RuntimeContext { get; }
        public float LifetimeSeconds { get; }
    }
}
