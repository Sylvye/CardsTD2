using Combat;
using UnityEngine;

namespace Enemies
{
    public class EnemyEffectContext
    {
        public EnemyAgent SourceEnemy { get; }
        public EnemyManager EnemyManager { get; }
        public EnemySpawner EnemySpawner { get; }
        public IPlayerEffects PlayerEffects { get; }
        public Vector3 WorldPosition { get; }
        public float TrackDistance { get; }

        public EnemyEffectContext(
            EnemyAgent sourceEnemy,
            EnemyManager enemyManager,
            EnemySpawner enemySpawner,
            IPlayerEffects playerEffects,
            Vector3 worldPosition,
            float trackDistance)
        {
            SourceEnemy = sourceEnemy;
            EnemyManager = enemyManager;
            EnemySpawner = enemySpawner;
            PlayerEffects = playerEffects;
            WorldPosition = worldPosition;
            TrackDistance = trackDistance;
        }
    }
}
