using System.Collections;
using Combat;
using Enemies;
using UnityEngine;

namespace RunFlow
{
    public class CombatOutcomeWatcher : MonoBehaviour
    {
        [SerializeField] private CombatSessionDriver combatSessionDriver;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private EnemyManager enemyManager;

        private bool hasResolved;

        private void FixedUpdate()
        {
            if (hasResolved || combatSessionDriver == null || combatSessionDriver.PlayerState == null)
                return;

            if (combatSessionDriver.PlayerState.CurrentHealth <= 0)
            {
                StartCoroutine(Resolve(false));
                return;
            }

            if (enemySpawner != null &&
                enemyManager != null &&
                enemySpawner.HasFinishedSpawning &&
                enemyManager.ActiveEnemies.Count == 0)
            {
                StartCoroutine(Resolve(true));
            }
        }

        private IEnumerator Resolve(bool victory)
        {
            hasResolved = true;
            yield return null;

            RunCoordinator coordinator = GameFlowRoot.Instance != null ? GameFlowRoot.Instance.Coordinator : null;
            CombatSceneRequest request = coordinator?.CurrentCombatRequest;
            if (coordinator == null || request == null)
            {
                Debug.Log($"Combat completed without a coordinator. Victory: {victory}");
                yield break;
            }

            CombatSceneResult result = new(
                request.nodeId,
                request.encounter,
                victory,
                combatSessionDriver.PlayerState.CurrentHealth
            );

            coordinator.HandleCombatResult(result);
        }
    }
}
