using System.Collections.Generic;
using Enemies;

namespace Towers
{
    public readonly struct TargetingContext
    {
        public TargetingContext(
            TowerAgent tower,
            TowerResolvedStats stats,
            TargetPriority priority,
            IReadOnlyList<EnemyAgent> candidates)
        {
            Tower = tower;
            Stats = stats;
            Priority = priority;
            Candidates = candidates;
        }

        public TowerAgent Tower { get; }
        public TowerResolvedStats Stats { get; }
        public TargetPriority Priority { get; }
        public IReadOnlyList<EnemyAgent> Candidates { get; }
    }
}
