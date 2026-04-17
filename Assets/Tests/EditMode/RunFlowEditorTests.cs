using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cards;
using Combat;
using Enemies;
using NUnit.Framework;
using RunFlow;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

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

        ClearGameFlowRoot();

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
            activeRunId = "edit-run",
            debugUiEnabled = true
        };
        profile.AddUnlock("unlock.test");
        saveService.SaveProfile(profile);

        ProfileSaveData loadedProfile = saveService.LoadProfile();
        Assert.That(loadedProfile.metaCurrency, Is.EqualTo(7));
        Assert.That(loadedProfile.activeRunId, Is.EqualTo("edit-run"));
        Assert.That(loadedProfile.debugUiEnabled, Is.True);
        Assert.That(loadedProfile.unlockIds, Does.Contain("unlock.test"));

        MapTemplateDef template = CreateTemplate();
        RunMapGenerator generator = new(contentRepository);
        RunMapStateData mapState = generator.Generate(template, 12345);

        CardAugmentDef compatibleAugment = FindCompatibleAugment(template.startingDeck[0]);
        OwnedCard firstCard = new OwnedCard(template.startingDeck[0], "owned-card-1", compatibleAugment != null ? new[] { compatibleAugment } : null);
        RunSaveData run = new()
        {
            runId = "edit-run",
            currentHealth = 17,
            maxHealth = 20,
            gold = 21,
            deck = new List<OwnedCard> { firstCard },
            ownedAugments = compatibleAugment != null
                ? new List<OwnedAugment> { new OwnedAugment(compatibleAugment, "owned-augment-1") }
                : new List<OwnedAugment>(),
            currentNodeId = mapState.startNodeId,
            completedNodeIds = new List<string> { mapState.startNodeId },
            mapState = mapState,
            pendingReward = new PendingRewardData
            {
                sourceNodeId = "node-c1-l0",
                entries = new List<PendingRewardEntry>
                {
                    new() { rewardType = RunRewardType.Card, contentId = contentRepository.GetCardId(template.startingDeck[0]) }
                },
                offeredCardIds = compatibleAugment != null
                    ? new List<string> { contentRepository.GetCardId(template.startingDeck[0]) }
                    : new List<string>()
            },
            queuedNextMapTemplateId = "act_2",
            endRunAfterPendingReward = false,
            seed = 12345
        };

        if (compatibleAugment != null)
        {
            run.pendingReward.entries.Add(new PendingRewardEntry
            {
                rewardType = RunRewardType.Augment,
                contentId = contentRepository.GetAugmentId(compatibleAugment)
            });
        }

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
        Assert.That(loadedRun.deck[0].AppliedAugments.Count, Is.EqualTo(compatibleAugment != null ? 1 : 0));
        Assert.That(loadedRun.ownedAugments.Count, Is.EqualTo(compatibleAugment != null ? 1 : 0));
        Assert.That(loadedRun.pendingReward.entries.Count, Is.EqualTo(compatibleAugment != null ? 2 : 1));
        Assert.That(loadedRun.queuedNextMapTemplateId, Is.EqualTo("act_2"));
        Assert.That(loadedRun.endRunAfterPendingReward, Is.False);
    }

    [Test]
    public void SaveService_LoadProfile_DefaultsDebugUiDisabledWhenMissing()
    {
        SaveService saveService = new(contentRepository, saveDirectory);

        ProfileSaveData loadedProfile = saveService.LoadProfile();

        Assert.That(loadedProfile.debugUiEnabled, Is.False);
    }

    [Test]
    public void SaveService_MigratesLegacyCardOnlyRewardsIntoPendingEntries()
    {
        SaveService saveService = new(contentRepository, saveDirectory);
        MapTemplateDef template = CreateTemplate();
        RunMapGenerator generator = new(contentRepository);
        RunMapStateData mapState = generator.Generate(template, 321);

        RunSaveData run = new()
        {
            runId = "legacy-run",
            currentHealth = 18,
            maxHealth = 20,
            gold = 14,
            deck = new List<OwnedCard> { new(template.startingDeck[0]) },
            currentNodeId = mapState.startNodeId,
            completedNodeIds = new List<string> { mapState.startNodeId },
            mapState = mapState,
            pendingReward = new PendingRewardData
            {
                sourceNodeId = "legacy-node",
                offeredCardIds = new List<string> { contentRepository.GetCardId(template.startingDeck[0]) }
            },
            seed = 321
        };

        saveService.SaveRun(run);
        RunSaveData loadedRun = saveService.LoadRun("legacy-run");

        Assert.NotNull(loadedRun.pendingReward);
        Assert.That(loadedRun.pendingReward.entries.Count, Is.EqualTo(1));
        Assert.That(loadedRun.pendingReward.entries[0].rewardType, Is.EqualTo(RunRewardType.Card));
        Assert.That(loadedRun.pendingReward.entries[0].contentId, Is.EqualTo(contentRepository.GetCardId(template.startingDeck[0])));
    }

    [Test]
    public void RunCoordinator_SetDebugUiEnabled_PersistsAndNotifiesOncePerChange()
    {
        SaveService saveService = new(contentRepository, saveDirectory);
        RunCoordinator coordinator = new(saveService, contentRepository, _ => { });

        int notificationCount = 0;
        bool lastValue = false;
        coordinator.DebugUiChanged += enabled =>
        {
            notificationCount++;
            lastValue = enabled;
        };

        coordinator.SetDebugUiEnabled(true);

        string profilePath = Path.Combine(saveDirectory, "profile.json");
        Assert.That(File.Exists(profilePath), Is.True);
        Assert.That(saveService.LoadProfile().debugUiEnabled, Is.True);
        Assert.That(notificationCount, Is.EqualTo(1));
        Assert.That(lastValue, Is.True);

        System.DateTime firstWriteTime = File.GetLastWriteTimeUtc(profilePath);
        System.Threading.Thread.Sleep(20);
        coordinator.SetDebugUiEnabled(true);

        Assert.That(notificationCount, Is.EqualTo(1));
        Assert.That(File.GetLastWriteTimeUtc(profilePath), Is.EqualTo(firstWriteTime));
    }

    [Test]
    public void CardRewardPoolDef_ReturnsMixedCardAndAugmentChoices()
    {
        MapTemplateDef template = CreateTemplate();
        CardAugmentDef compatibleAugment = FindCompatibleAugment(template.startingDeck[0]);
        Assert.NotNull(compatibleAugment);

        CardRewardPoolDef rewardPool = ScriptableObject.CreateInstance<CardRewardPoolDef>();
        rewardPool.cards = new List<WeightedCardRewardEntry>
        {
            new() { card = template.startingDeck[0], weight = 1 }
        };
        rewardPool.augments = new List<WeightedAugmentRewardEntry>
        {
            new() { augment = compatibleAugment, weight = 1 }
        };
        rewardPool.choiceCount = 2;

        List<PendingRewardEntry> choices = rewardPool.GetRandomChoices(11, "mixed-reward");
        Assert.That(choices.Count, Is.EqualTo(2));
        Assert.That(choices.Exists(entry => entry.rewardType == RunRewardType.Card && entry.contentId == contentRepository.GetCardId(template.startingDeck[0])), Is.True);
        Assert.That(choices.Exists(entry => entry.rewardType == RunRewardType.Augment && entry.contentId == contentRepository.GetAugmentId(compatibleAugment)), Is.True);
    }

    [Test]
    public void CardRewardPoolDef_IgnoresInvalidEntriesAndAvoidsDuplicateRewards()
    {
        MapTemplateDef template = CreateTemplate();
        CardDef card = template.startingDeck[0];
        CardAugmentDef compatibleAugment = FindCompatibleAugment(card);
        Assert.NotNull(compatibleAugment);

        CardRewardPoolDef rewardPool = ScriptableObject.CreateInstance<CardRewardPoolDef>();
        rewardPool.cards = new List<WeightedCardRewardEntry>
        {
            new() { card = card, weight = 10 },
            new() { card = card, weight = 30 },
            new() { card = null, weight = 5 },
            new() { card = template.startingDeck.Count > 1 ? template.startingDeck[1] : null, weight = 0 }
        };
        rewardPool.augments = new List<WeightedAugmentRewardEntry>
        {
            new() { augment = compatibleAugment, weight = 1 },
            new() { augment = null, weight = 4 },
            new() { augment = compatibleAugment, weight = -1 }
        };
        rewardPool.choiceCount = 3;

        List<PendingRewardEntry> choices = rewardPool.GetRandomChoices(7, "dedupe");
        Assert.That(choices.Count, Is.EqualTo(2));
        Assert.That(CountRewards(choices, RunRewardType.Card, contentRepository.GetCardId(card)), Is.EqualTo(1));
        Assert.That(CountRewards(choices, RunRewardType.Augment, contentRepository.GetAugmentId(compatibleAugment)), Is.EqualTo(1));
    }

    [Test]
    public void ShopInventoryDef_GetRandomOffers_IsDeterministicAndIgnoresZeroWeightOffers()
    {
        ShopInventoryDef inventory = ScriptableObject.CreateInstance<ShopInventoryDef>();
        inventory.choiceCount = 2;
        inventory.offers = new List<ShopOfferData>
        {
            new() { id = "card", displayName = "Card", offerType = ShopOfferType.Card, price = 10, weight = 5 },
            new() { id = "augment", displayName = "Augment", offerType = ShopOfferType.Augment, price = 12, weight = 1 },
            new() { id = "heal", displayName = "Heal", offerType = ShopOfferType.Heal, price = 8, healAmount = 4, weight = 0 }
        };

        List<ShopOfferData> firstRoll = inventory.GetRandomOffers(123, "shop-a");
        List<ShopOfferData> secondRoll = inventory.GetRandomOffers(123, "shop-a");

        Assert.That(firstRoll.Count, Is.EqualTo(2));
        CollectionAssert.AreEqual(firstRoll.ConvertAll(offer => offer.OfferId), secondRoll.ConvertAll(offer => offer.OfferId));
        Assert.That(firstRoll.Exists(offer => offer.OfferId == "heal"), Is.False);
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
    public void RunFlowContent_EncounterIdsAreUnique()
    {
        EncounterDef[] encounters = Resources.LoadAll<EncounterDef>("RunFlow/Encounters");
        Dictionary<string, string> namesById = new();

        for (int i = 0; i < encounters.Length; i++)
        {
            EncounterDef encounter = encounters[i];
            Assert.NotNull(encounter);

            string encounterId = encounter.EncounterId;
            Assert.That(string.IsNullOrWhiteSpace(encounterId), Is.False, $"Encounter '{encounter.name}' is missing an id.");

            if (namesById.TryGetValue(encounterId, out string existingName))
                Assert.Fail($"Duplicate encounter id '{encounterId}' found on '{existingName}' and '{encounter.name}'.");

            namesById[encounterId] = encounter.name;
        }
    }

    [Test]
    public void RunFlowContent_EncounterPoolIdsAreUnique()
    {
        EncounterPoolDef[] encounterPools = Resources.LoadAll<EncounterPoolDef>("RunFlow");
        Dictionary<string, string> namesById = new();

        for (int i = 0; i < encounterPools.Length; i++)
        {
            EncounterPoolDef encounterPool = encounterPools[i];
            Assert.NotNull(encounterPool);

            string poolId = encounterPool.PoolId;
            Assert.That(string.IsNullOrWhiteSpace(poolId), Is.False, $"Encounter pool '{encounterPool.name}' is missing an id.");

            if (namesById.TryGetValue(poolId, out string existingName))
                Assert.Fail($"Duplicate encounter pool id '{poolId}' found on '{existingName}' and '{encounterPool.name}'.");

            namesById[poolId] = encounterPool.name;
        }
    }

    [Test]
    public void RunFlowContent_MapTemplatesLoadFromActsFolderAndHaveUniqueIds()
    {
        MapTemplateDef[] templates = Resources.LoadAll<MapTemplateDef>("RunFlow");
        Dictionary<string, string> namesById = new();
        int defaultTemplateCount = 0;

        Assert.That(templates.Length, Is.GreaterThanOrEqualTo(3));
        for (int i = 0; i < templates.Length; i++)
        {
            MapTemplateDef template = templates[i];
            Assert.NotNull(template);

            string templateId = template.TemplateId;
            Assert.That(string.IsNullOrWhiteSpace(templateId), Is.False, $"Map template '{template.name}' is missing an id.");

            if (namesById.TryGetValue(templateId, out string existingName))
                Assert.Fail($"Duplicate map template id '{templateId}' found on '{existingName}' and '{template.name}'.");

            namesById[templateId] = template.name;
            defaultTemplateCount += template.isDefaultStartTemplate ? 1 : 0;
        }

        Assert.That(defaultTemplateCount, Is.EqualTo(1));

        MapTemplateDef act1 = contentRepository.GetMapTemplateById("act_1");
        MapTemplateDef act2 = contentRepository.GetMapTemplateById("act_2");
        MapTemplateDef act3 = contentRepository.GetMapTemplateById("act_3");

        Assert.NotNull(act1);
        Assert.NotNull(act2);
        Assert.NotNull(act3);
        Assert.That(AssetDatabase.GetAssetPath(act1), Does.Contain("Assets/Resources/RunFlow/Acts/"));
        Assert.That(AssetDatabase.GetAssetPath(act2), Does.Contain("Assets/Resources/RunFlow/Acts/"));
        Assert.That(AssetDatabase.GetAssetPath(act3), Does.Contain("Assets/Resources/RunFlow/Acts/"));
        Assert.That(act1.nextActTemplate, Is.EqualTo(act2));
        Assert.That(act2.nextActTemplate, Is.EqualTo(act3));
        Assert.That(act3.nextActTemplate, Is.Null);
    }

    [Test]
    public void RunContentRepository_GetDefaultMapTemplate_PrefersExplicitDefaultTemplate()
    {
        MapTemplateDef defaultTemplate = contentRepository.GetDefaultMapTemplate();

        Assert.NotNull(defaultTemplate);
        Assert.That(defaultTemplate.TemplateId, Is.EqualTo("act_1"));
        Assert.That(defaultTemplate.displayName, Is.EqualTo("Act 1"));
        Assert.True(defaultTemplate.isDefaultStartTemplate);
    }

    [Test]
    public void RunContentRepository_SelectDefaultMapTemplate_FallsBackDeterministicallyWhenNoTemplateIsMarkedDefault()
    {
        MapTemplateDef laterTemplate = CreateRuntimeTemplate("z_template", "Later Template");
        MapTemplateDef earlierTemplate = CreateRuntimeTemplate("a_template", "Earlier Template");

        LogAssert.Expect(LogType.Warning, "No map template is marked as the default start template. Using 'a_template'.");
        MapTemplateDef selectedTemplate = InvokeSelectDefaultMapTemplate(new List<MapTemplateDef> { laterTemplate, earlierTemplate });

        Assert.That(selectedTemplate, Is.EqualTo(earlierTemplate));
    }

    [Test]
    public void RunContentRepository_Refresh_WarnsOnDuplicateTemplateIdsAndKeepsDeterministicFirstTemplate()
    {
        CreateMapTemplateAsset("AA Duplicate Map Template", "duplicate_map_template", "Duplicate Alpha");
        CreateMapTemplateAsset("ZZ Duplicate Map Template", "duplicate_map_template", "Duplicate Omega");

        LogAssert.Expect(LogType.Warning, "Duplicate map template id 'duplicate_map_template' found on 'ZZ Duplicate Map Template'. Existing template 'AA Duplicate Map Template' will be kept. Map template ids must be unique.");

        RunContentRepository repository = new();
        repository.Refresh();

        MapTemplateDef duplicateTemplate = repository.GetMapTemplateById("duplicate_map_template");
        Assert.NotNull(duplicateTemplate);
        Assert.That(duplicateTemplate.name, Is.EqualTo("AA Duplicate Map Template"));
        Assert.That(duplicateTemplate.displayName, Is.EqualTo("Duplicate Alpha"));
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
    public void RunCoordinator_BossVictory_QueuesRewardsBeforeTransitioningToNextAct()
    {
        SaveService saveService = new(contentRepository, saveDirectory);
        ProfileSaveData profile = new() { activeRunId = "boss-next-act" };
        saveService.SaveProfile(profile);

        MapTemplateDef act1 = contentRepository.GetMapTemplateById("act_1");
        Assert.NotNull(act1);
        Assert.NotNull(act1.nextActTemplate);

        CardDef rewardCard = GetFirstCard();
        RunSaveData run = CreateBossRun("boss-next-act", act1.TemplateId);
        int deckCountBeforeReward = run.deck.Count;
        saveService.SaveRun(run);

        string loadedScene = null;
        RunCoordinator coordinator = new(saveService, contentRepository, sceneName => loadedScene = sceneName);
        EncounterDef bossEncounter = CreateBossEncounterWithCardReward(rewardCard, goldReward: 11, metaCurrencyReward: 3);

        coordinator.HandleCombatResult(new CombatSceneResult("boss-node", bossEncounter, true, 15));

        Assert.That(loadedScene, Is.EqualTo(SceneNames.RunMap));
        Assert.NotNull(coordinator.CurrentRun);
        Assert.That(coordinator.CurrentMapTemplate.TemplateId, Is.EqualTo("act_1"));
        Assert.NotNull(coordinator.CurrentRun.pendingReward);
        Assert.That(coordinator.CurrentRun.queuedNextMapTemplateId, Is.EqualTo("act_2"));
        Assert.That(coordinator.CurrentRun.endRunAfterPendingReward, Is.False);

        PendingRewardEntry reward = coordinator.GetPendingRewards()[0];
        Assert.True(coordinator.ClaimPendingReward(reward.rewardType, reward.contentId));

        Assert.That(loadedScene, Is.EqualTo(SceneNames.RunMap));
        Assert.NotNull(coordinator.CurrentRun);
        Assert.That(coordinator.CurrentMapTemplate.TemplateId, Is.EqualTo("act_2"));
        Assert.That(coordinator.CurrentRun.mapState.mapTemplateId, Is.EqualTo("act_2"));
        Assert.That(coordinator.CurrentRun.currentHealth, Is.EqualTo(15));
        Assert.That(coordinator.CurrentRun.gold, Is.EqualTo(21));
        Assert.That(coordinator.CurrentRun.pendingReward, Is.Null);
        Assert.That(coordinator.CurrentRun.queuedNextMapTemplateId, Is.Null);
        Assert.That(coordinator.CurrentRun.endRunAfterPendingReward, Is.False);
        Assert.That(coordinator.CurrentRun.completedNodeIds.Count, Is.EqualTo(1));
        Assert.That(coordinator.CurrentRun.deck.Count, Is.EqualTo(deckCountBeforeReward + (reward.rewardType == RunRewardType.Card ? 1 : 0)));
    }

    [Test]
    public void RunCoordinator_SkipPendingBossReward_AfterReload_TransitionsToNextAct()
    {
        SaveService saveService = new(contentRepository, saveDirectory);
        ProfileSaveData profile = new() { activeRunId = "boss-reload" };
        saveService.SaveProfile(profile);

        MapTemplateDef act1 = contentRepository.GetMapTemplateById("act_1");
        Assert.NotNull(act1);

        RunSaveData run = CreateBossRun("boss-reload", act1.TemplateId);
        saveService.SaveRun(run);

        string loadedScene = null;
        RunCoordinator coordinator = new(saveService, contentRepository, sceneName => loadedScene = sceneName);
        coordinator.HandleCombatResult(new CombatSceneResult("boss-node", CreateBossEncounterWithCardReward(GetFirstCard()), true, 16));

        RunCoordinator reloadedCoordinator = new(saveService, contentRepository, sceneName => loadedScene = sceneName);
        Assert.NotNull(reloadedCoordinator.CurrentRun);
        Assert.NotNull(reloadedCoordinator.CurrentRun.pendingReward);
        Assert.That(reloadedCoordinator.CurrentRun.queuedNextMapTemplateId, Is.EqualTo("act_2"));

        reloadedCoordinator.SkipPendingReward();

        Assert.That(loadedScene, Is.EqualTo(SceneNames.RunMap));
        Assert.NotNull(reloadedCoordinator.CurrentRun);
        Assert.That(reloadedCoordinator.CurrentMapTemplate.TemplateId, Is.EqualTo("act_2"));
        Assert.That(reloadedCoordinator.CurrentRun.mapState.mapTemplateId, Is.EqualTo("act_2"));
        Assert.That(reloadedCoordinator.CurrentRun.pendingReward, Is.Null);
        Assert.That(reloadedCoordinator.CurrentRun.queuedNextMapTemplateId, Is.Null);
        Assert.That(reloadedCoordinator.CurrentRun.endRunAfterPendingReward, Is.False);
    }

    [Test]
    public void RunCoordinator_FinalBossVictory_WaitsForRewardThenEndsRun()
    {
        SaveService saveService = new(contentRepository, saveDirectory);
        ProfileSaveData profile = new() { activeRunId = "boss-final" };
        saveService.SaveProfile(profile);

        RunSaveData run = CreateBossRun("boss-final", "act_3");
        string runId = run.runId;
        saveService.SaveRun(run);

        string loadedScene = null;
        RunCoordinator coordinator = new(saveService, contentRepository, sceneName => loadedScene = sceneName);
        coordinator.HandleCombatResult(new CombatSceneResult("boss-node", CreateBossEncounterWithCardReward(GetFirstCard(), goldReward: 7), true, 12));

        Assert.That(loadedScene, Is.EqualTo(SceneNames.RunMap));
        Assert.NotNull(coordinator.CurrentRun);
        Assert.NotNull(coordinator.CurrentRun.pendingReward);
        Assert.That(coordinator.CurrentRun.queuedNextMapTemplateId, Is.Null);
        Assert.That(coordinator.CurrentRun.endRunAfterPendingReward, Is.True);

        coordinator.SkipPendingReward();

        Assert.That(loadedScene, Is.EqualTo(SceneNames.MainMenu));
        Assert.IsNull(coordinator.CurrentRun);
        Assert.IsNull(saveService.LoadRun(runId));
        Assert.That(coordinator.Profile.activeRunId, Is.Null);
        Assert.True(coordinator.Profile.HasUnlock("unlock.first_run_clear"));
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
        CardRewardPoolDef rewardPool = AssetDatabase.LoadAssetAtPath<CardRewardPoolDef>("Assets/Resources/RunFlow/Rewards/Act 1 Rewards.asset");
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

    [Test]
    public void RunFlowProjectSetup_LoadDefaultPathPrefab_ReturnsStarterCombatPath()
    {
        MethodInfo loadDefaultPathPrefab = typeof(RunFlowProjectSetup).GetMethod("LoadDefaultPathPrefab", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(loadDefaultPathPrefab);

        GameObject pathPrefab = (GameObject)loadDefaultPathPrefab.Invoke(null, null);
        Assert.NotNull(pathPrefab);
        Assert.That(AssetDatabase.GetAssetPath(pathPrefab), Is.EqualTo("Assets/Resources/RunFlow/Paths/StarterCombatPath 1.prefab"));
    }

    [Test]
    public void MainMenuSceneController_EnsurePauseMenu_CreatesSettingsOnlyMenu()
    {
        ConfigureGameFlowRoot();
        GameObject controllerObject = new("MainMenu Controller");
        MainMenuSceneController controller = controllerObject.AddComponent<MainMenuSceneController>();

        InvokePrivateMethod(controller, "EnsurePauseMenu");

        PauseMenuController pauseMenu = controllerObject.GetComponent<PauseMenuController>();
        Assert.NotNull(pauseMenu);
        Assert.That(GetPrivateField<bool>(pauseMenu, "pauseGameplay"), Is.False);

        Object.DestroyImmediate(controllerObject);
    }

    [Test]
    public void RunMapSceneController_EnsurePauseMenu_CreatesSettingsOnlyMenu()
    {
        ConfigureGameFlowRoot();
        GameObject controllerObject = new("RunMap Controller");
        RunMapSceneController controller = controllerObject.AddComponent<RunMapSceneController>();

        InvokePrivateMethod(controller, "EnsurePauseMenu");

        PauseMenuController pauseMenu = controllerObject.GetComponent<PauseMenuController>();
        Assert.NotNull(pauseMenu);
        Assert.That(GetPrivateField<bool>(pauseMenu, "pauseGameplay"), Is.False);

        Object.DestroyImmediate(controllerObject);
    }

    [Test]
    public void CombatSceneBootstrapper_EnsurePauseMenu_CreatesPauseMenuAndHooksPauseState()
    {
        ConfigureGameFlowRoot();
        GameObject bootstrapperObject = new("Combat Bootstrapper");
        CombatSceneBootstrapper bootstrapper = bootstrapperObject.AddComponent<CombatSceneBootstrapper>();
        CombatSessionDriver combatSessionDriver = bootstrapperObject.AddComponent<CombatSessionDriver>();
        SetPrivateField(bootstrapper, "combatSessionDriver", combatSessionDriver);

        InvokePrivateMethod(bootstrapper, "EnsurePauseMenu");

        PauseMenuController pauseMenu = bootstrapperObject.GetComponent<PauseMenuController>();
        Assert.NotNull(pauseMenu);
        Assert.That(GetPrivateField<bool>(pauseMenu, "pauseGameplay"), Is.True);

        pauseMenu.OpenMenu();
        Assert.That(combatSessionDriver.IsPaused, Is.True);
        pauseMenu.CloseMenu();
        Assert.That(combatSessionDriver.IsPaused, Is.False);

        Object.DestroyImmediate(bootstrapperObject);
    }

    [Test]
    public void BattleHUD_ResolvedSessionText_FollowsCoordinatorDebugFlag()
    {
        RunCoordinator coordinator = ConfigureGameFlowRoot();

        GameObject hudObject = new("BattleHUD");
        BattleHUD battleHUD = hudObject.AddComponent<BattleHUD>();
        GameObject resolvedTextObject = new("ResolvedText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        resolvedTextObject.transform.SetParent(hudObject.transform, false);
        TextMeshProUGUI resolvedText = resolvedTextObject.GetComponent<TextMeshProUGUI>();

        SetPrivateField(battleHUD, "resolvedSessionText", resolvedText);

        GameObject driverObject = new("CombatSessionDriver");
        CombatSessionDriver combatSessionDriver = driverObject.AddComponent<CombatSessionDriver>();

        battleHUD.Initialize(new PlayerState(), combatSessionDriver);
        Assert.That(resolvedText.gameObject.activeSelf, Is.False);

        coordinator.SetDebugUiEnabled(true);
        Assert.That(resolvedText.gameObject.activeSelf, Is.True);

        Object.DestroyImmediate(driverObject);
        Object.DestroyImmediate(hudObject);
    }

    [Test]
    public void RunFlowProjectSetup_CreateCombatScene_ConfiguresExistingCombatScene()
    {
        MethodInfo createCombatScene = typeof(RunFlowProjectSetup).GetMethod("CreateCombatScene", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(createCombatScene);

        List<EncounterDef> regularFights = contentRepository.GetEncountersByKind(EncounterKind.RegularFight);
        Assert.That(regularFights.Count, Is.GreaterThan(0));

        EncounterDef debugEncounter = regularFights[0];
        List<CardDef> cards = new(contentRepository.Cards);
        createCombatScene.Invoke(null, new object[] { debugEncounter, cards });

        _ = EditorSceneManager.OpenScene("Assets/Scenes/Combat.unity", OpenSceneMode.Single);

        CombatSessionDriver combatSessionDriver = Object.FindAnyObjectByType<CombatSessionDriver>();
        HandViewDriver handViewDriver = Object.FindAnyObjectByType<HandViewDriver>();
        EnemySpawner enemySpawner = Object.FindAnyObjectByType<EnemySpawner>();
        EnemyManager enemyManager = Object.FindAnyObjectByType<EnemyManager>();
        BattleHUD battleHUD = Object.FindAnyObjectByType<BattleHUD>();
        GameObject pathAnchor = GameObject.Find("Path Anchor");
        CombatSceneBootstrapper[] bootstrappers = Object.FindObjectsByType<CombatSceneBootstrapper>();
        CombatOutcomeWatcher[] outcomeWatchers = Object.FindObjectsByType<CombatOutcomeWatcher>();

        Assert.NotNull(combatSessionDriver);
        Assert.NotNull(handViewDriver);
        Assert.NotNull(enemySpawner);
        Assert.NotNull(enemyManager);
        Assert.NotNull(battleHUD);
        Assert.NotNull(pathAnchor);
        Assert.That(bootstrappers.Length, Is.EqualTo(1));
        Assert.That(outcomeWatchers.Length, Is.EqualTo(1));

        CombatSceneBootstrapper bootstrapper = bootstrappers[0];
        CombatOutcomeWatcher outcomeWatcher = outcomeWatchers[0];
        Assert.That(GetPrivateField<CombatSessionDriver>(bootstrapper, "combatSessionDriver"), Is.EqualTo(combatSessionDriver));
        Assert.That(GetPrivateField<HandViewDriver>(bootstrapper, "handViewDriver"), Is.EqualTo(handViewDriver));
        Assert.That(GetPrivateField<EnemySpawner>(bootstrapper, "enemySpawner"), Is.EqualTo(enemySpawner));
        Assert.That(GetPrivateField<Transform>(bootstrapper, "pathAnchor"), Is.EqualTo(pathAnchor.transform));
        Assert.That(GetPrivateField<EncounterDef>(bootstrapper, "debugEncounter"), Is.EqualTo(debugEncounter));
        Assert.That(GetPrivateField<int>(bootstrapper, "debugCurrentHealth"), Is.EqualTo(20));
        Assert.That(GetPrivateField<int>(bootstrapper, "debugMaxHealth"), Is.EqualTo(20));

        List<OwnedCard> debugDeck = GetPrivateField<List<OwnedCard>>(bootstrapper, "debugDeck");
        Assert.NotNull(debugDeck);
        Assert.That(debugDeck.Count, Is.EqualTo(Mathf.Min(5, cards.Count)));

        Assert.That(GetPrivateField<CombatSessionDriver>(outcomeWatcher, "combatSessionDriver"), Is.EqualTo(combatSessionDriver));
        Assert.That(GetPrivateField<EnemySpawner>(outcomeWatcher, "enemySpawner"), Is.EqualTo(enemySpawner));
        Assert.That(GetPrivateField<EnemyManager>(outcomeWatcher, "enemyManager"), Is.EqualTo(enemyManager));
        Assert.NotNull(GetPrivateField<Object>(battleHUD, "speedButton"));
        Assert.NotNull(GetPrivateField<Object>(battleHUD, "speedButtonText"));
        Assert.NotNull(GetPrivateField<Object>(battleHUD, "resolvedSessionText"));
        Assert.False(GetPrivateField<bool>(handViewDriver, "autoInitializeOnStart"));
        Assert.False(GetPrivateField<bool>(enemySpawner, "startOnPlay"));
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

    private string CreateMapTemplateAsset(string assetName, string templateId, string displayName, bool isDefaultStartTemplate = false, MapTemplateDef nextActTemplate = null)
    {
        MapTemplateDef template = ScriptableObject.CreateInstance<MapTemplateDef>();
        template.name = assetName;
        template.id = templateId;
        template.displayName = displayName;
        template.isDefaultStartTemplate = isDefaultStartTemplate;
        template.nextActTemplate = nextActTemplate;
        template.totalPlayableNodes = 8;
        template.maxActivePaths = 3;
        template.minColumns = 5;
        template.maxColumns = 6;

        string assetPath = $"Assets/Resources/RunFlow/{assetName}.asset";
        createdAssetPaths.Add(assetPath);
        AssetDatabase.CreateAsset(template, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return assetPath;
    }

    private static MapTemplateDef CreateRuntimeTemplate(string templateId, string templateName, bool isDefaultStartTemplate = false)
    {
        MapTemplateDef template = ScriptableObject.CreateInstance<MapTemplateDef>();
        template.name = templateName;
        template.id = templateId;
        template.displayName = templateName;
        template.isDefaultStartTemplate = isDefaultStartTemplate;
        return template;
    }

    private CardDef GetFirstCard()
    {
        foreach (CardDef card in contentRepository.Cards)
        {
            if (card != null)
                return card;
        }

        Assert.Fail("Expected at least one card definition.");
        return null;
    }

    private RunSaveData CreateBossRun(string runId, string templateId)
    {
        CardDef card = GetFirstCard();
        return new RunSaveData
        {
            runId = runId,
            currentHealth = 20,
            maxHealth = 20,
            gold = 10,
            deck = new List<OwnedCard> { new OwnedCard(card, "boss-card-1", null) },
            ownedAugments = new List<OwnedAugment>(),
            currentNodeId = "boss-node",
            completedNodeIds = new List<string>(),
            mapState = CreateSingleBossMapState(templateId),
            pendingReward = null,
            queuedNextMapTemplateId = null,
            endRunAfterPendingReward = false,
            seed = 123
        };
    }

    private EncounterDef CreateBossEncounterWithCardReward(CardDef rewardCard, int goldReward = 0, int metaCurrencyReward = 0)
    {
        CardRewardPoolDef rewardPool = ScriptableObject.CreateInstance<CardRewardPoolDef>();
        rewardPool.choiceCount = 1;
        rewardPool.cards = new List<WeightedCardRewardEntry>
        {
            new() { card = rewardCard, weight = 1 }
        };

        EncounterDef encounter = ScriptableObject.CreateInstance<EncounterDef>();
        encounter.id = "test-boss";
        encounter.displayName = "Test Boss";
        encounter.encounterKind = EncounterKind.Boss;
        encounter.rewardPool = rewardPool;
        encounter.goldReward = goldReward;
        encounter.metaCurrencyReward = metaCurrencyReward;
        return encounter;
    }

    private CardAugmentDef FindCompatibleAugment(CardDef card)
    {
        foreach (CardAugmentDef augment in contentRepository.Augments)
        {
            if (augment != null && augment.IsCompatible(card))
                return augment;
        }

        return null;
    }

    private static int CountRewards(List<PendingRewardEntry> rewards, RunRewardType rewardType, string contentId)
    {
        int count = 0;
        for (int i = 0; i < rewards.Count; i++)
        {
            PendingRewardEntry reward = rewards[i];
            if (reward != null && reward.rewardType == rewardType && reward.contentId == contentId)
                count++;
        }

        return count;
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

    private static RunMapStateData CreateSingleBossMapState(string templateId)
    {
        return new RunMapStateData
        {
            mapTemplateId = templateId,
            startNodeId = "boss-node",
            nodes = new List<RunMapNodeData>
            {
                new()
                {
                    nodeId = "boss-node",
                    displayName = "Boss",
                    nodeType = MapNodeType.Boss,
                    column = 0,
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

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(field, $"Missing field '{fieldName}' on {target.GetType().Name}.");
        return (T)field.GetValue(target);
    }

    private static MapTemplateDef InvokeSelectDefaultMapTemplate(IReadOnlyList<MapTemplateDef> templates)
    {
        MethodInfo method = typeof(RunContentRepository).GetMethod("SelectDefaultMapTemplate", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (MapTemplateDef)method.Invoke(null, new object[] { templates });
    }

    private RunCoordinator ConfigureGameFlowRoot()
    {
        ClearGameFlowRoot();

        SaveService saveService = new(contentRepository, saveDirectory);
        RunCoordinator coordinator = new(saveService, contentRepository, _ => { });
        GameFlowRoot root = GameFlowRoot.EnsureInstance();
        root.OverrideServices(contentRepository, saveService, coordinator);
        return coordinator;
    }

    private static void InvokePrivateMethod(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(target, null);
    }

    private static void ClearGameFlowRoot()
    {
        GameFlowRoot root = GameFlowRoot.Instance;
        if (root != null)
            Object.DestroyImmediate(root.gameObject);

        FieldInfo instanceField = typeof(GameFlowRoot).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        instanceField?.SetValue(null, null);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(field, $"Missing field '{fieldName}' on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
