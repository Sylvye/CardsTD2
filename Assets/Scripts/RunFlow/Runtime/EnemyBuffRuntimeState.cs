using System.Collections.Generic;
using Enemies;
using UnityEngine;

namespace RunFlow
{
    public class EnemyBuffRuntimeState
    {
        public float MoveSpeedMultiplier { get; set; } = 1f;
        public float MaxHealthMultiplier { get; set; } = 1f;
        public float SpawnCountMultiplier { get; set; } = 1f;
        public int LifeDamageAdd { get; set; }
        public float FlatDamageReduction { get; set; }
        public int EliteSelectionSeed { get; set; }

        private readonly List<EnemyDef> unlockedEliteEnemies = new();
        private readonly HashSet<string> unlockedEliteEnemyIds = new();

        public IReadOnlyList<EnemyDef> UnlockedEliteEnemies => unlockedEliteEnemies;
        public IReadOnlyCollection<string> UnlockedEliteEnemyIds => unlockedEliteEnemyIds;

        public int ResolveSpawnCount(int baseSpawnCount)
        {
            int clampedBaseCount = Mathf.Max(1, baseSpawnCount);
            float scaledCount = clampedBaseCount * Mathf.Max(0f, SpawnCountMultiplier);
            return Mathf.Max(1, Mathf.CeilToInt(scaledCount));
        }

        public float ResolveMaxHealth(float baseMaxHealth)
        {
            return Mathf.Max(1f, Mathf.Max(0f, baseMaxHealth) * Mathf.Max(0f, MaxHealthMultiplier));
        }

        public int ResolveLifeDamage(int baseLifeDamage)
        {
            return Mathf.Max(0, baseLifeDamage + LifeDamageAdd);
        }

        public void UnlockEliteEnemy(string enemyId, EnemyDef enemyDef)
        {
            if (string.IsNullOrWhiteSpace(enemyId) || enemyDef == null || !unlockedEliteEnemyIds.Add(enemyId))
                return;

            unlockedEliteEnemies.Add(enemyDef);
        }
    }
}
