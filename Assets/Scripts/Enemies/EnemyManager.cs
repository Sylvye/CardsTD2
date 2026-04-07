using System.Collections.Generic;
using Cards;
using UnityEngine;

namespace Enemies
{
    public class EnemyManager : MonoBehaviour
    {
        [SerializeField] private HandViewDriver handViewDriver;

        private readonly List<EnemyAgent> activeEnemies = new();

        public IReadOnlyList<EnemyAgent> ActiveEnemies => activeEnemies;

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

            if (handViewDriver is not null)
            {
                handViewDriver.LoseLives(enemy.LifeDamage);
            }
        }
    }
}