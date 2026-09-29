using System;

namespace Enemies
{
    public enum SpawnBatchMode
    {
        FixedEnemy = 0,
        ElitePoolEnemy = 1
    }

    [Serializable]
    public class SpawnBatch
    {
        public SpawnBatchMode mode = SpawnBatchMode.FixedEnemy;
        public EnemyDef enemyDef;
        public int spawnCount = 1;
        public float spawnInterval = 1f;
        public float waitTime = 1f;
    }
}
