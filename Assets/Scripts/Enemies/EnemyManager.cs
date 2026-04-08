using System.Collections.Generic;
using Combat;
using UnityEngine;

namespace Enemies
{
    public class EnemyManager : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour playerEffectsSource;

        private readonly List<EnemyAgent> activeEnemies = new();
        private IPlayerEffects playerEffects;

        public IReadOnlyList<EnemyAgent> ActiveEnemies => activeEnemies;

        private void Awake()
        {
            playerEffects = playerEffectsSource as IPlayerEffects;

            if (playerEffectsSource != null && playerEffects == null)
            {
                Debug.LogError($"{nameof(EnemyManager)} requires {nameof(playerEffectsSource)} to implement {nameof(IPlayerEffects)}.");
            }
        }

        public void RegisterEnemy(EnemyAgent enemy)
        {
            if (enemy == null || activeEnemies.Contains(enemy))
                return;

            activeEnemies.Add(enemy);
        }

        public void UnregisterEnemy(EnemyAgent enemy)
        {
            if (enemy is null)
                return;

            activeEnemies.Remove(enemy);
        }

        public void HandleEnemyEscaped(EnemyAgent enemy)
        {
            if (enemy is null)
                return;

            playerEffects?.LoseLives(enemy.LifeDamage);
        }
    }
}
