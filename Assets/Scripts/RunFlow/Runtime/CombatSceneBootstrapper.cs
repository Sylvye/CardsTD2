using System.Collections.Generic;
using Cards;
using Combat;
using Enemies;
using Relics;
using UnityEngine;

namespace RunFlow
{
    public class CombatSceneBootstrapper : MonoBehaviour
    {
        [SerializeField] private CombatSessionDriver combatSessionDriver;
        [SerializeField] private HandViewDriver handViewDriver;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private Transform pathAnchor;

        private PauseMenuController pauseMenuController;

        private void Start()
        {
            EnsurePauseMenu();

            CombatSceneRequest request = GetRequest();
            if (request == null || request.encounter == null || request.pathPrefab == null || request.run == null)
            {
                Debug.LogError("Combat scene could not find a valid combat request.");
                return;
            }

            EnemyPath runtimePath = SpawnPath(request.pathPrefab);
            if (enemySpawner != null)
            {
                enemySpawner.ConfigureEncounter(request.encounter, runtimePath, combatSessionDriver);
                enemySpawner.Begin();
            }

            if (combatSessionDriver != null)
                combatSessionDriver.ConfigureSession(BuildSessionSetup(request));

            handViewDriver?.Initialize(request.run.deck, combatSessionDriver, request.run.ownedRelics);
        }

        private CombatSessionSetup BuildSessionSetup(CombatSceneRequest request)
        {
            CombatSessionSetup setup = combatSessionDriver?.ConfiguredSetup?.Clone() ?? new CombatSessionSetup();

            if (request?.run != null)
            {
                setup.CurrentHealth = request.run.currentHealth;
                setup.MaxHealth = request.run.maxHealth;
            }

            RelicResolver.ModifyCombatSetup(request?.run?.ownedRelics, setup);
            return setup;
        }

        private CombatSceneRequest GetRequest()
        {
            RunCoordinator coordinator = GameFlowRoot.Instance != null ? GameFlowRoot.Instance.Coordinator : null;
            return coordinator?.CurrentCombatRequest;
        }

        private EnemyPath SpawnPath(EnemyPath pathPrefab)
        {
            if (pathAnchor == null)
                pathAnchor = transform;

            for (int i = pathAnchor.childCount - 1; i >= 0; i--)
                Destroy(pathAnchor.GetChild(i).gameObject);

            if (pathPrefab == null)
                return null;

            GameObject pathObject = Instantiate(pathPrefab.gameObject, pathAnchor);
            pathObject.name = $"{pathPrefab.name} Path";
            return pathObject.GetComponent<EnemyPath>();
        }

        private void EnsurePauseMenu()
        {
            pauseMenuController ??= gameObject.GetComponent<PauseMenuController>();
            pauseMenuController ??= gameObject.AddComponent<PauseMenuController>();
            pauseMenuController.Initialize(
                GameFlowRoot.Instance != null ? GameFlowRoot.Instance.Coordinator : null,
                true,
                isOpen =>
                {
                    if (combatSessionDriver != null)
                        combatSessionDriver.SetPaused(isOpen);
                });
        }
    }
}
