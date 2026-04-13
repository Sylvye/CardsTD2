using System.Collections.Generic;
using Combat;
using Enemies;
using Towers;
using UnityEngine;

namespace Cards
{
    public readonly struct SpellEffectContext
    {
        public SpellEffectContext(
            GameObject zoneObject,
            EnemyAgent sourceEnemy,
            TowerAgent sourceTower,
            IReadOnlyCollection<EnemyAgent> currentEnemies,
            IReadOnlyCollection<TowerAgent> currentTowers,
            EnemyManager enemyManager,
            TowerManager towerManager,
            IPlayerEffects playerEffects)
        {
            ZoneObject = zoneObject;
            SourceEnemy = sourceEnemy;
            SourceTower = sourceTower;
            CurrentEnemies = currentEnemies;
            CurrentTowers = currentTowers;
            EnemyManager = enemyManager;
            TowerManager = towerManager;
            PlayerEffects = playerEffects;
        }

        public GameObject ZoneObject { get; }
        public EnemyAgent SourceEnemy { get; }
        public TowerAgent SourceTower { get; }
        public IReadOnlyCollection<EnemyAgent> CurrentEnemies { get; }
        public IReadOnlyCollection<TowerAgent> CurrentTowers { get; }
        public EnemyManager EnemyManager { get; }
        public TowerManager TowerManager { get; }
        public IPlayerEffects PlayerEffects { get; }
    }
}
