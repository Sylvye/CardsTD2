using UnityEngine;
using Combat;
using Cards;

namespace Enemies
{
    public class EnemyEffectContext
    {
        public EnemyAgent SourceEnemy { get; }
        public EnemyManager EnemyManager { get; }
        public EnemySpawner EnemySpawner { get; }
        public HandViewDriver HandViewDriver { get; }
        public Vector3 WorldPosition { get; }
        public float TrackDistance { get; }

        public EnemyEffectContext(
            EnemyAgent sourceEnemy,
            EnemyManager enemyManager,
            EnemySpawner enemySpawner,
            HandViewDriver handViewDriver,
            Vector3 worldPosition,
            float trackDistance)
        {
            SourceEnemy = sourceEnemy;
            EnemyManager = enemyManager;
            EnemySpawner = enemySpawner;
            HandViewDriver = handViewDriver;
            WorldPosition = worldPosition;
            TrackDistance = trackDistance;
        }
    }
}