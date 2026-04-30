using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cards;
using Combat;
using Enemies;
using NUnit.Framework;
using Relics;
using RunFlow;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
                }
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
    public void RunCoordinator_ResetMetaProgress_ClearsUnlocksAndCurrencyButPreservesRunAndDebugFlag()
    {
        SaveService saveService = new(contentRepository, saveDirectory);
        ProfileSaveData profile = new()
        {
            metaCurrency = 17,
            activeRunId = "debug-run",
            debugUiEnabled = true
        };
        profile.AddUnlock("unlock.test");
        saveService.SaveProfile(profile);
        saveService.SaveRun(CreateBossRun("debug-run", "act_debug"));

        RunCoordinator coordinator = new(saveService, contentRepository, _ => { });
        coordinator.ResetMetaProgress();

        ProfileSaveData loadedProfile = saveService.LoadProfile();
        Assert.That(loadedProfile.metaCurrency, Is.EqualTo(0));
        Assert.That(loadedProfile.unlockIds, Is.Empty);
        Assert.That(loadedProfile.activeRunId, Is.EqualTo("debug-run"));
        Assert.That(loadedProfile.debugUiEnabled, Is.True);
    }

    [Test]
    public void RunCoordinator_DebugMutations_PersistRunAndProfileChanges()
    {
        SaveService saveService = new(contentRepository, saveDirectory);
        ProfileSaveData profile = new()
        {
            metaCurrency = 4,
            activeRunId = "mutate-run",
            debugUiEnabled = true
        };
        saveService.SaveProfile(profile);
        saveService.SaveRun(CreateBossRun("mutate-run", "act_debug"));

        RunCoordinator coordinator = new(saveService, contentRepository, _ => { });
        CardDef firstCard = GetFirstCard();
        CardAugmentDef firstAugment = FindCompatibleAugment(firstCard);
        Assert.NotNull(firstAugment);

        Assert.True(coordinator.TryGainCurrency(12, out _));
        Assert.True(coordinator.TryGainMetaCurrency(6, out _));
        Assert.True(coordinator.TrySetLives(9, out _));
        Assert.True(coordinator.TryAddLives(-3, out _));
        Assert.True(coordinator.TryAddCardById(contentRepository.GetCardId(firstCard), out _));
        Assert.True(coordinator.TryAddAugmentById(contentRepository.GetAugmentId(firstAugment), out _));

        ProfileSaveData loadedProfile = saveService.LoadProfile();
        RunSaveData loadedRun = saveService.LoadRun("mutate-run");

        Assert.That(loadedProfile.metaCurrency, Is.EqualTo(10));
        Assert.That(loadedRun.gold, Is.EqualTo(22));
        Assert.That(loadedRun.currentHealth, Is.EqualTo(6));
        Assert.That(loadedRun.deck.Count, Is.EqualTo(2));
        Assert.That(loadedRun.deck[1].CurrentDefinition, Is.EqualTo(firstCard));
        Assert.That(loadedRun.ownedAugments.Count, Is.EqualTo(1));
        Assert.That(loadedRun.ownedAugments[0].Definition, Is.EqualTo(firstAugment));
    }

    [Test]
    public void RunCoordinator_DebugMutations_RejectMissingRunAndInvalidIdsWithoutMutatingState()
    {
        SaveService saveService = new(contentRepository, saveDirectory);
        saveService.SaveProfile(new ProfileSaveData { metaCurrency = 3 });
        RunCoordinator coordinator = new(saveService, contentRepository, _ => { });

        Assert.False(coordinator.TryGainCurrency(5, out _));
        Assert.False(coordinator.TrySetLives(10, out _));
        Assert.False(coordinator.TryAddLives(2, out _));
        Assert.False(coordinator.TryAddCardById("missing-card", out _));
        Assert.False(coordinator.TryAddAugmentById("missing-augment", out _));

        ProfileSaveData loadedProfile = saveService.LoadProfile();
        Assert.That(loadedProfile.metaCurrency, Is.EqualTo(3));
        Assert.That(loadedProfile.activeRunId, Is.Null);
    }

    [Test]
    public void DebugConsoleCommandProcessor_ParsesConfiguredCommandsAndWhitespaceInsensitiveCommands()
    {
        SaveService saveService = new(contentRepository, saveDirectory);
        ProfileSaveData profile = new()
        {
            metaCurrency = 2,
            activeRunId = "parser-run",
            debugUiEnabled = true
        };
        saveService.SaveProfile(profile);
        saveService.SaveRun(CreateBossRun("parser-run", "act_debug"));

        RunCoordinator coordinator = new(saveService, contentRepository, _ => { });
        DebugConsoleCommandProcessor processor = new(coordinator);
        CardDef firstCard = GetFirstCard();
        CardAugmentDef firstAugment = FindCompatibleAugment(firstCard);
        Assert.NotNull(firstAugment);

        DebugConsoleCommandResult currencyResult = processor.Execute("  MONEY   ADD   7 ");
        DebugConsoleCommandResult metaResult = processor.Execute("meta add 3");
        DebugConsoleCommandResult livesResult = processor.Execute("lives add -4");
        DebugConsoleCommandResult cardResult = processor.Execute($"card add {contentRepository.GetCardId(firstCard)}");
        DebugConsoleCommandResult augmentResult = processor.Execute($"AUGMENT ADD {contentRepository.GetAugmentId(firstAugment)}");

        Assert.That(currencyResult.Success, Is.True);
        Assert.That(metaResult.Success, Is.True);
        Assert.That(livesResult.Success, Is.True);
        Assert.That(cardResult.Success, Is.True);
        Assert.That(augmentResult.Success, Is.True);

        RunSaveData loadedRun = saveService.LoadRun("parser-run");
        ProfileSaveData loadedProfile = saveService.LoadProfile();
        Assert.That(loadedRun.gold, Is.EqualTo(17));
        Assert.That(loadedRun.currentHealth, Is.EqualTo(16));
        Assert.That(loadedRun.deck.Count, Is.EqualTo(2));
        Assert.That(loadedRun.ownedAugments.Count, Is.EqualTo(1));
        Assert.That(loadedProfile.metaCurrency, Is.EqualTo(5));
    }

    [Test]
    public void DebugConsoleCommandProcessor_ReturnsHelpfulErrorsForMalformedCommands()
    {
        DebugConsoleCommandProcessor processor = new(new RunCoordinator(new SaveService(contentRepository, saveDirectory), contentRepository, _ => { }));

        DebugConsoleCommandResult malformedLives = processor.Execute("lives nope 4");
        DebugConsoleCommandResult malformedAmount = processor.Execute("money add nope");
        DebugConsoleCommandResult unknownCommand = processor.Execute("warp core");

        Assert.That(malformedLives.Success, Is.False);
        Assert.That(malformedLives.Message, Does.Contain("lives set"));
        Assert.That(malformedAmount.Success, Is.False);
        Assert.That(malformedAmount.Message, Does.Contain("integer amount"));
        Assert.That(unknownCommand.Success, Is.False);
        Assert.That(unknownCommand.Message, Does.Contain("Unknown command"));
    }

    [Test]
    public void DebugConsoleCommandProcessor_Help_ListsAvailableCommands()
    {
        DebugConsoleCommandProcessor processor = new(new RunCoordinator(new SaveService(contentRepository, saveDirectory), contentRepository, _ => { }));

        DebugConsoleCommandResult helpResult = processor.Execute("help");

        Assert.That(helpResult.Success, Is.True);
        Assert.That(helpResult.Message, Does.Contain("Available commands"));
        Assert.That(helpResult.Message, Does.Contain("meta reset"));
        Assert.That(helpResult.Message, Does.Contain("money add <amount>"));
        Assert.That(helpResult.Message, Does.Contain("meta add <amount>"));
        Assert.That(helpResult.Message, Does.Contain("card add <id>"));
        Assert.That(helpResult.Message, Does.Contain("augment add <id>"));
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
    public void RunContentRepository_LoadsTestingRelics()
    {
        Assert.NotNull(contentRepository.GetRelicById("mana_battery"));
        Assert.NotNull(contentRepository.GetRelicById("tower_discount"));
        Assert.NotNull(contentRepository.GetRelicById("spell_discount"));
        Assert.NotNull(contentRepository.GetRelicById("deep_pockets"));
    }

    [Test]
    public void SaveService_RoundTripsOwnedRelicsAndDiscoveredRelics()
    {
        RelicDef relic = contentRepository.GetRelicById("mana_battery");
        Assert.NotNull(relic);

        SaveService saveService = new(contentRepository, saveDirectory);
        ProfileSaveData profile = new() { activeRunId = "relic-run" };
        profile.AddDiscoveredRelic(relic.RelicId);
        saveService.SaveProfile(profile);

        RunSaveData run = new()
        {
            runId = "relic-run",
            currentHealth = 20,
            maxHealth = 20,
            gold = 0,
            deck = new List<OwnedCard> { new(GetFirstCard()) },
            ownedRelics = new List<OwnedRelic> { new(relic) },
            completedNodeIds = new List<string>(),
            mapState = new RunMapStateData(),
            seed = 7
        };
        saveService.SaveRun(run);

        ProfileSaveData loadedProfile = saveService.LoadProfile();
        RunSaveData loadedRun = saveService.LoadRun("relic-run");

        Assert.True(loadedProfile.HasDiscoveredRelic(relic.RelicId));
        Assert.NotNull(loadedRun);
        Assert.That(loadedRun.ownedRelics.Count, Is.EqualTo(1));
        Assert.That(loadedRun.ownedRelics[0].Definition, Is.EqualTo(relic));
    }

    [Test]
    public void RunCoordinator_TryPurchaseShopOffer_AddsRelicDiscoversAndFiltersDuplicates()
    {
        RelicDef relic = contentRepository.GetRelicById("mana_battery");
        Assert.NotNull(relic);

        ShopInventoryDef inventory = contentRepository.GetShopInventoryById("act_1_shop");
        Assert.NotNull(inventory);

        int originalChoiceCount = inventory.choiceCount;
        List<ShopOfferData> originalOffers = new(inventory.offers);

        try
        {
            inventory.choiceCount = 1;
            inventory.offers = new List<ShopOfferData>
            {
                new() { id = "test-relic", displayName = "Test Relic", offerType = ShopOfferType.Relic, price = 10, relic = relic, weight = 1 }
            };

            SaveService saveService = new(contentRepository, saveDirectory);
            RunSaveData run = new()
            {
                runId = "shop-relic-run",
                currentHealth = 20,
                maxHealth = 20,
                gold = 25,
                deck = new List<OwnedCard> { new(GetFirstCard()) },
                currentNodeId = "shop-a",
                completedNodeIds = new List<string>(),
                mapState = new RunMapStateData
                {
                    mapTemplateId = "test-template",
                    startNodeId = "shop-a",
                    nodes = new List<RunMapNodeData>
                    {
                        new() { nodeId = "shop-a", displayName = "Shop A", nodeType = MapNodeType.Shop, shopInventoryId = inventory.InventoryId },
                        new() { nodeId = "shop-b", displayName = "Shop B", nodeType = MapNodeType.Shop, shopInventoryId = inventory.InventoryId }
                    }
                },
                seed = 123
            };

            saveService.SaveProfile(new ProfileSaveData { activeRunId = run.runId });
            saveService.SaveRun(run);

            RunCoordinator coordinator = new(saveService, contentRepository, _ => { });
            List<ShopOfferData> offers = coordinator.GetAvailableShopOffers("shop-a");
            Assert.That(offers.Count, Is.EqualTo(1));
            Assert.That(coordinator.GetResolvedShopOfferPrice(offers[0]), Is.EqualTo(10));

            Assert.True(coordinator.TryPurchaseShopOffer("shop-a", "test-relic"));
            Assert.That(coordinator.CurrentRun.gold, Is.EqualTo(15));
            Assert.That(coordinator.GetOwnedRelics().Count, Is.EqualTo(1));
            Assert.That(coordinator.GetOwnedRelics()[0].Definition, Is.EqualTo(relic));
            Assert.True(coordinator.Profile.HasDiscoveredRelic(relic.RelicId));

            Assert.False(coordinator.TryPurchaseShopOffer("shop-a", "test-relic"));
            Assert.That(coordinator.CurrentRun.gold, Is.EqualTo(15));
            Assert.That(coordinator.GetAvailableShopOffers("shop-b").Count, Is.EqualTo(0));
        }
        finally
        {
            inventory.choiceCount = originalChoiceCount;
            inventory.offers = originalOffers;
        }
    }

    [Test]
    public void RelicCardModifier_ModifiesMatchingCardCostsAndClampsToZero()
    {
        CardDef towerCard = GetFirstCardOfType(CardType.Tower);
        CardDef spellCard = GetFirstCardOfType(CardType.Spell);

        RelicCardModifierDef towerDiscount = ScriptableObject.CreateInstance<RelicCardModifierDef>();
        towerDiscount.allowedCardTypes = new List<CardType> { CardType.Tower };
        towerDiscount.manaCostDelta = -1;

        RelicCardModifierDef spellDiscount = ScriptableObject.CreateInstance<RelicCardModifierDef>();
        spellDiscount.allowedCardTypes = new List<CardType> { CardType.Spell };
        spellDiscount.manaCostDelta = -1;

        RelicDef towerRelic = ScriptableObject.CreateInstance<RelicDef>();
        towerRelic.effects = new List<RelicEffectDef> { towerDiscount };

        RelicDef spellRelic = ScriptableObject.CreateInstance<RelicDef>();
        spellRelic.effects = new List<RelicEffectDef> { spellDiscount };

        CardDef zeroCostTower = ScriptableObject.CreateInstance<CardDef>();
        zeroCostTower.type = CardType.Tower;
        zeroCostTower.baseManaCost = 0;

        try
        {
            ResolvedCardData towerResolved = CardRuntimeResolver.Build(
                new OwnedCard(towerCard),
                new List<OwnedRelic> { new(towerRelic) });
            ResolvedCardData spellUnaffected = CardRuntimeResolver.Build(
                new OwnedCard(spellCard),
                new List<OwnedRelic> { new(towerRelic) });
            ResolvedCardData spellResolved = CardRuntimeResolver.Build(
                new OwnedCard(spellCard),
                new List<OwnedRelic> { new(spellRelic) });
            ResolvedCardData clamped = CardRuntimeResolver.Build(
                new OwnedCard(zeroCostTower),
                new List<OwnedRelic> { new(towerRelic) });

            Assert.That(towerResolved.ManaCost, Is.EqualTo(Mathf.Max(0, towerCard.baseManaCost - 1)));
            Assert.That(spellUnaffected.ManaCost, Is.EqualTo(spellCard.baseManaCost));
            Assert.That(spellResolved.ManaCost, Is.EqualTo(Mathf.Max(0, spellCard.baseManaCost - 1)));
            Assert.That(clamped.ManaCost, Is.EqualTo(0));
        }
        finally
        {
            Object.DestroyImmediate(towerDiscount);
            Object.DestroyImmediate(spellDiscount);
            Object.DestroyImmediate(towerRelic);
            Object.DestroyImmediate(spellRelic);
            Object.DestroyImmediate(zeroCostTower);
        }
    }

    [Test]
    public void RelicCombatSetupModifier_OverridesStartingManaAndIncreasesMaxHandSize()
    {
        RelicCombatSetupModifierDef manaEffect = ScriptableObject.CreateInstance<RelicCombatSetupModifierDef>();
        manaEffect.overrideStartingMana = true;
        manaEffect.startingManaOverride = 15;

        RelicCombatSetupModifierDef handEffect = ScriptableObject.CreateInstance<RelicCombatSetupModifierDef>();
        handEffect.maxHandSizeDelta = 1;

        RelicDef manaRelic = ScriptableObject.CreateInstance<RelicDef>();
        manaRelic.effects = new List<RelicEffectDef> { manaEffect };

        RelicDef handRelic = ScriptableObject.CreateInstance<RelicDef>();
        handRelic.effects = new List<RelicEffectDef> { handEffect };

        try
        {
            CombatSessionSetup setup = new()
            {
                StartingMana = 10,
                MaxHandSize = 5
            };

            RelicResolver.ModifyCombatSetup(
                new List<OwnedRelic> { new(manaRelic), new(handRelic) },
                setup);

            Assert.That(setup.StartingMana, Is.EqualTo(15));
            Assert.That(setup.MaxHandSize, Is.EqualTo(6));
        }
        finally
        {
            Object.DestroyImmediate(manaEffect);
            Object.DestroyImmediate(handEffect);
            Object.DestroyImmediate(manaRelic);
            Object.DestroyImmediate(handRelic);
        }
    }

    [Test]
    public void RunCoordinator_MetaUnlockPurchase_SpendsCurrencyPersistsAndPreventsDuplicate()
    {
        MetaUnlockEntry unlock = GetFirstUnlockGroup();
        CardDef unlockedCard = GetFirstContainedCard(unlock);
        SaveService saveService = new(contentRepository, saveDirectory);
        saveService.SaveProfile(new ProfileSaveData { metaCurrency = unlock.cost + 2 });

        RunCoordinator coordinator = new(saveService, contentRepository, _ => { });

        Assert.True(coordinator.TryPurchaseMetaUnlock(unlock.UnlockId));
        Assert.True(coordinator.IsCardUnlocked(unlockedCard));
        Assert.That(coordinator.Profile.metaCurrency, Is.EqualTo(2));
        Assert.True(coordinator.Profile.HasUnlock(unlock.UnlockId));

        Assert.False(coordinator.TryPurchaseMetaUnlock(unlock.UnlockId));
        Assert.That(coordinator.Profile.metaCurrency, Is.EqualTo(2));

        ProfileSaveData loadedProfile = saveService.LoadProfile();
        Assert.True(loadedProfile.HasUnlock(unlock.UnlockId));
        Assert.That(loadedProfile.metaCurrency, Is.EqualTo(2));
    }

    [Test]
    public void RunCoordinator_RelicPlaceholder_IsVisibleButNotPurchasable()
    {
        MetaUnlockEntry unlock = GetFirstMetaUnlock(MetaUnlockType.Relic);
        SaveService saveService = new(contentRepository, saveDirectory);
        saveService.SaveProfile(new ProfileSaveData { metaCurrency = 100 });

        RunCoordinator coordinator = new(saveService, contentRepository, _ => { });

        Assert.That(coordinator.GetMetaUnlocks(), Does.Contain(unlock));
        Assert.False(coordinator.TryPurchaseMetaUnlock(unlock.UnlockId));
        Assert.False(coordinator.Profile.HasUnlock(unlock.UnlockId));
    }

    [Test]
    public void RunCoordinator_UnlockGroupPurchase_SpendsCurrencyAndPersistsOnlyGroupUnlock()
    {
        MetaUnlockEntry group = GetFirstUnlockGroup();
        SaveService saveService = new(contentRepository, saveDirectory);
        saveService.SaveProfile(new ProfileSaveData { metaCurrency = group.cost + 4 });

        RunCoordinator coordinator = new(saveService, contentRepository, _ => { });
        List<MetaUnlockContent> contents = coordinator.GetUnlockContents(group);
        Assert.That(contents.Count, Is.GreaterThan(0));
        Assert.False(coordinator.IsCardUnlocked(contents[0].card));
        for (int i = 0; i < contents.Count; i++)
            if (contents[i].card != null)
                Assert.That(contentRepository.GetMetaUnlockForCard(contents[i].card), Is.EqualTo(group));

        Assert.True(coordinator.TryPurchaseMetaUnlock(group.UnlockId));
        Assert.That(coordinator.Profile.metaCurrency, Is.EqualTo(4));
        Assert.True(coordinator.Profile.HasUnlock(group.UnlockId));
        Assert.That(coordinator.Profile.unlockIds.Count, Is.EqualTo(1));
        for (int i = 0; i < contents.Count; i++)
            if (contents[i].card != null)
                Assert.True(coordinator.IsCardUnlocked(contents[i].card));

        ProfileSaveData loadedProfile = saveService.LoadProfile();
        Assert.True(loadedProfile.HasUnlock(group.UnlockId));
        Assert.That(loadedProfile.unlockIds.Count, Is.EqualTo(1));
    }

    [Test]
    public void RunCoordinator_UnlockGroupPurchase_DoesNotDoubleSpendWhenAlreadyPurchased()
    {
        MetaUnlockEntry group = GetFirstUnlockGroup();
        SaveService saveService = new(contentRepository, saveDirectory);
        saveService.SaveProfile(new ProfileSaveData { metaCurrency = group.cost + 10 });

        RunCoordinator coordinator = new(saveService, contentRepository, _ => { });

        Assert.True(coordinator.TryPurchaseMetaUnlock(group.UnlockId));
        int currencyAfterPurchase = coordinator.Profile.metaCurrency;
        Assert.False(coordinator.TryPurchaseMetaUnlock(group.UnlockId));
        Assert.That(coordinator.Profile.metaCurrency, Is.EqualTo(currencyAfterPurchase));
    }

    [Test]
    public void RunCoordinator_PrerequisiteLockedUnlock_CannotBePurchasedUntilRequirementIsMet()
    {
        MetaUnlockEntry upgradeUnlock = GetFirstMetaUnlockWithPrerequisites();
        if (upgradeUnlock == null)
        {
            MetaUnlockEntry standaloneUnlock = GetFirstUnlockGroup();
            SaveService fallbackSaveService = new(contentRepository, saveDirectory);
            fallbackSaveService.SaveProfile(new ProfileSaveData { metaCurrency = standaloneUnlock.cost + 3 });

            RunCoordinator fallbackCoordinator = new(fallbackSaveService, contentRepository, _ => { });
            Assert.True(fallbackCoordinator.AreMetaUnlockPrerequisitesMet(standaloneUnlock));
            Assert.That(fallbackCoordinator.GetMetaUnlockRequirementText(standaloneUnlock), Is.EqualTo(string.Empty));
            Assert.True(fallbackCoordinator.TryPurchaseMetaUnlock(standaloneUnlock.UnlockId));
            Assert.True(fallbackCoordinator.Profile.HasUnlock(standaloneUnlock.UnlockId));
            return;
        }

        MetaUnlockEntry baseUnlock = GetMetaUnlockById(upgradeUnlock.prerequisiteUnlockIds[0]);
        SaveService saveService = new(contentRepository, saveDirectory);
        saveService.SaveProfile(new ProfileSaveData { metaCurrency = baseUnlock.cost + upgradeUnlock.cost + 3 });

        RunCoordinator coordinator = new(saveService, contentRepository, _ => { });

        Assert.False(coordinator.AreMetaUnlockPrerequisitesMet(upgradeUnlock));
        Assert.That(coordinator.GetMetaUnlockRequirementText(upgradeUnlock), Does.Contain(baseUnlock.GetDisplayName()));
        Assert.False(coordinator.TryPurchaseMetaUnlock(upgradeUnlock.UnlockId));
        Assert.False(coordinator.Profile.HasUnlock(upgradeUnlock.UnlockId));

        Assert.True(coordinator.TryPurchaseMetaUnlock(baseUnlock.UnlockId));
        Assert.True(coordinator.AreMetaUnlockPrerequisitesMet(upgradeUnlock));
        Assert.True(coordinator.TryPurchaseMetaUnlock(upgradeUnlock.UnlockId));
        Assert.True(coordinator.Profile.HasUnlock(upgradeUnlock.UnlockId));
    }

    [Test]
    public void RunCoordinator_GroupContainedCardsBecomeEligibleForRewardsAndShops()
    {
        MetaUnlockEntry group = GetFirstUnlockGroup();
        SaveService saveService = new(contentRepository, saveDirectory);
        saveService.SaveProfile(new ProfileSaveData { metaCurrency = group.cost });
        RunCoordinator coordinator = new(saveService, contentRepository, _ => { });
        CardDef groupedCard = GetFirstContainedCard(group);
        Assert.NotNull(groupedCard);

        CardRewardPoolDef rewardPool = ScriptableObject.CreateInstance<CardRewardPoolDef>();
        rewardPool.choiceCount = 1;
        rewardPool.cards = new List<WeightedCardRewardEntry>
        {
            new() { card = groupedCard, weight = 1 }
        };

        ShopInventoryDef inventory = ScriptableObject.CreateInstance<ShopInventoryDef>();
        inventory.choiceCount = 1;
        inventory.offers = new List<ShopOfferData>
        {
            new() { id = "grouped-card", displayName = "Grouped Card", offerType = ShopOfferType.Card, price = 1, card = groupedCard, weight = 1 }
        };

        Assert.That(rewardPool.GetRandomChoices(1, "locked", coordinator.IsCardUnlocked).Count, Is.EqualTo(0));
        Assert.That(inventory.GetRandomOffers(1, "locked", offer => offer.offerType != ShopOfferType.Card || coordinator.IsCardUnlocked(offer.card)).Count, Is.EqualTo(0));

        Assert.True(coordinator.TryPurchaseMetaUnlock(group.UnlockId));
        Assert.That(rewardPool.GetRandomChoices(1, "unlocked", coordinator.IsCardUnlocked).Count, Is.EqualTo(1));
        Assert.That(inventory.GetRandomOffers(1, "unlocked", offer => offer.offerType != ShopOfferType.Card || coordinator.IsCardUnlocked(offer.card)).Count, Is.EqualTo(1));
    }

    [Test]
    public void CardRewardPoolDef_FiltersLockedCatalogCardsBeforeChoosing()
    {
        CardDef lockedCard = GetFirstContainedCard(GetFirstUnlockGroup());
        CardDef unlockedCard = GetFirstCatalogAbsentCard();
        SaveService saveService = new(contentRepository, saveDirectory);
        RunCoordinator coordinator = new(saveService, contentRepository, _ => { });

        CardRewardPoolDef rewardPool = ScriptableObject.CreateInstance<CardRewardPoolDef>();
        rewardPool.choiceCount = 2;
        rewardPool.cards = new List<WeightedCardRewardEntry>
        {
            new() { card = lockedCard, weight = 100 },
            new() { card = unlockedCard, weight = 1 }
        };

        List<PendingRewardEntry> choices = rewardPool.GetRandomChoices(99, "locked-card-filter", coordinator.IsCardUnlocked);

        Assert.That(choices.Count, Is.EqualTo(1));
        Assert.That(choices[0].contentId, Is.EqualTo(contentRepository.GetCardId(unlockedCard)));
        Assert.That(choices.Exists(entry => entry.contentId == contentRepository.GetCardId(lockedCard)), Is.False);
    }

    [Test]
    public void ShopInventoryDef_FiltersLockedCatalogCardsBeforeChoosing()
    {
        CardDef lockedCard = GetFirstContainedCard(GetFirstUnlockGroup());
        CardDef unlockedCard = GetFirstCatalogAbsentCard();
        SaveService saveService = new(contentRepository, saveDirectory);
        RunCoordinator coordinator = new(saveService, contentRepository, _ => { });

        ShopInventoryDef inventory = ScriptableObject.CreateInstance<ShopInventoryDef>();
        inventory.choiceCount = 2;
        inventory.offers = new List<ShopOfferData>
        {
            new() { id = "locked-card", displayName = "Locked Card", offerType = ShopOfferType.Card, price = 1, card = lockedCard, weight = 100 },
            new() { id = "unlocked-card", displayName = "Unlocked Card", offerType = ShopOfferType.Card, price = 1, card = unlockedCard, weight = 1 }
        };

        List<ShopOfferData> offers = inventory.GetRandomOffers(
            55,
            "locked-shop-filter",
            offer => offer.offerType != ShopOfferType.Card || coordinator.IsCardUnlocked(offer.card));

        Assert.That(offers.Count, Is.EqualTo(1));
        Assert.That(offers[0].OfferId, Is.EqualTo("unlocked-card"));
    }

    [Test]
    public void RunCoordinator_TryPurchaseShopOffer_RejectsLockedCardOffer()
    {
        ShopInventoryDef inventory = contentRepository.GetShopInventoryById("act_1_shop");
        Assert.NotNull(inventory);

        ShopOfferData lockedOffer = null;
        for (int i = 0; i < inventory.offers.Count; i++)
        {
            ShopOfferData offer = inventory.offers[i];
            if (offer != null &&
                offer.offerType == ShopOfferType.Card &&
                offer.card != null &&
                contentRepository.GetMetaUnlockForCard(offer.card) != null)
            {
                lockedOffer = offer;
                break;
            }
        }

        Assert.NotNull(lockedOffer);
        CardDef lockedCard = lockedOffer.card;

        SaveService saveService = new(contentRepository, saveDirectory);
        RunSaveData run = new()
        {
            runId = "locked-shop-run",
            currentHealth = 20,
            maxHealth = 20,
            gold = 999,
            deck = new List<OwnedCard> { new(GetFirstCatalogAbsentCard()) },
            currentNodeId = "shop-node",
            completedNodeIds = new List<string>(),
            mapState = new RunMapStateData
            {
                mapTemplateId = "test-template",
                startNodeId = "shop-node",
                nodes = new List<RunMapNodeData>
                {
                    new()
                    {
                        nodeId = "shop-node",
                        displayName = "Shop",
                        nodeType = MapNodeType.Shop,
                        shopInventoryId = inventory.InventoryId
                    }
                }
            },
            seed = 123
        };

        saveService.SaveProfile(new ProfileSaveData { activeRunId = run.runId });
        saveService.SaveRun(run);
        RunCoordinator coordinator = new(saveService, contentRepository, _ => { });

        Assert.False(coordinator.IsCardUnlocked(lockedCard));
        Assert.False(coordinator.TryPurchaseShopOffer("shop-node", lockedOffer.OfferId));
        Assert.That(coordinator.CurrentRun.deck.Count, Is.EqualTo(1));
        Assert.That(coordinator.CurrentRun.gold, Is.EqualTo(999));
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
            if (node.nodeType == MapNodeType.Fight || node.nodeType == MapNodeType.Miniboss || node.nodeType == MapNodeType.Boss)
            {
                Assert.That(string.IsNullOrWhiteSpace(node.encounterId), Is.False);
                Assert.That(template.GetCombatMapPool(node.nodeType)?.GetValidEntries().Count, Is.GreaterThan(0));
            }

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
        SetNodeRule(template, MapNodeType.Fight, 1, 0, -1);
        SetNodeRule(template, MapNodeType.Shop, 1, 2, 2);
        SetNodeRule(template, MapNodeType.Rest, 1, 1, 1);
        SetNodeRule(template, MapNodeType.Miniboss, 1, 1, 1);

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
        SetNodeRule(template, MapNodeType.Fight, 1, 5, -1);
        SetNodeRule(template, MapNodeType.Shop, 0, 0, 0);
        SetNodeRule(template, MapNodeType.Rest, 0, 0, 0);
        SetNodeRule(template, MapNodeType.Miniboss, 0, 0, 0);

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
    public void RunCoordinator_ResolvesCombatPathsFromPoolsWithoutRepeatsUntilExhausted()
    {
        RunMapGenerator generator = new(contentRepository);
        MapTemplateDef template = CreateTemplate();
        template.totalPlayableNodes = 7;
        SetNodeRule(template, MapNodeType.Fight, 1, 5, -1);
        SetNodeRule(template, MapNodeType.Shop, 0, 0, 0);
        SetNodeRule(template, MapNodeType.Rest, 0, 0, 0);
        SetNodeRule(template, MapNodeType.Miniboss, 0, 0, 0);

        RunMapStateData mapState = generator.Generate(template, 6612);
        List<RunMapNodeData> fightNodes = mapState.nodes.FindAll(node => node.nodeType == MapNodeType.Fight);
        List<WeightedEnemyPathEntry> validEntries = template.GetCombatMapPool(MapNodeType.Fight).GetValidEntries();
        int expectedCycleSize = Mathf.Min(validEntries.Count, fightNodes.Count);
        Assert.That(expectedCycleSize, Is.GreaterThanOrEqualTo(1));

        HashSet<EnemyPath> firstCycle = new();
        for (int i = 0; i < expectedCycleSize; i++)
            firstCycle.Add(ResolvePathForTest(template, mapState, fightNodes[i], 6612));

        Assert.That(firstCycle.Count, Is.EqualTo(expectedCycleSize));
        for (int i = 0; i < fightNodes.Count; i++)
        {
            EnemyPath path = ResolvePathForTest(template, mapState, fightNodes[i], 6612);
            Assert.That(validEntries.Exists(entry => entry.pathPrefab == path));
        }
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
    public void RunFlowContent_PathPoolsHaveValidEnemyPaths()
    {
        CombatMapPoolDef[] pathPools = Resources.LoadAll<CombatMapPoolDef>("RunFlow");

        Assert.That(pathPools.Length, Is.GreaterThanOrEqualTo(1));
        for (int i = 0; i < pathPools.Length; i++)
        {
            CombatMapPoolDef pathPool = pathPools[i];
            Assert.NotNull(pathPool);
            List<WeightedEnemyPathEntry> entries = pathPool.GetValidEntries();
            Assert.That(entries.Count, Is.GreaterThan(0), $"Path pool '{pathPool.name}' has no valid EnemyPath entries.");
            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                Assert.NotNull(entries[entryIndex].pathPrefab, $"Path pool '{pathPool.name}' has a missing EnemyPath entry.");
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
        AssertTemplateHasRequiredNodeConfigs(act1);
        AssertTemplateHasRequiredNodeConfigs(act2);
        AssertTemplateHasRequiredNodeConfigs(act3);
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
        List<OwnedCard> deck = CreateOwnedDeckFromTemplateCards();
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
            deck = deck,
            ownedAugments = new List<OwnedAugment>()
        };
        saveService.SaveRun(run);

        string loadedScene = null;
        RunCoordinator coordinator = new(saveService, contentRepository, sceneName => loadedScene = sceneName);
        List<RunMapNodeData> availableNodes = coordinator.GetAvailableNodes();

        Assert.That(availableNodes.Count, Is.EqualTo(2));
        coordinator.SelectNode("node-rest-a");
        Assert.That(coordinator.CurrentRun.currentNodeId, Is.EqualTo("node-rest-a"));

        Assert.True(coordinator.ApplyRestHeal("node-rest-a"));
        Assert.False(coordinator.CurrentRun.HasCompletedNode("node-rest-a"));
        Assert.That(coordinator.CurrentRun.currentNodeId, Is.EqualTo("node-rest-a"));
        Assert.False(coordinator.CanUseRestMainAction("node-rest-a"));

        availableNodes = coordinator.GetAvailableNodes();
        Assert.That(availableNodes.Count, Is.EqualTo(1));
        Assert.That(availableNodes[0].nodeId, Is.EqualTo("node-rest-a"));

        Assert.False(coordinator.ApplyRestHeal("node-rest-a"));
        List<OwnedCard> upgradeableCards = coordinator.GetUpgradeableCards();
        Assert.IsNotEmpty(upgradeableCards);
        Assert.False(coordinator.ApplyRestUpgrade("node-rest-a", upgradeableCards[0].UniqueId));

        Assert.True(TryFindCompatibleAugmentTarget(coordinator.CurrentRun.deck, out CardAugmentDef augment, out OwnedCard targetCard));
        coordinator.CurrentRun.gold = Mathf.Max(coordinator.CurrentRun.gold, augment.applicationCost);
        OwnedAugment ownedAugment = new(augment, "rest-augment-after-heal");
        coordinator.CurrentRun.ownedAugments.Add(ownedAugment);

        Assert.True(coordinator.ApplyRestAugment("node-rest-a", ownedAugment.UniqueId, targetCard.UniqueId));
        Assert.False(coordinator.CurrentRun.HasCompletedNode("node-rest-a"));

        coordinator.LeaveRest("node-rest-a");
        availableNodes = coordinator.GetAvailableNodes();
        Assert.That(availableNodes.Count, Is.EqualTo(1));
        Assert.That(availableNodes[0].nodeId, Is.EqualTo("node-merge"));
        Assert.That(loadedScene, Is.Null);
    }

    [Test]
    public void RunCoordinator_RestAugmentAndUpgradeKeepRestNodeActiveUntilLeave()
    {
        SaveService saveService = new(contentRepository, saveDirectory);
        ProfileSaveData profile = new() { activeRunId = "rest-augment-run" };
        saveService.SaveProfile(profile);

        MapTemplateDef template = contentRepository.GetDefaultMapTemplate();
        List<OwnedCard> deck = CreateOwnedDeckFromTemplateCards();
        RunSaveData run = new()
        {
            runId = "rest-augment-run",
            currentHealth = 20,
            maxHealth = 20,
            gold = 10,
            currentNodeId = "node-start",
            completedNodeIds = new List<string> { "node-start" },
            mapState = CreateBranchingMapState(template.TemplateId),
            seed = 1,
            deck = deck,
            ownedAugments = new List<OwnedAugment>()
        };
        saveService.SaveRun(run);

        RunCoordinator coordinator = new(saveService, contentRepository, _ => { });
        coordinator.SelectNode("node-rest-a");

        Assert.True(TryFindCompatibleAugmentTarget(coordinator.CurrentRun.deck, out CardAugmentDef augment, out OwnedCard targetCard));
        coordinator.CurrentRun.gold = Mathf.Max(coordinator.CurrentRun.gold, augment.applicationCost + 1);
        OwnedAugment ownedAugment = new(augment, "rest-augment-1");
        coordinator.CurrentRun.ownedAugments.Add(ownedAugment);

        Assert.True(coordinator.ApplyRestAugment("node-rest-a", ownedAugment.UniqueId, targetCard.UniqueId));
        Assert.False(coordinator.CurrentRun.HasCompletedNode("node-rest-a"));
        Assert.That(coordinator.CurrentRun.currentNodeId, Is.EqualTo("node-rest-a"));

        List<RunMapNodeData> availableNodes = coordinator.GetAvailableNodes();
        Assert.That(availableNodes.Count, Is.EqualTo(1));
        Assert.That(availableNodes[0].nodeId, Is.EqualTo("node-rest-a"));

        List<OwnedCard> upgradeableCards = coordinator.GetUpgradeableCards();
        Assert.IsNotEmpty(upgradeableCards);
        Assert.True(coordinator.ApplyRestUpgrade("node-rest-a", upgradeableCards[0].UniqueId));
        Assert.False(coordinator.CurrentRun.HasCompletedNode("node-rest-a"));
        Assert.That(coordinator.CurrentRun.currentNodeId, Is.EqualTo("node-rest-a"));
        Assert.False(coordinator.CanUseRestMainAction("node-rest-a"));
        Assert.False(coordinator.ApplyRestHeal("node-rest-a"));

        availableNodes = coordinator.GetAvailableNodes();
        Assert.That(availableNodes.Count, Is.EqualTo(1));
        Assert.That(availableNodes[0].nodeId, Is.EqualTo("node-rest-a"));

        coordinator.LeaveRest("node-rest-a");
        Assert.True(coordinator.CurrentRun.HasCompletedNode("node-rest-a"));

        availableNodes = coordinator.GetAvailableNodes();
        Assert.That(availableNodes.Count, Is.EqualTo(1));
        Assert.That(availableNodes[0].nodeId, Is.EqualTo("node-merge"));
    }

    [Test]
    public void RunCoordinator_RestActionsRequireActiveIncompleteRestNode()
    {
        SaveService saveService = new(contentRepository, saveDirectory);
        ProfileSaveData profile = new() { activeRunId = "rest-validation-run" };
        saveService.SaveProfile(profile);

        MapTemplateDef template = contentRepository.GetDefaultMapTemplate();
        List<OwnedCard> deck = CreateOwnedDeckFromTemplateCards();
        RunSaveData run = new()
        {
            runId = "rest-validation-run",
            currentHealth = 20,
            maxHealth = 20,
            gold = 10,
            currentNodeId = "node-start",
            completedNodeIds = new List<string> { "node-start" },
            mapState = CreateBranchingMapState(template.TemplateId),
            seed = 1,
            deck = deck,
            ownedAugments = new List<OwnedAugment>()
        };
        saveService.SaveRun(run);

        RunCoordinator coordinator = new(saveService, contentRepository, _ => { });
        coordinator.SelectNode("node-rest-a");

        int healthBefore = coordinator.CurrentRun.currentHealth;
        Assert.True(TryFindCompatibleAugmentTarget(coordinator.CurrentRun.deck, out CardAugmentDef augment, out OwnedCard targetCard));

        List<OwnedCard> upgradeableCards = coordinator.GetUpgradeableCards();
        Assert.IsNotEmpty(upgradeableCards);

        OwnedAugment ownedAugment = new(augment, "rest-augment-2");
        coordinator.CurrentRun.ownedAugments.Add(ownedAugment);

        Assert.False(coordinator.ApplyRestHeal("node-rest-b"));
        Assert.False(coordinator.ApplyRestUpgrade("node-rest-b", upgradeableCards[0].UniqueId));
        Assert.False(coordinator.ApplyRestAugment("node-rest-b", ownedAugment.UniqueId, targetCard.UniqueId));
        Assert.That(coordinator.CurrentRun.currentHealth, Is.EqualTo(healthBefore));
        Assert.False(coordinator.CurrentRun.HasCompletedNode("node-rest-b"));
        Assert.That(coordinator.GetOwnedAugments().Exists(entry => entry.UniqueId == ownedAugment.UniqueId), Is.True);
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
        Assert.NotNull(coordinator.CurrentCombatRequest.pathPrefab);

        coordinator.HandleCombatResult(new CombatSceneResult(firstFight.nodeId, true, 18));
        Assert.That(loadedScene, Is.EqualTo(SceneNames.RunMap));
        Assert.True(coordinator.CurrentRun.HasCompletedNode(firstFight.nodeId));
        Assert.NotNull(coordinator.CurrentRun.pendingReward);

        coordinator.SkipPendingReward();
        RunMapNodeData secondFight = AdvanceUntilCombatNode(coordinator);
        Assert.NotNull(secondFight);

        coordinator.SelectNode(secondFight.nodeId);
        coordinator.HandleCombatResult(new CombatSceneResult(secondFight.nodeId, false, 0));

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

        RunSaveData run = CreateBossRun("boss-next-act", act1.TemplateId);
        int deckCountBeforeReward = run.deck.Count;
        saveService.SaveRun(run);

        string loadedScene = null;
        RunCoordinator coordinator = new(saveService, contentRepository, sceneName => loadedScene = sceneName);
        NodeRewardRule rewardRule = act1.GetRewardRule(MapNodeType.Boss);
        Assert.NotNull(rewardRule);

        coordinator.HandleCombatResult(new CombatSceneResult("boss-node", true, 15));

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
        Assert.That(coordinator.CurrentRun.gold, Is.EqualTo(10 + rewardRule.goldReward));
        Assert.That(coordinator.Profile.metaCurrency, Is.EqualTo(rewardRule.metaCurrencyReward));
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
        coordinator.HandleCombatResult(new CombatSceneResult("boss-node", true, 16));

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
        coordinator.HandleCombatResult(new CombatSceneResult("boss-node", true, 12));

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

        EnemyDef enemyA = AssetDatabase.LoadAssetAtPath<EnemyDef>("Assets/Resources/Combat/Enemies/Definitions/Enemy A.asset");
        EnemyDef enemyB = AssetDatabase.LoadAssetAtPath<EnemyDef>("Assets/Resources/Combat/Enemies/Definitions/Enemy B.asset");

        Assert.NotNull(enemyA);
        Assert.NotNull(enemyB);

        List<EnemyDef> enemies = new() { enemyA, enemyB };
        object[] args =
        {
            "editor-test-encounter",
            displayName,
            EncounterKind.Miniboss,
            enemies,
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
    public void EnemySpawner_BatchDelay_IsAppliedBeforeBatchStarts()
    {
        GameObject spawnerObject = new("EnemySpawner Timing Test");
        EnemySpawner spawner = spawnerObject.AddComponent<EnemySpawner>();
        EncounterDef encounter = ScriptableObject.CreateInstance<EncounterDef>();
        encounter.spawnBatches = new List<SpawnBatch>
        {
            new()
            {
                spawnCount = 1,
                spawnInterval = 0f,
                waitTime = 0.5f
            },
            new()
            {
                spawnCount = 1,
                spawnInterval = 0f,
                waitTime = 1f
            }
        };

        spawner.ConfigureEncounter(encounter, null);
        spawner.Begin();

        Assert.That(GetPrivateField<int>(spawner, "currentBatchIndex"), Is.EqualTo(0));
        Assert.That(GetPrivateField<bool>(spawner, "isWaitingBetweenBatches"), Is.True);
        Assert.That(GetPrivateField<float>(spawner, "waitTimer"), Is.EqualTo(0.5f).Within(0.001f));

        MethodInfo fixedUpdate = typeof(EnemySpawner).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(fixedUpdate);

        LogAssert.Expect(LogType.Warning, "EnemySpawner: invalid batch at index 0, skipping.");

        for (int i = 0; i < 40; i++)
        {
            fixedUpdate.Invoke(spawner, null);
            if (GetPrivateField<int>(spawner, "currentBatchIndex") == 1)
                break;
        }

        Assert.That(GetPrivateField<int>(spawner, "currentBatchIndex"), Is.EqualTo(1));
        Assert.That(GetPrivateField<bool>(spawner, "isWaitingBetweenBatches"), Is.True);
        Assert.That(GetPrivateField<float>(spawner, "waitTimer"), Is.EqualTo(1f).Within(0.001f));

        Object.DestroyImmediate(encounter);
        Object.DestroyImmediate(spawnerObject);
    }

    [Test]
    public void RunFlowProjectSetup_LoadDefaultPathPrefab_ReturnsStarterCombatPath()
    {
        MethodInfo loadDefaultPathPrefab = typeof(RunFlowProjectSetup).GetMethod("LoadDefaultPathPrefab", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(loadDefaultPathPrefab);

        GameObject pathPrefab = (GameObject)loadDefaultPathPrefab.Invoke(null, null);
        Assert.NotNull(pathPrefab);
        Assert.That(AssetDatabase.GetAssetPath(pathPrefab), Is.EqualTo("Assets/Resources/RunFlow/Paths/Path 1.prefab"));
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
    public void PauseMenuController_DebugConsoleVisibility_FollowsDebugToggle()
    {
        RunCoordinator coordinator = ConfigureGameFlowRoot();
        GameObject menuObject = new("Pause Menu");
        PauseMenuController pauseMenu = menuObject.AddComponent<PauseMenuController>();

        pauseMenu.Initialize(coordinator, false);

        RectTransform debugConsoleRoot = GetPrivateField<RectTransform>(pauseMenu, "debugConsoleRoot");
        InputField commandInput = GetPrivateField<InputField>(pauseMenu, "debugCommandInputField");
        Assert.NotNull(debugConsoleRoot);
        Assert.NotNull(commandInput);
        Assert.That(debugConsoleRoot.gameObject.activeSelf, Is.False);

        coordinator.SetDebugUiEnabled(true);
        Assert.That(debugConsoleRoot.gameObject.activeSelf, Is.True);

        coordinator.SetDebugUiEnabled(false);
        Assert.That(debugConsoleRoot.gameObject.activeSelf, Is.False);

        Object.DestroyImmediate(menuObject);
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

        createCombatScene.Invoke(null, null);

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

        ShopInventoryDef shopInventory = contentRepository.GetShopInventoryById("act_1_shop");
        Assert.NotNull(shopInventory);
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

        CombatMapPoolDef combatMapPool = ScriptableObject.CreateInstance<CombatMapPoolDef>();
        combatMapPool.id = "combat-map-pool";
        combatMapPool.paths = new List<WeightedEnemyPathEntry>();
        List<EnemyPath> paths = LoadEnemyPaths();
        Assert.That(paths.Count, Is.GreaterThan(0), "Expected at least one EnemyPath prefab in RunFlow/Paths.");
        for (int i = 0; i < paths.Count; i++)
            combatMapPool.paths.Add(new WeightedEnemyPathEntry { pathPrefab = paths[i], weight = 1 });

        CardRewardPoolDef rewardPool = AssetDatabase.LoadAssetAtPath<CardRewardPoolDef>("Assets/Resources/RunFlow/Rewards/Act 1 Rewards.asset");
        template.nodeConfigs = new List<MapNodeConfigDef>
        {
            CreateCombatNodeConfig<FightNodeConfigDef>(
                "test-fight-node",
                "Test Fight",
                new NodeTypeGenerationRule { nodeType = MapNodeType.Fight, weight = 6, minCount = 0, maxCount = -1 },
                fightPool,
                combatMapPool,
                rewardPool,
                10,
                1),
            CreateShopNodeConfig(
                "test-shop-node",
                "Test Shop",
                new NodeTypeGenerationRule { nodeType = MapNodeType.Shop, weight = 2, minCount = 1, maxCount = 2 },
                shopInventory),
            CreateRestNodeConfig(
                "test-rest-node",
                "Test Rest",
                new NodeTypeGenerationRule { nodeType = MapNodeType.Rest, weight = 2, minCount = 1, maxCount = 2 }),
            CreateCombatNodeConfig<MinibossNodeConfigDef>(
                "test-miniboss-node",
                "Test Miniboss",
                new NodeTypeGenerationRule { nodeType = MapNodeType.Miniboss, weight = 1, minCount = 1, maxCount = 1 },
                minibossPool,
                combatMapPool,
                rewardPool,
                20,
                2),
            CreateCombatNodeConfig<BossNodeConfigDef>(
                "test-boss-node",
                "Test Boss",
                new NodeTypeGenerationRule { nodeType = MapNodeType.Boss, weight = 0, minCount = 0, maxCount = 0 },
                bossPool,
                combatMapPool,
                rewardPool,
                32,
                4)
        };

        return template;
    }

    private static T CreateCombatNodeConfig<T>(
        string id,
        string displayName,
        NodeTypeGenerationRule generationRule,
        EncounterPoolDef encounterPool,
        CombatMapPoolDef pathPool,
        CardRewardPoolDef rewardPool,
        int goldReward,
        int metaCurrencyReward) where T : CombatNodeConfigDef
    {
        T config = ScriptableObject.CreateInstance<T>();
        config.id = id;
        config.displayName = displayName;
        config.generationRule = generationRule;
        config.encounterPool = encounterPool;
        config.pathPool = pathPool;
        config.rewardPool = rewardPool;
        config.goldReward = goldReward;
        config.metaCurrencyReward = metaCurrencyReward;
        return config;
    }

    private static ShopNodeConfigDef CreateShopNodeConfig(
        string id,
        string displayName,
        NodeTypeGenerationRule generationRule,
        ShopInventoryDef shopInventory)
    {
        ShopNodeConfigDef config = ScriptableObject.CreateInstance<ShopNodeConfigDef>();
        config.id = id;
        config.displayName = displayName;
        config.generationRule = generationRule;
        config.shopInventory = shopInventory;
        return config;
    }

    private static RestNodeConfigDef CreateRestNodeConfig(string id, string displayName, NodeTypeGenerationRule generationRule)
    {
        RestNodeConfigDef config = ScriptableObject.CreateInstance<RestNodeConfigDef>();
        config.id = id;
        config.displayName = displayName;
        config.generationRule = generationRule;
        return config;
    }

    private static void SetNodeRule(MapTemplateDef template, MapNodeType nodeType, int weight, int minCount, int maxCount)
    {
        MapNodeConfigDef config = template.GetNodeConfig(nodeType);
        Assert.NotNull(config, $"Template is missing a {nodeType} config.");
        config.generationRule = new NodeTypeGenerationRule
        {
            nodeType = nodeType,
            weight = weight,
            minCount = minCount,
            maxCount = maxCount
        };
    }

    private static List<EnemyPath> LoadEnemyPaths()
    {
        List<EnemyPath> paths = new();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/RunFlow/Paths" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            EnemyPath enemyPath = prefab != null ? prefab.GetComponent<EnemyPath>() : null;
            if (enemyPath != null)
                paths.Add(enemyPath);
        }

        return paths;
    }

    private static EnemyPath ResolvePathForTest(MapTemplateDef template, RunMapStateData mapState, RunMapNodeData node, int seed)
    {
        List<WeightedEnemyPathEntry> entries = template.GetCombatMapPool(node.nodeType).GetValidEntries();
        int index = 0;
        for (int i = 0; i < mapState.nodes.Count; i++)
        {
            RunMapNodeData candidate = mapState.nodes[i];
            if (candidate.nodeType != node.nodeType)
                continue;

            if (candidate.nodeId == node.nodeId)
                break;

            index++;
        }

        int nodeTypeSeed = node.nodeType switch
        {
            MapNodeType.Miniboss => 0x4D415049,
            MapNodeType.Boss => 0x4D415042,
            _ => 0x50415448
        };
        MethodInfo method = typeof(RunCoordinator).GetMethod("BuildPathSequence", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        List<EnemyPath> sequence = (List<EnemyPath>)method.Invoke(null, new object[] { entries, index, seed ^ nodeTypeSeed });
        Assert.That(sequence.Count, Is.GreaterThan(index));
        return sequence[index];
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

    private CardDef GetFirstCardOfType(CardType cardType)
    {
        foreach (CardDef card in contentRepository.Cards)
        {
            if (card != null && card.type == cardType)
                return card;
        }

        Assert.Fail($"Expected at least one {cardType} card definition.");
        return null;
    }

    private MetaUnlockEntry GetFirstMetaUnlock(MetaUnlockType type)
    {
        List<MetaUnlockEntry> unlocks = contentRepository.GetMetaUnlocks();
        for (int i = 0; i < unlocks.Count; i++)
        {
            MetaUnlockEntry unlock = unlocks[i];
            if (unlock != null && unlock.type == type)
                return unlock;
        }

        Assert.Fail($"Expected at least one {type} meta unlock.");
        return null;
    }

    private MetaUnlockEntry GetFirstUnlockGroup()
    {
        List<MetaUnlockEntry> unlocks = contentRepository.GetMetaUnlocks();
        for (int i = 0; i < unlocks.Count; i++)
        {
            MetaUnlockEntry unlock = unlocks[i];
            if (unlock != null &&
                unlock.type == MetaUnlockType.UnlockGroup &&
                GetUnlockContents(unlock).Exists(content => content.card != null))
            {
                return unlock;
            }
        }

        Assert.Fail("Expected at least one unlock group containing cards.");
        return null;
    }

    private MetaUnlockEntry GetFirstMetaUnlockWithPrerequisites()
    {
        List<MetaUnlockEntry> unlocks = contentRepository.GetMetaUnlocks();
        for (int i = 0; i < unlocks.Count; i++)
        {
            MetaUnlockEntry unlock = unlocks[i];
            if (unlock != null && unlock.prerequisiteUnlockIds != null && unlock.prerequisiteUnlockIds.Count > 0)
                return unlock;
        }

        return null;
    }

    private MetaUnlockEntry GetMetaUnlockById(string unlockId)
    {
        MetaUnlockEntry unlock = contentRepository.GetMetaUnlockById(unlockId);
        Assert.NotNull(unlock, $"Expected meta unlock '{unlockId}'.");
        return unlock;
    }

    private static List<MetaUnlockContent> GetUnlockContents(MetaUnlockEntry unlock)
    {
        List<MetaUnlockContent> contents = new();
        if (unlock?.contents == null)
            return contents;

        for (int i = 0; i < unlock.contents.Count; i++)
            if (unlock.contents[i] != null)
                contents.Add(unlock.contents[i]);

        return contents;
    }

    private static CardDef GetFirstContainedCard(MetaUnlockEntry unlock)
    {
        if (unlock?.contents != null)
        {
            for (int i = 0; i < unlock.contents.Count; i++)
            {
                MetaUnlockContent content = unlock.contents[i];
                if (content != null && content.type == MetaUnlockType.Card && content.card != null)
                    return content.card;
            }
        }

        Assert.Fail($"Expected unlock '{unlock?.UnlockId}' to contain at least one card.");
        return null;
    }

    private CardDef GetFirstCatalogAbsentCard()
    {
        foreach (CardDef card in contentRepository.Cards)
        {
            if (card != null && contentRepository.GetMetaUnlockForCard(card) == null)
                return card;
        }

        Assert.Fail("Expected at least one card outside the meta unlock catalog.");
        return null;
    }

    private List<OwnedCard> CreateOwnedDeckFromTemplateCards()
    {
        List<OwnedCard> deck = new();
        foreach (CardDef card in contentRepository.Cards)
        {
            if (card != null)
                deck.Add(new OwnedCard(card));
        }

        return deck;
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

    private CardAugmentDef FindCompatibleAugment(CardDef card)
    {
        foreach (CardAugmentDef augment in contentRepository.Augments)
        {
            if (augment != null && augment.IsCompatible(card))
                return augment;
        }

        return null;
    }

    private bool TryFindCompatibleAugmentTarget(List<OwnedCard> deck, out CardAugmentDef augment, out OwnedCard targetCard)
    {
        augment = null;
        targetCard = null;

        if (deck == null)
            return false;

        foreach (CardAugmentDef candidate in contentRepository.Augments)
        {
            if (candidate == null)
                continue;

            for (int i = 0; i < deck.Count; i++)
            {
                OwnedCard card = deck[i];
                if (card != null && card.CanApplyAugment(candidate))
                {
                    augment = candidate;
                    targetCard = card;
                    return true;
                }
            }
        }

        return false;
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

    private static void AssertTemplateHasRequiredNodeConfigs(MapTemplateDef template)
    {
        Assert.NotNull(template.GetNodeConfig(MapNodeType.Fight), $"{template.name} is missing a Fight node config.");
        Assert.NotNull(template.GetNodeConfig(MapNodeType.Shop), $"{template.name} is missing a Shop node config.");
        Assert.NotNull(template.GetNodeConfig(MapNodeType.Rest), $"{template.name} is missing a Rest node config.");
        Assert.NotNull(template.GetNodeConfig(MapNodeType.Miniboss), $"{template.name} is missing a Miniboss node config.");
        Assert.NotNull(template.GetNodeConfig(MapNodeType.Boss), $"{template.name} is missing a Boss node config.");

        AssertCombatNodeConfig(template, MapNodeType.Fight);
        AssertCombatNodeConfig(template, MapNodeType.Miniboss);
        AssertCombatNodeConfig(template, MapNodeType.Boss);
    }

    private static void AssertCombatNodeConfig(MapTemplateDef template, MapNodeType nodeType)
    {
        CombatNodeConfigDef config = template.GetNodeConfig(nodeType) as CombatNodeConfigDef;
        Assert.NotNull(config, $"{template.name} {nodeType} config must be a combat node config.");
        Assert.NotNull(config.encounterPool, $"{template.name} {nodeType} config is missing an enemy-set pool.");
        Assert.That(config.encounterPool.GetValidEntries().Count, Is.GreaterThan(0), $"{template.name} {nodeType} config has no valid enemy sets.");
        Assert.NotNull(config.pathPool, $"{template.name} {nodeType} config is missing an EnemyPath pool.");
        Assert.That(config.pathPool.GetValidEntries().Count, Is.GreaterThan(0), $"{template.name} {nodeType} config has no valid EnemyPath entries.");
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
                coordinator.LeaveRest(restNode.nodeId);
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
