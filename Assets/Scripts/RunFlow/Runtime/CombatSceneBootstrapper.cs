using System.Collections.Generic;
using Cards;
using Combat;
using Enemies;
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
        [SerializeField] private EncounterDef debugEncounter;
        [SerializeField] private List<OwnedCard> debugDeck = new();
        [SerializeField] private int debugCurrentHealth = 20;
        [SerializeField] private int debugMaxHealth = 20;

        private void Start()
        {
            CombatSceneRequest request = GetRequest();
            if (request == null || request.encounter == null || request.run == null)
            {
                Debug.LogError("Combat scene could not find a valid combat request.");
                return;
            }

            EnemyPath runtimePath = SpawnPath(request.encounter);
            if (enemySpawner != null)
            {
                enemySpawner.ConfigureEncounter(request.encounter, runtimePath, combatSessionDriver);
                enemySpawner.Begin();
            }

            if (combatSessionDriver != null)
            {
                combatSessionDriver.ConfigureSession(new CombatSessionSetup
                {
                    StartingMana = request.startingMana,
                    MaxMana = request.maxMana,
                    ManaRegenPerSecond = request.manaRegenPerSecond,
                    CurrentHealth = request.run.currentHealth,
                    MaxHealth = request.run.maxHealth,
                    OpeningHandSize = request.openingHandSize
                });
            }

            handViewDriver?.Initialize(request.run.deck, request.manualDrawCost, combatSessionDriver);
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
                completedNodeIds = new List<string>(),
                mapState = new RunMapStateData(),
                seed = 1
            };

            return new CombatSceneRequest("debug", debugEncounter, debugRun);
        }

        private EnemyPath SpawnPath(EncounterDef encounter)
        {
            if (pathAnchor == null)
                pathAnchor = transform;

            for (int i = pathAnchor.childCount - 1; i >= 0; i--)
                Destroy(pathAnchor.GetChild(i).gameObject);

            if (encounter == null || encounter.pathPrefab == null)
                return pathAnchor.GetComponentInChildren<EnemyPath>(true);

            GameObject pathObject = Instantiate(encounter.pathPrefab, pathAnchor);
            pathObject.name = $"{encounter.DisplayNameOrFallback} Path";
            return pathObject.GetComponent<EnemyPath>();
        }
    }
}
