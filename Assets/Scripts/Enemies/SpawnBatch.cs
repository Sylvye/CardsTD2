using System;

namespace Enemies
{
    [Serializable]
    public class SpawnBatch
    {
        public EnemyDef enemyDef;
        public int spawnCount = 1;
        public float spawnInterval = 1f;
        public float waitTime = 1f;
    }
}