using Combat;
using Enemies;

namespace Towers
{
    public readonly struct TowerRuntimeContext
    {
        public TowerRuntimeContext(TowerManager towerManager, EnemyManager enemyManager, IPlayerEffects playerEffects)
        {
            TowerManager = towerManager;
            EnemyManager = enemyManager;
            PlayerEffects = playerEffects;
        }

        public TowerManager TowerManager { get; }
        public EnemyManager EnemyManager { get; }
        public IPlayerEffects PlayerEffects { get; }
    }
}
