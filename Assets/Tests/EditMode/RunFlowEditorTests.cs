using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cards;
using Enemies;
using NUnit.Framework;
using RunFlow;
using UnityEditor;
using UnityEngine;

public class RunFlowEditorTests
{
    private string saveDirectory;
    private RunContentRepository contentRepository;
    private readonly List<string> createdAssetPaths = new();

    [SetUp]
    public void SetUp()
    {
        saveDirectory = Path.Combine(Application.dataPath, "../Temp/RunFlowEditorTests", TestContext.CurrentContext.Test.Name);
        if (Directory.Exists(saveDirectory))
            Directory.Delete(saveDirectory, true);

        Directory.CreateDirectory(saveDirectory);
        contentRepository = new RunContentRepository();
        contentRepository.Refresh();
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < createdAssetPaths.Count; i++)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(createdAssetPaths[i]) != null)
                AssetDatabase.DeleteAsset(createdAssetPaths[i]);
        }

        createdAssetPaths.Clear();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (Directory.Exists(saveDirectory))
            Directory.Delete(saveDirectory, true);
    }

    [Test]
    public void SaveService_RoundTripsProfileAndGeneratedRunData()
    {
        SaveService saveService = new(contentRepository, saveDirectory);

        ProfileSaveData profile = new()
        {
            metaCurrency = 7,
            activeRunId = "edit-run"
        };
        profile.AddUnlock("unlock.test");
        saveService.SaveProfile(profile);

        ProfileSaveData loadedProfile = saveService.LoadProfile();
        Assert.That(loadedProfile.metaCurrency, Is.EqualTo(7));
        Assert.That(loadedProfile.activeRunId, Is.EqualTo("edit-run"));
        Assert.That(loadedProfile.unlockIds, Does.Contain("unlock.test"));

        MapTemplateDef template = CreateTemplate();
        RunMapGenerator generator = new(contentRepository);
        RunMapStateData mapState = generator.Generate(template, 12345);

        OwnedCard firstCard = new(template.startingDeck[0]);
        RunSaveData run = new()
        {
            runId = "edit-run",
            currentHealth = 17,
            maxHealth = 20,
            gold = 21,
            deck = new List<OwnedCard> { firstCard },
            currentNodeId = mapState.startNodeId,
            completedNodeIds = new List<string> { mapState.startNodeId },
            mapState = mapState,
            pendingReward = new PendingRewardData
            {
                sourceNodeId = "node-c1-l0",
                offeredCardIds = new List<string> { contentRepository.GetCardId(template.startingDeck[0]) }
            },
            seed = 12345
        };

        saveService.SaveRun(run);
        RunSaveData loadedRun = saveService.LoadRun("edit-run");

        Assert.NotNull(loadedRun);
        Assert.That(loadedRun.currentHealth, Is.EqualTo(17));
        Assert.That(loadedRun.gold, Is.EqualTo(21));
        Assert.That(loadedRun.mapState.mapTemplateId, Is.EqualTo(template.TemplateId));
        Assert.That(loadedRun.mapState.nodes.Count, Is.EqualTo(mapState.nodes.Count));
        Assert.That(loadedRun.mapState.startNodeId, Is.EqualTo(mapState.startNodeId));
        Assert.That(loadedRun.mapState.nodes[0].column, Is.EqualTo(mapState.nodes[0].column));
        Assert.That(loadedRun.mapState.nodes[0].nextNodeIds.Count, Is.EqualTo(mapState.nodes[0].nextNodeIds.Count));
        Assert.That(loadedRun.deck.Count, Is.EqualTo(1));
        Assert.That(loadedRun.deck[0].CurrentDefinition, Is.EqualTo(template.startingDeck[0]));
        Assert.That(loadedRun.pendingReward.offeredCardIds.Count, Is.EqualTo(1));
    }

    [Test]
    public void RunMapGenerator_CreatesSingleStartSingleBossAndForwardOnlyEdges()
    {
        RunMapGenerator generator = new(contentRepository);
        MapTemplateDef template = CreateTemplate();
        RunMapStateData mapState = generator.Generate(template, 42);

        List<RunMapNodeData> startNodes = mapState.nodes.FindAll(node => node.nodeType == MapNodeType.Start);
        List<RunMapNodeData> bossNodes = mapState.nodes.FindAll(node => node.nodeType == MapNodeType.Boss);
        List<RunMapNodeData> victoryNodes = mapState.nodes.FindAll(node => node.nodeType == MapNodeType.Victory);

        Assert.That(startNodes.Count, Is.EqualTo(1));
        Assert.That(bossNodes.Count, Is.EqualTo(1));
        Assert.That(victoryNodes.Count, Is.EqualTo(0));
        Assert.That(mapState.nodes.Count - 1, Is.EqualTo(template.totalPlayableNodes));

        Dictionary<int, int> nodesPerColumn = new();
        for (int i = 0; i < mapState.nodes.Count; i++)
        {
            RunMapNodeData node = mapState.nodes[i];
            if (!nodesPerColumn.TryGetValue(node.column, out int count))
                count = 0;

            nodesPerColumn[node.column] = count + 1;

            for (int nextIndex = 0; nextIndex < node.nextNodeIds.Count; nextIndex++)
            {
                RunMapNodeData nextNode = mapState.FindNode(node.nextNodeIds[nextIndex]);
                Assert.NotNull(nextNode);
                Assert.That(nextNode.column, Is.EqualTo(node.column + 1));
            }
        }

        foreach (KeyValuePair<int, int> entry in nodesPerColumn)
            Assert.That(entry.Value, Is.LessThanOrEqualTo(Mathf.Max(1, template.maxActivePaths)));
    }

    [Test]
    public void RunMapGenerator_ProducesDeterministicLayoutAndEncounters()
    {
        RunMapGenerator generator = new(contentRepository);
        MapTemplateDef template = CreateTemplate();

        RunMapStateData mapA = generator.Generate(template, 98765);
        RunMapStateData mapB = generator.Generate(template, 98765);

        Assert.That(mapA.nodes.Count, Is.EqualTo(mapB.nodes.Count));
        for (int i = 0; i < mapA.nodes.Count; i++)
        {
            RunMapNodeData left = mapA.nodes[i];
            RunMapNodeData right = mapB.nodes[i];
            Assert.That(left.nodeId, Is.EqualTo(right.nodeId));
            Assert.That(left.nodeType, Is.EqualTo(right.nodeType));
            Assert.That(left.encounterId, Is.EqualTo(right.encounterId));
            Assert.That(left.column, Is.EqualTo(right.column));
            Assert.That(left.lane, Is.EqualTo(right.lane));
            CollectionAssert.AreEqual(left.nextNodeIds, right.nextNodeIds);
        }
    }

    [Test]
    public void RunMapGenerator_RespectsNodeTypeMinimumsAndMaximums()
    {
        RunMapGenerator generator = new(contentRepository);
        MapTemplateDef template = CreateTemplate();
        template.totalPlayableNodes = 9;
        template.nodeTypeRules = new List<NodeTypeGenerationRule>
        {
            new() { nodeType = MapNodeType.Fight, weight = 1, minCount = 0, maxCount = -1 },
            new() { nodeType = MapNodeType.Shop, weight = 1, minCount = 2, maxCount = 2 },
            new() { nodeType = MapNodeType.Rest, weight = 1, minCount = 1, maxCount = 1 },
            new() { nodeType = MapNodeType.Miniboss, weight = 1, minCount = 1, maxCount = 1 }
        };

        RunMapStateData mapState = generator.Generate(template, 222);
        int shopCount = CountNodes(mapState, MapNodeType.Shop);
        int restCount = CountNodes(mapState, MapNodeType.Rest);
        int minibossCount = CountNodes(mapState, MapNodeType.Miniboss);

        Assert.That(shopCount, Is.EqualTo(2));
        Assert.That(restCount, Is.EqualTo(1));
        Assert.That(minibossCount, Is.EqualTo(1));
    }

    [Test]
    public void RunMapGenerator_AssignsEncountersFromPoolsWithoutRepeatsUntilExhausted()
    {
        RunMapGenerator generator = new(contentRepository);
        MapTemplateDef template = CreateTemplate();
        template.totalPlayableNodes = 7;
        template.nodeTypeRules = new List<NodeTypeGenerationRule>
        {
            new() { nodeType = MapNodeType.Fight, weight = 1, minCount = 5, maxCount = -1 },
            new() { nodeType = MapNodeType.Shop, weight = 0, minCount = 0, maxCount = 0 },
            new() { nodeType = MapNodeType.Rest, weight = 0, minCount = 0, maxCount = 0 },
            new() { nodeType = MapNodeType.Miniboss, weight = 0, minCount = 0, maxCount = 0 }
        };

        RunMapStateData mapState = generator.Generate(template, 5511);
        List<RunMapNodeData> fightNodes = mapState.nodes.FindAll(node => node.nodeType == MapNodeType.Fight);
        Assert.That(fightNodes.Count, Is.GreaterThanOrEqualTo(3));

        HashSet<string> firstCycle = new();
        for (int i = 0; i < Mathf.Min(3, fightNodes.Count); i++)
            firstCycle.Add(fightNodes[i].encounterId);

        Assert.That(firstCycle.Count, Is.EqualTo(Mathf.Min(3, fightNodes.Count)));
        for (int i = 0; i < fightNodes.Count; i++)
            Assert.That(template.GetEncounterPool(MapNodeType.Fight).GetValidEntries().Exists(entry => entry.encounter.EncounterId == fightNodes[i].encounterId));
    }

    [Test]
    public void RunMapGenerator_EveryRouteCanReachBoss()
    {
        RunMapGenerator generator = new(contentRepository);
        MapTemplateDef template = CreateTemplate();
        RunMapStateData mapState = generator.Generate(template, 7721);
        RunMapNodeData bossNode = mapState.nodes.Find(node => node.nodeType == MapNodeType.Boss);

        Assert.NotNull(bossNode);
        for (int i = 0; i < mapState.nodes.Count; i++)
            Assert.True(CanReachBoss(mapState, mapState.nodes[i], bossNode.nodeId, new HashSet<string>()));
    }

    [Test]
    public void RunCoordinator_CommitsToSelectedBranchAndKeepsMergeReachable()
    {
        SaveService saveService = new(contentRepository, saveDirectory);
        ProfileSaveData profile = new() { activeRunId = "branch-run" };
        saveService.SaveProfile(profile);

        MapTemplateDef template = contentRepository.GetDefaultMapTemplate();
        RunSaveData run = new()
        {
            runId = "branch-run",
            currentHealth = 20,
            maxHealth = 20,
            gold = 10,
            currentNodeId = "node-start",
            completedNodeIds = new List<string> { "node-start" },
            mapState = CreateBranchingMapState(template.TemplateId),
            seed = 1,
            deck = new List<OwnedCard>()
        };
        saveService.SaveRun(run);

        string loadedScene = null;
        RunCoordinator coordinator = new(saveService, contentRepository, sceneName => loadedScene = sceneName);
        List<RunMapNodeData> availableNodes = coordinator.GetAvailableNodes();

        Assert.That(availableNodes.Count, Is.EqualTo(2));
        coordinator.SelectNode("node-rest-a");
        Assert.That(coordinator.CurrentRun.currentNodeId, Is.EqualTo("node-rest-a"));

        Assert.True(coordinator.ApplyRestHeal("node-rest-a"));
        availableNodes = coordinator.GetAvailableNodes();
        Assert.That(availableNodes.Count, Is.EqualTo(1));
        Assert.That(availableNodes[0].nodeId, Is.EqualTo("node-merge"));
        Assert.That(loadedScene, Is.Null);
    }

    [Test]
    public void RunCoordinator_TransitionsThroughCombatAndFailure()
    {
        SaveService saveService = new(contentRepository, saveDirectory);
        string loadedScene = null;
        RunCoordinator coordinator = new(saveService, contentRepository, sceneName => loadedScene = sceneName);

        coordinator.StartNewRun();
        Assert.That(loadedScene, Is.EqualTo(SceneNames.RunMap));
        Assert.NotNull(coordinator.CurrentRun);
        string runId = coordinator.CurrentRun.runId;

        RunMapNodeData firstFight = AdvanceUntilCombatNode(coordinator);
        Assert.NotNull(firstFight);

        coordinator.SelectNode(firstFight.nodeId);
        Assert.That(loadedScene, Is.EqualTo(SceneNames.Combat));
        Assert.NotNull(coordinator.CurrentCombatRequest);

        coordinator.HandleCombatResult(new CombatSceneResult(firstFight.nodeId, coordinator.CurrentCombatRequest.encounter, true, 18));
        Assert.That(loadedScene, Is.EqualTo(SceneNames.RunMap));
        Assert.True(coordinator.CurrentRun.HasCompletedNode(firstFight.nodeId));
        Assert.NotNull(coordinator.CurrentRun.pendingReward);

        coordinator.SkipPendingReward();
        RunMapNodeData secondFight = AdvanceUntilCombatNode(coordinator);
        Assert.NotNull(secondFight);

        coordinator.SelectNode(secondFight.nodeId);
        coordinator.HandleCombatResult(new CombatSceneResult(secondFight.nodeId, coordinator.CurrentCombatRequest.encounter, false, 0));

        Assert.That(loadedScene, Is.EqualTo(SceneNames.MainMenu));
        Assert.IsNull(coordinator.CurrentRun);
        Assert.IsNull(saveService.LoadRun(runId));
    }

    [Test]
    public void RunFlowProjectSetup_CreateEncounter_PreservesExistingSpawnBatches()
    {
        const string displayName = "Editor Test Encounter";
        string encounterAssetPath = $"Assets/Resources/RunFlow/Encounters/{displayName}.asset";
        if (AssetDatabase.LoadAssetAtPath<Object>(encounterAssetPath) != null)
            AssetDatabase.DeleteAsset(encounterAssetPath);

        createdAssetPaths.Add(encounterAssetPath);

        MethodInfo createEncounter = typeof(RunFlowProjectSetup).GetMethod("CreateEncounter", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(createEncounter);

        GameObject pathPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RunFlow/Paths/StarterCombatPath 1.prefab");
        CardRewardPoolDef rewardPool = AssetDatabase.LoadAssetAtPath<CardRewardPoolDef>("Assets/Resources/RunFlow/Rewards/StarterRewardPool.asset");
        EnemyDef enemyA = AssetDatabase.LoadAssetAtPath<EnemyDef>("Assets/Resources/Combat/Enemies/Definitions/Enemy A.asset");
        EnemyDef enemyB = AssetDatabase.LoadAssetAtPath<EnemyDef>("Assets/Resources/Combat/Enemies/Definitions/Enemy B.asset");

        Assert.NotNull(pathPrefab);
        Assert.NotNull(rewardPool);
        Assert.NotNull(enemyA);
        Assert.NotNull(enemyB);

        List<EnemyDef> enemies = new() { enemyA, enemyB };
        object[] args =
        {
            "editor-test-encounter",
            displayName,
            EncounterKind.Miniboss,
            pathPrefab,
            enemies,
            rewardPool,
            25,
            3,
            2,
            1
        };

        EncounterDef encounter = (EncounterDef)createEncounter.Invoke(null, args);
        Assert.NotNull(encounter);
        Assert.That(encounter.spawnBatches.Count, Is.GreaterThan(0));

        encounter.spawnBatches = new List<SpawnBatch>
        {
            new()
            {
                enemyDef = enemyB,
                spawnCount = 7,
                spawnInterval = 0.25f,
                waitTime = 2.5f
            }
        };
        EditorUtility.SetDirty(encounter);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EncounterDef preservedEncounter = (EncounterDef)createEncounter.Invoke(null, args);
        Assert.NotNull(preservedEncounter);
        Assert.That(preservedEncounter.spawnBatches.Count, Is.EqualTo(1));
        Assert.That(preservedEncounter.spawnBatches[0].enemyDef, Is.EqualTo(enemyB));
        Assert.That(preservedEncounter.spawnBatches[0].spawnCount, Is.EqualTo(7));
        Assert.That(preservedEncounter.spawnBatches[0].spawnInterval, Is.EqualTo(0.25f));
        Assert.That(preservedEncounter.spawnBatches[0].waitTime, Is.EqualTo(2.5f));
    }

    private MapTemplateDef CreateTemplate()
    {
        MapTemplateDef template = ScriptableObject.CreateInstance<MapTemplateDef>();
        template.id = "test-template";
        template.displayName = "Test Template";
        template.totalPlayableNodes = 8;
        template.maxActivePaths = 3;
        template.minColumns = 5;
        template.maxColumns = 6;
        template.branchChance = 0.6f;
        template.mergeChance = 0.5f;
        template.startingHealth = 20;
        template.maxHealth = 20;
        template.startingGold = 12;
        template.startingDeck = new List<CardDef>();

        foreach (CardDef card in contentRepository.Cards)
        {
            if (card == null)
                continue;

            template.startingDeck.Add(card);
            if (template.startingDeck.Count >= 5)
                break;
        }

        template.defaultShopInventory = contentRepository.GetDefaultShopInventory();
        EncounterPoolDef fightPool = ScriptableObject.CreateInstance<EncounterPoolDef>();
        fightPool.id = "fight-pool";
        fightPool.encounters = new List<WeightedEncounterEntry>();
        List<EncounterDef> regularFights = contentRepository.GetEncountersByKind(EncounterKind.RegularFight);
        for (int i = 0; i < regularFights.Count; i++)
            fightPool.encounters.Add(new WeightedEncounterEntry { encounter = regularFights[i], weight = 1 });

        EncounterPoolDef minibossPool = ScriptableObject.CreateInstance<EncounterPoolDef>();
        minibossPool.id = "miniboss-pool";
        minibossPool.encounters = new List<WeightedEncounterEntry>();
        List<EncounterDef> minibosses = contentRepository.GetEncountersByKind(EncounterKind.Miniboss);
        for (int i = 0; i < minibosses.Count; i++)
            minibossPool.encounters.Add(new WeightedEncounterEntry { encounter = minibosses[i], weight = 1 });

        EncounterPoolDef bossPool = ScriptableObject.CreateInstance<EncounterPoolDef>();
        bossPool.id = "boss-pool";
        bossPool.encounters = new List<WeightedEncounterEntry>();
        List<EncounterDef> bosses = contentRepository.GetEncountersByKind(EncounterKind.Boss);
        if (bosses.Count == 0)
        {
            EncounterDef boss = ScriptableObject.CreateInstance<EncounterDef>();
            boss.id = "editor-boss";
            boss.displayName = "Editor Boss";
            boss.encounterKind = EncounterKind.Boss;
            bosses.Add(boss);
        }

        for (int i = 0; i < bosses.Count; i++)
            bossPool.encounters.Add(new WeightedEncounterEntry { encounter = bosses[i], weight = 1 });

        template.nodeTypeRules = new List<NodeTypeGenerationRule>
        {
            new() { nodeType = MapNodeType.Fight, weight = 6, minCount = 0, maxCount = -1 },
            new() { nodeType = MapNodeType.Shop, weight = 2, minCount = 1, maxCount = 2 },
            new() { nodeType = MapNodeType.Rest, weight = 2, minCount = 1, maxCount = 2 },
            new() { nodeType = MapNodeType.Miniboss, weight = 1, minCount = 1, maxCount = 1 }
        };
        template.nodeEncounterPools = new List<NodeEncounterPoolBinding>
        {
            new() { nodeType = MapNodeType.Fight, encounterPool = fightPool },
            new() { nodeType = MapNodeType.Miniboss, encounterPool = minibossPool },
            new() { nodeType = MapNodeType.Boss, encounterPool = bossPool }
        };

        return template;
    }

    private static int CountNodes(RunMapStateData mapState, MapNodeType nodeType)
    {
        int count = 0;
        for (int i = 0; i < mapState.nodes.Count; i++)
        {
            if (mapState.nodes[i].nodeType == nodeType)
                count++;
        }

        return count;
    }

    private static bool CanReachBoss(RunMapStateData mapState, RunMapNodeData node, string bossNodeId, HashSet<string> visited)
    {
        if (node == null || !visited.Add(node.nodeId))
            return false;

        if (node.nodeId == bossNodeId)
            return true;

        for (int i = 0; i < node.nextNodeIds.Count; i++)
        {
            if (CanReachBoss(mapState, mapState.FindNode(node.nextNodeIds[i]), bossNodeId, visited))
                return true;
        }

        return false;
    }

    private static RunMapStateData CreateBranchingMapState(string templateId)
    {
        return new RunMapStateData
        {
            mapTemplateId = templateId,
            startNodeId = "node-start",
            nodes = new List<RunMapNodeData>
            {
                new()
                {
                    nodeId = "node-start",
                    displayName = "Start",
                    nodeType = MapNodeType.Start,
                    column = 0,
                    lane = 0,
                    nextNodeIds = new List<string> { "node-rest-a", "node-rest-b" }
                },
                new()
                {
                    nodeId = "node-rest-a",
                    displayName = "Rest A",
                    nodeType = MapNodeType.Rest,
                    column = 1,
                    lane = 0,
                    nextNodeIds = new List<string> { "node-merge" }
                },
                new()
                {
                    nodeId = "node-rest-b",
                    displayName = "Rest B",
                    nodeType = MapNodeType.Rest,
                    column = 1,
                    lane = 1,
                    nextNodeIds = new List<string> { "node-merge" }
                },
                new()
                {
                    nodeId = "node-merge",
                    displayName = "Merge",
                    nodeType = MapNodeType.Fight,
                    encounterId = "regular-fight-a",
                    column = 2,
                    lane = 0,
                    nextNodeIds = new List<string>()
                }
            }
        };
    }

    private static RunMapNodeData AdvanceUntilCombatNode(RunCoordinator coordinator)
    {
        for (int i = 0; i < 12; i++)
        {
            List<RunMapNodeData> availableNodes = coordinator.GetAvailableNodes();
            RunMapNodeData combatNode = availableNodes.Find(node => node.nodeType == MapNodeType.Fight || node.nodeType == MapNodeType.Miniboss || node.nodeType == MapNodeType.Boss);
            if (combatNode != null)
                return combatNode;

            RunMapNodeData shopNode = availableNodes.Find(node => node.nodeType == MapNodeType.Shop);
            if (shopNode != null)
            {
                coordinator.SelectNode(shopNode.nodeId);
                coordinator.LeaveShop(shopNode.nodeId);
                continue;
            }

            RunMapNodeData restNode = availableNodes.Find(node => node.nodeType == MapNodeType.Rest);
            if (restNode != null)
            {
                coordinator.SelectNode(restNode.nodeId);
                coordinator.ApplyRestHeal(restNode.nodeId);
                continue;
            }
        }

        return null;
    }
}
