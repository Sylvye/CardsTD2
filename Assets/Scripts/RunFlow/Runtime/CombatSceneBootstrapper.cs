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

        [Header("Debug Fallback")]
        [Tooltip("Assign an encounter here to preview a fight directly in the Combat scene without entering through the run map.")]
        [SerializeField] private EncounterDef debugEncounter;
        [SerializeField] private List<OwnedCard> debugDeck = new();
        [SerializeField] private int debugCurrentHealth = 20;
        [SerializeField] private int debugMaxHealth = 20;

        private PauseMenuController pauseMenuController;

        private void Start()
        {
            EnsurePauseMenu();

            CombatSceneRequest request = GetRequest();
            if (request == null || request.encounter == null || request.run == null)
            {
                Debug.LogError("Combat scene could not find a valid combat request.");
                return;
            }

            EnemyPath runtimePath = SpawnPath(request.pathPrefab, request.encounter);
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
            CombatSessionSetup setup = request?.sessionOverrides?.Clone() ?? combatSessionDriver?.ConfiguredSetup ?? new CombatSessionSetup();

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
            if (coordinator?.CurrentCombatRequest != null)
                return coordinator.CurrentCombatRequest;

            if (debugEncounter == null)
                return null;

            RunSaveData debugRun = new()
            {
                runId = "debug",
                currentHealth = debugCurrentHealth,
                maxHealth = debugMaxHealth,
                gold = 0,
                deck = new List<OwnedCard>(debugDeck),
                ownedRelics = new List<OwnedRelic>(),
                completedNodeIds = new List<string>(),
                mapState = new RunMapStateData(),
                seed = 1
            };

            return new CombatSceneRequest("debug", debugEncounter, debugRun);
        }

        private EnemyPath SpawnPath(EnemyPath pathPrefab, EncounterDef legacyEncounter)
        {
            if (pathAnchor == null)
                pathAnchor = transform;

            for (int i = pathAnchor.childCount - 1; i >= 0; i--)
                Destroy(pathAnchor.GetChild(i).gameObject);

            GameObject pathObjectPrefab = pathPrefab != null
                ? pathPrefab.gameObject
                : legacyEncounter != null ? legacyEncounter.pathPrefab : null;

            if (pathObjectPrefab == null)
                return pathAnchor.GetComponentInChildren<EnemyPath>(true);

            string pathName = pathPrefab != null
                ? pathPrefab.name
                : legacyEncounter != null ? legacyEncounter.DisplayNameOrFallback : "Combat";
            GameObject pathObject = Instantiate(pathObjectPrefab, pathAnchor);
            pathObject.name = $"{pathName} Path";
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
