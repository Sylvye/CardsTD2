using Enemies;

namespace Towers
{
    public readonly struct TowerRuntimeContext
    {
        public TowerRuntimeContext(TowerManager towerManager, EnemyManager enemyManager)
        {
            TowerManager = towerManager;
            EnemyManager = enemyManager;
        }

        public TowerManager TowerManager { get; }
        public EnemyManager EnemyManager { get; }
    }
}
