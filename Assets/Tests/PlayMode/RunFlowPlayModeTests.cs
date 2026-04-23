using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cards;
using NUnit.Framework;
using RunFlow;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class RunFlowPlayModeTests
{
    private readonly List<string> saveDirectories = new();

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        for (int i = 0; i < saveDirectories.Count; i++)
        {
            if (Directory.Exists(saveDirectories[i]))
                Directory.Delete(saveDirectories[i], true);
        }

        saveDirectories.Clear();
        yield return SceneManager.LoadSceneAsync(SceneNames.MainMenu);
    }

    [UnityTest]
    public IEnumerator MainMenu_To_RunMap_To_Combat_And_Back_WithReward()
    {
        yield return ConfigureRuntime("SmokeFlow");
        yield return SceneManager.LoadSceneAsync(SceneNames.MainMenu);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNames.MainMenu));

        RunCoordinator coordinator = GameFlowRoot.Instance.Coordinator;
        coordinator.StartNewRun();
        yield return WaitForScene(SceneNames.RunMap);

        RunMapNodeData firstFight = null;
        yield return AdvanceUntilNodeTypeAvailable(coordinator, MapNodeType.Fight, node => firstFight = node, treatAnyCombatAsMatch: true);
        Assert.NotNull(firstFight);

        coordinator.SelectNode(firstFight.nodeId);
        yield return WaitForScene(SceneNames.Combat);

        CombatSceneRequest request = coordinator.CurrentCombatRequest;
        Assert.NotNull(request);
        Assert.That(request.encounter.encounterKind, Is.Not.EqualTo(EncounterKind.Boss));

        coordinator.HandleCombatResult(new CombatSceneResult(firstFight.nodeId, request.encounter, true, coordinator.CurrentRun.currentHealth));
        yield return WaitForScene(SceneNames.RunMap);

        Assert.NotNull(coordinator.CurrentRun.pendingReward);
        Assert.That(coordinator.GetPendingRewards().Count, Is.GreaterThan(0));
    }

    [UnityTest]
    public IEnumerator RunMapScene_RendersScrollableGraph()
    {
        yield return ConfigureRuntime("ScrollableMap");

        RunCoordinator coordinator = GameFlowRoot.Instance.Coordinator;
        coordinator.StartNewRun();
        yield return WaitForScene(SceneNames.RunMap);

        RunMapSceneController controller = Object.FindFirstObjectByType<RunMapSceneController>();
        ScrollRect scrollRect = Object.FindFirstObjectByType<ScrollRect>();

        Assert.NotNull(controller);
        Assert.NotNull(scrollRect);
        Assert.True(scrollRect.horizontal);
        Assert.True(scrollRect.vertical);
        Assert.NotNull(scrollRect.content);
        Assert.That(scrollRect.content.GetComponentsInChildren<Button>(true).Length, Is.GreaterThan(0));
    }

    [UnityTest]
    public IEnumerator ShopPurchase_PersistsAcrossSceneReload()
    {
        yield return ConfigureRuntime("ShopPersistence");

        RunCoordinator coordinator = GameFlowRoot.Instance.Coordinator;
        coordinator.StartNewRun();
        yield return WaitForScene(SceneNames.RunMap);

        RunMapNodeData shopNode = null;
        yield return AdvanceUntilNodeTypeAvailable(coordinator, MapNodeType.Shop, node => shopNode = node);
        Assert.NotNull(shopNode);

        coordinator.SelectNode(shopNode.nodeId);
        yield return null;

        List<ShopOfferData> offers = coordinator.GetAvailableShopOffers(shopNode.nodeId);
        ShopOfferData offer = offers.Find(candidate => candidate.offerType != ShopOfferType.Augment);
        Assert.NotNull(offer);

        coordinator.CurrentRun.gold = Mathf.Max(coordinator.CurrentRun.gold, offer.price);
        int goldBefore = coordinator.CurrentRun.gold;
        Assert.True(coordinator.TryPurchaseShopOffer(shopNode.nodeId, offer.OfferId));
        Assert.Less(coordinator.CurrentRun.gold, goldBefore);

        yield return SceneManager.LoadSceneAsync(SceneNames.RunMap);
        Assert.That(coordinator.GetAvailableShopOffers(shopNode.nodeId).Count, Is.EqualTo(offers.Count - 1));
    }

    [UnityTest]
    public IEnumerator ShopOffers_AreDeterministicAndRespectConfiguredSubset()
    {
        yield return ConfigureRuntime("ShopSubsetDeterminism");

        RunCoordinator coordinator = GameFlowRoot.Instance.Coordinator;
        RunContentRepository repository = GameFlowRoot.Instance.ContentRepository;
        coordinator.StartNewRun();
        yield return WaitForScene(SceneNames.RunMap);

        RunMapNodeData shopNode = null;
        yield return AdvanceUntilNodeTypeAvailable(coordinator, MapNodeType.Shop, node => shopNode = node);
        Assert.NotNull(shopNode);

        ShopInventoryDef inventory = repository.GetShopInventoryById(shopNode.shopInventoryId);
        Assert.NotNull(inventory);

        int originalChoiceCount = inventory.choiceCount;
        List<ShopOfferData> originalOffers = new(inventory.offers);

        try
        {
            ShopOfferData originalCard = FindFirstCardOffer(originalOffers);
            ShopOfferData originalAugment = FindFirstAugmentOffer(originalOffers);
            Assert.NotNull(originalCard);
            Assert.NotNull(originalAugment);

            inventory.choiceCount = 2;
            inventory.offers = new List<ShopOfferData>
            {
                new() { id = "weighted-card", displayName = "Weighted Card", offerType = ShopOfferType.Card, price = 9, card = originalCard.card, weight = 10 },
                new() { id = "weighted-augment", displayName = "Weighted Augment", offerType = ShopOfferType.Augment, price = 11, augment = originalAugment.augment, weight = 3 },
                new() { id = "weighted-heal", displayName = "Weighted Heal", offerType = ShopOfferType.Heal, price = 7, healAmount = 5, weight = 0 }
            };

            List<ShopOfferData> firstRoll = coordinator.GetAvailableShopOffers(shopNode.nodeId);
            List<ShopOfferData> secondRoll = coordinator.GetAvailableShopOffers(shopNode.nodeId);

            Assert.That(firstRoll.Count, Is.EqualTo(2));
            CollectionAssert.AreEqual(firstRoll.ConvertAll(offer => offer.OfferId), secondRoll.ConvertAll(offer => offer.OfferId));
            Assert.That(firstRoll.Exists(offer => offer.OfferId == "weighted-heal"), Is.False);
        }
        finally
        {
            inventory.choiceCount = originalChoiceCount;
            inventory.offers = originalOffers;
        }
    }

    [UnityTest]
    public IEnumerator RestUpgrade_PersistsAcrossSceneReloadWithoutAutoCompletingRestStop()
    {
        yield return ConfigureRuntime("RestUpgradePersistence");

        RunCoordinator coordinator = GameFlowRoot.Instance.Coordinator;
        coordinator.StartNewRun();
        yield return WaitForScene(SceneNames.RunMap);

        RunMapNodeData restNode = null;
        yield return AdvanceUntilNodeTypeAvailable(coordinator, MapNodeType.Rest, node => restNode = node);
        Assert.NotNull(restNode);

        coordinator.SelectNode(restNode.nodeId);
        yield return null;

        List<OwnedCard> upgradeableCards = coordinator.GetUpgradeableCards();
        Assert.IsNotEmpty(upgradeableCards);
        OwnedCard card = upgradeableCards[0];
        string cardId = card.UniqueId;
        CardDef originalDefinition = card.CurrentDefinition;

        Assert.True(coordinator.ApplyRestUpgrade(restNode.nodeId, cardId));

        yield return SceneManager.LoadSceneAsync(SceneNames.RunMap);

        OwnedCard persistedCard = coordinator.CurrentRun.deck.Find(entry => entry.UniqueId == cardId);
        Assert.NotNull(persistedCard);
        Assert.AreNotEqual(originalDefinition, persistedCard.CurrentDefinition);
        Assert.That(coordinator.CurrentRun.currentNodeId, Is.EqualTo(restNode.nodeId));
        Assert.False(coordinator.CurrentRun.HasCompletedNode(restNode.nodeId));
        Assert.False(coordinator.CanUseRestMainAction(restNode.nodeId));
        Assert.That(coordinator.GetAvailableNodes().Count, Is.EqualTo(1));
        Assert.That(coordinator.GetAvailableNodes()[0].nodeId, Is.EqualTo(restNode.nodeId));
    }

    [UnityTest]
    public IEnumerator ShopAugmentPurchase_AddsOwnedAugmentAndPersistsAcrossSceneReload()
    {
        yield return ConfigureRuntime("ShopAugmentPersistence");

        RunCoordinator coordinator = GameFlowRoot.Instance.Coordinator;
        coordinator.StartNewRun();
        yield return WaitForScene(SceneNames.RunMap);

        RunMapNodeData shopNode = null;
        yield return AdvanceUntilNodeTypeAvailable(coordinator, MapNodeType.Shop, node => shopNode = node);
        Assert.NotNull(shopNode);

        coordinator.SelectNode(shopNode.nodeId);
        yield return null;

        List<ShopOfferData> offers = coordinator.GetAvailableShopOffers(shopNode.nodeId);
        ShopOfferData augmentOffer = offers.Find(candidate => candidate.offerType == ShopOfferType.Augment);
        Assert.NotNull(augmentOffer);

        coordinator.CurrentRun.gold = Mathf.Max(coordinator.CurrentRun.gold, augmentOffer.price);
        int goldBefore = coordinator.CurrentRun.gold;
        int ownedAugmentCountBefore = coordinator.GetOwnedAugments().Count;
        Assert.True(coordinator.TryPurchaseShopOffer(shopNode.nodeId, augmentOffer.OfferId));
        Assert.That(coordinator.GetOwnedAugments().Count, Is.EqualTo(ownedAugmentCountBefore + 1));
        Assert.That(coordinator.CurrentRun.gold, Is.EqualTo(goldBefore - augmentOffer.price));

        yield return SceneManager.LoadSceneAsync(SceneNames.RunMap);
        Assert.That(coordinator.GetOwnedAugments().Count, Is.EqualTo(ownedAugmentCountBefore + 1));
    }

    [UnityTest]
    public IEnumerator ShopPurchase_RejectsOffersOutsideRolledSubset()
    {
        yield return ConfigureRuntime("ShopRejectsHiddenOffer");

        RunCoordinator coordinator = GameFlowRoot.Instance.Coordinator;
        RunContentRepository repository = GameFlowRoot.Instance.ContentRepository;
        coordinator.StartNewRun();
        yield return WaitForScene(SceneNames.RunMap);

        RunMapNodeData shopNode = null;
        yield return AdvanceUntilNodeTypeAvailable(coordinator, MapNodeType.Shop, node => shopNode = node);
        Assert.NotNull(shopNode);

        ShopInventoryDef inventory = repository.GetShopInventoryById(shopNode.shopInventoryId);
        Assert.NotNull(inventory);

        int originalChoiceCount = inventory.choiceCount;
        List<ShopOfferData> originalOffers = new(inventory.offers);

        try
        {
            ShopOfferData originalCard = FindFirstCardOffer(originalOffers);
            ShopOfferData originalAugment = FindFirstAugmentOffer(originalOffers);
            Assert.NotNull(originalCard);
            Assert.NotNull(originalAugment);

            inventory.choiceCount = 1;
            inventory.offers = new List<ShopOfferData>
            {
                new()
                {
                    id = "visible-offer",
                    displayName = "Visible Offer",
                    offerType = ShopOfferType.Card,
                    price = 18,
                    card = originalCard.card,
                    weight = 100
                },
                new()
                {
                    id = "hidden-offer",
                    displayName = "Hidden Offer",
                    offerType = ShopOfferType.Augment,
                    price = 12,
                    augment = originalAugment.augment,
                    weight = 1
                }
            };

            coordinator.SelectNode(shopNode.nodeId);
            yield return null;

            List<ShopOfferData> offers = coordinator.GetAvailableShopOffers(shopNode.nodeId);
            Assert.That(offers.Count, Is.EqualTo(1));
            Assert.That(offers[0].OfferId, Is.EqualTo("visible-offer"));

            coordinator.CurrentRun.gold = Mathf.Max(coordinator.CurrentRun.gold, 100);
            Assert.False(coordinator.TryPurchaseShopOffer(shopNode.nodeId, "hidden-offer"));
            Assert.True(coordinator.TryPurchaseShopOffer(shopNode.nodeId, "visible-offer"));
        }
        finally
        {
            inventory.choiceCount = originalChoiceCount;
            inventory.offers = originalOffers;
        }
    }

    [UnityTest]
    public IEnumerator ClaimPendingAugmentReward_AddsOwnedAugment()
    {
        yield return ConfigureRuntime("PendingAugmentReward");

        RunCoordinator coordinator = GameFlowRoot.Instance.Coordinator;
        RunContentRepository repository = GameFlowRoot.Instance.ContentRepository;
        coordinator.StartNewRun();
        yield return WaitForScene(SceneNames.RunMap);

        CardAugmentDef augment = FindCompatibleAugment(repository, coordinator.CurrentRun.deck, out _);
        Assert.NotNull(augment);

        string augmentId = repository.GetAugmentId(augment);
        coordinator.CurrentRun.pendingReward = new PendingRewardData
        {
            sourceNodeId = "test-node",
            entries = new List<PendingRewardEntry>
            {
                new() { rewardType = RunRewardType.Augment, contentId = augmentId }
            }
        };

        Assert.True(coordinator.ClaimPendingReward(RunRewardType.Augment, augmentId));
        Assert.That(coordinator.GetOwnedAugments().Exists(entry => entry.Definition == augment), Is.True);
    }

    [UnityTest]
    public IEnumerator RestAugmentApplication_PersistsAcrossSceneReload()
    {
        yield return ConfigureRuntime("RestAugmentPersistence");

        RunCoordinator coordinator = GameFlowRoot.Instance.Coordinator;
        RunContentRepository repository = GameFlowRoot.Instance.ContentRepository;
        coordinator.StartNewRun();
        yield return WaitForScene(SceneNames.RunMap);

        RunMapNodeData restNode = null;
        yield return AdvanceUntilNodeTypeAvailable(coordinator, MapNodeType.Rest, node => restNode = node);
        Assert.NotNull(restNode);

        coordinator.SelectNode(restNode.nodeId);
        yield return null;

        CardAugmentDef augment = FindCompatibleAugment(repository, coordinator.CurrentRun.deck, out OwnedCard targetCard);
        Assert.NotNull(augment);
        Assert.NotNull(targetCard);

        coordinator.CurrentRun.gold = Mathf.Max(coordinator.CurrentRun.gold, augment.applicationCost + 1);
        OwnedAugment ownedAugment = new OwnedAugment(augment, "rest-augment-1");
        coordinator.CurrentRun.ownedAugments.Add(ownedAugment);

        int goldBefore = coordinator.CurrentRun.gold;
        Assert.True(coordinator.ApplyRestAugment(restNode.nodeId, ownedAugment.UniqueId, targetCard.UniqueId));

        yield return SceneManager.LoadSceneAsync(SceneNames.RunMap);

        OwnedCard persistedCard = coordinator.CurrentRun.deck.Find(entry => entry.UniqueId == targetCard.UniqueId);
        Assert.NotNull(persistedCard);
        Assert.That(HasAppliedAugment(persistedCard, augment), Is.True);
        Assert.That(coordinator.GetOwnedAugments().Exists(entry => entry.UniqueId == ownedAugment.UniqueId), Is.False);
        Assert.That(coordinator.CurrentRun.gold, Is.EqualTo(goldBefore - augment.applicationCost));
        Assert.That(coordinator.CurrentRun.currentNodeId, Is.EqualTo(restNode.nodeId));
        Assert.False(coordinator.CurrentRun.HasCompletedNode(restNode.nodeId));

        List<RunMapNodeData> availableNodes = coordinator.GetAvailableNodes();
        Assert.That(availableNodes.Count, Is.EqualTo(1));
        Assert.That(availableNodes[0].nodeId, Is.EqualTo(restNode.nodeId));
    }

    [UnityTest]
    public IEnumerator RestHeal_AfterApplyingAugment_KeepsRestStopActiveUntilLeave()
    {
        yield return ConfigureRuntime("RestHealAfterAugment");

        RunCoordinator coordinator = GameFlowRoot.Instance.Coordinator;
        RunContentRepository repository = GameFlowRoot.Instance.ContentRepository;
        coordinator.StartNewRun();
        yield return WaitForScene(SceneNames.RunMap);

        RunMapNodeData restNode = null;
        yield return AdvanceUntilNodeTypeAvailable(coordinator, MapNodeType.Rest, node => restNode = node);
        Assert.NotNull(restNode);

        coordinator.SelectNode(restNode.nodeId);
        yield return null;

        CardAugmentDef augment = FindCompatibleAugment(repository, coordinator.CurrentRun.deck, out OwnedCard targetCard);
        Assert.NotNull(augment);
        Assert.NotNull(targetCard);

        coordinator.CurrentRun.gold = Mathf.Max(coordinator.CurrentRun.gold, augment.applicationCost + 1);
        coordinator.CurrentRun.ownedAugments.Add(new OwnedAugment(augment, "rest-augment-heal-1"));

        Assert.True(coordinator.ApplyRestAugment(restNode.nodeId, "rest-augment-heal-1", targetCard.UniqueId));
        Assert.True(coordinator.ApplyRestHeal(restNode.nodeId));

        yield return SceneManager.LoadSceneAsync(SceneNames.RunMap);

        Assert.False(coordinator.CurrentRun.HasCompletedNode(restNode.nodeId));
        Assert.That(coordinator.CurrentRun.currentNodeId, Is.EqualTo(restNode.nodeId));
        Assert.False(coordinator.CanUseRestMainAction(restNode.nodeId));
        Assert.That(coordinator.GetAvailableNodes().Count, Is.EqualTo(1));
        Assert.That(coordinator.GetAvailableNodes()[0].nodeId, Is.EqualTo(restNode.nodeId));

        coordinator.LeaveRest(restNode.nodeId);
        Assert.True(coordinator.CurrentRun.HasCompletedNode(restNode.nodeId));
        Assert.That(coordinator.GetAvailableNodes().Exists(node => node.nodeId == restNode.nodeId), Is.False);
    }

    [UnityTest]
    public IEnumerator BossVictory_WithLinkedNextAct_TransitionsAfterRewardResolution()
    {
        yield return ConfigureRuntime("BossVictoryNextAct");

        GameFlowRoot root = GameFlowRoot.Instance;
        RunContentRepository repository = root.ContentRepository;
        SaveService saveService = root.SaveService;

        MapTemplateDef act1 = repository.GetMapTemplateById("act_1");
        MapTemplateDef act2 = repository.GetMapTemplateById("act_2");
        EncounterDef bossEncounter = repository.GetEncountersByKind(EncounterKind.Boss)[0];
        OwnedCard card = new(GetFirstRuntimeCard(repository), "linked-boss-card", null);
        CardAugmentDef augment = FindCompatibleAugment(repository, new List<OwnedCard> { card }, out _);
        RunSaveData run = CreateSingleBossRun("linked-boss-run", act1.TemplateId, bossEncounter.EncounterId, card, augment != null ? new OwnedAugment(augment, "linked-boss-augment") : null);
        saveService.SaveProfile(new ProfileSaveData { activeRunId = run.runId });
        saveService.SaveRun(run);

        RunCoordinator coordinator = new(saveService, repository, root.LoadScene);
        root.OverrideServices(repository, saveService, coordinator);
        yield return SceneManager.LoadSceneAsync(SceneNames.RunMap);
        yield return WaitForScene(SceneNames.RunMap);

        coordinator.CurrentRun.currentHealth = 14;
        coordinator.CurrentRun.gold = 37;
        int deckCountBefore = coordinator.CurrentRun.deck.Count;
        int augmentCountBefore = coordinator.GetOwnedAugments().Count;
        int goldBeforeCombat = coordinator.CurrentRun.gold;

        coordinator.SelectNode("boss-node");
        yield return WaitForScene(SceneNames.Combat);
        Assert.That(coordinator.CurrentCombatRequest.encounter.encounterKind, Is.EqualTo(EncounterKind.Boss));

        coordinator.HandleCombatResult(new CombatSceneResult("boss-node", coordinator.CurrentCombatRequest.encounter, true, 13));
        yield return WaitForScene(SceneNames.RunMap);

        Assert.NotNull(coordinator.CurrentRun);
        Assert.That(coordinator.CurrentMapTemplate.TemplateId, Is.EqualTo("act_1"));
        Assert.NotNull(coordinator.CurrentRun.pendingReward);
        Assert.That(coordinator.CurrentRun.queuedNextMapTemplateId, Is.EqualTo("act_2"));

        coordinator.SkipPendingReward();
        yield return null;

        Assert.NotNull(coordinator.CurrentRun);
        Assert.That(coordinator.CurrentMapTemplate, Is.EqualTo(act2));
        Assert.That(coordinator.CurrentRun.mapState.mapTemplateId, Is.EqualTo("act_2"));
        Assert.That(coordinator.CurrentRun.currentHealth, Is.EqualTo(13));
        Assert.That(coordinator.CurrentRun.gold, Is.EqualTo(goldBeforeCombat + bossEncounter.goldReward));
        Assert.That(coordinator.CurrentRun.deck.Count, Is.EqualTo(deckCountBefore));
        Assert.That(coordinator.GetOwnedAugments().Count, Is.EqualTo(augmentCountBefore));
        Assert.That(coordinator.CurrentRun.runId, Is.EqualTo("linked-boss-run"));
        Assert.That(coordinator.CurrentRun.pendingReward, Is.Null);
    }

    [UnityTest]
    public IEnumerator FinalBossVictory_EndsAfterRewardResolution()
    {
        yield return ConfigureRuntime("BossVictoryFinalAct");

        GameFlowRoot root = GameFlowRoot.Instance;
        RunContentRepository repository = root.ContentRepository;
        SaveService saveService = root.SaveService;

        MapTemplateDef act3 = repository.GetMapTemplateById("act_3");
        EncounterDef bossEncounter = repository.GetEncountersByKind(EncounterKind.Boss)[0];
        RunSaveData run = CreateSingleBossRun("final-boss-run", act3.TemplateId, bossEncounter.EncounterId, new OwnedCard(GetFirstRuntimeCard(repository), "final-boss-card", null));
        saveService.SaveProfile(new ProfileSaveData { activeRunId = run.runId });
        saveService.SaveRun(run);

        RunCoordinator coordinator = new(saveService, repository, root.LoadScene);
        root.OverrideServices(repository, saveService, coordinator);
        yield return SceneManager.LoadSceneAsync(SceneNames.RunMap);
        yield return WaitForScene(SceneNames.RunMap);

        coordinator.SelectNode("boss-node");
        yield return WaitForScene(SceneNames.Combat);

        coordinator.HandleCombatResult(new CombatSceneResult("boss-node", coordinator.CurrentCombatRequest.encounter, true, 18));
        yield return WaitForScene(SceneNames.RunMap);

        Assert.NotNull(coordinator.CurrentRun);
        Assert.NotNull(coordinator.CurrentRun.pendingReward);
        Assert.That(coordinator.CurrentRun.endRunAfterPendingReward, Is.True);

        coordinator.SkipPendingReward();
        yield return WaitForScene(SceneNames.MainMenu);

        Assert.IsNull(coordinator.CurrentRun);
    }

    private IEnumerator ConfigureRuntime(string testName)
    {
        string saveDirectory = Path.Combine(Application.dataPath, "../Temp/RunFlowPlayModeTests", testName);
        if (Directory.Exists(saveDirectory))
            Directory.Delete(saveDirectory, true);

        Directory.CreateDirectory(saveDirectory);
        saveDirectories.Add(saveDirectory);

        RunContentRepository repository = new();
        repository.Refresh();

        GameFlowRoot root = GameFlowRoot.EnsureInstance();
        SaveService saveService = new(repository, saveDirectory);
        RunCoordinator coordinator = new(saveService, repository, root.LoadScene);
        root.OverrideServices(repository, saveService, coordinator);
        yield return null;
    }

    private static IEnumerator WaitForScene(string sceneName)
    {
        for (int i = 0; i < 120; i++)
        {
            if (SceneManager.GetActiveScene().name == sceneName)
                yield break;

            yield return null;
        }

        Assert.Fail($"Timed out waiting for scene '{sceneName}'.");
    }

    private static IEnumerator AdvanceUntilNodeTypeAvailable(RunCoordinator coordinator, MapNodeType targetType, System.Action<RunMapNodeData> captureNode, bool treatAnyCombatAsMatch = false)
    {
        for (int i = 0; i < 16; i++)
        {
            List<RunMapNodeData> availableNodes = coordinator.GetAvailableNodes();
            RunMapNodeData targetNode = treatAnyCombatAsMatch
                ? availableNodes.Find(node => IsCombatNode(node.nodeType))
                : availableNodes.Find(node => node.nodeType == targetType);
            if (targetNode != null)
            {
                captureNode(targetNode);
                yield break;
            }

            RunMapNodeData nextNode = ChooseNodeLeadingToType(coordinator, availableNodes, targetType);
            Assert.NotNull(nextNode);

            if (IsCombatNode(nextNode.nodeType))
            {
                coordinator.SelectNode(nextNode.nodeId);
                yield return WaitForScene(SceneNames.Combat);
                coordinator.HandleCombatResult(new CombatSceneResult(nextNode.nodeId, coordinator.CurrentCombatRequest.encounter, true, coordinator.CurrentRun.currentHealth));

                if (nextNode.nodeType == MapNodeType.Boss)
                    yield break;

                yield return WaitForScene(SceneNames.RunMap);
                if (coordinator.CurrentRun != null && coordinator.CurrentRun.pendingReward != null)
                    coordinator.SkipPendingReward();
            }
            else if (nextNode.nodeType == MapNodeType.Shop)
            {
                coordinator.SelectNode(nextNode.nodeId);
                yield return null;
                if (targetType == MapNodeType.Shop)
                {
                    captureNode(nextNode);
                    yield break;
                }

                coordinator.LeaveShop(nextNode.nodeId);
            }
            else if (nextNode.nodeType == MapNodeType.Rest)
            {
                coordinator.SelectNode(nextNode.nodeId);
                yield return null;
                if (targetType == MapNodeType.Rest)
                {
                    captureNode(nextNode);
                    yield break;
                }

                coordinator.LeaveRest(nextNode.nodeId);
            }

            yield return null;
        }

        Assert.Fail($"Unable to route to node type '{targetType}'.");
    }

    private static RunMapNodeData ChooseNodeLeadingToType(RunCoordinator coordinator, List<RunMapNodeData> candidates, MapNodeType targetType)
    {
        for (int i = 0; i < candidates.Count; i++)
        {
            RunMapNodeData candidate = candidates[i];
            if (candidate != null && CanReachType(coordinator, candidate, targetType, new HashSet<string>()))
                return candidate;
        }

        return candidates.Count > 0 ? candidates[0] : null;
    }

    private static bool CanReachType(RunCoordinator coordinator, RunMapNodeData node, MapNodeType targetType, HashSet<string> visited)
    {
        if (node == null || !visited.Add(node.nodeId))
            return false;

        if (node.nodeType == targetType)
            return true;

        for (int i = 0; i < node.nextNodeIds.Count; i++)
        {
            if (CanReachType(coordinator, coordinator.GetNode(node.nextNodeIds[i]), targetType, visited))
                return true;
        }

        return false;
    }

    private static bool IsCombatNode(MapNodeType nodeType)
    {
        return nodeType == MapNodeType.Fight || nodeType == MapNodeType.Miniboss || nodeType == MapNodeType.Boss;
    }

    private static ShopOfferData FindFirstCardOffer(List<ShopOfferData> offers)
    {
        return offers.Find(offer => offer != null && offer.offerType == ShopOfferType.Card && offer.card != null);
    }

    private static ShopOfferData FindFirstAugmentOffer(List<ShopOfferData> offers)
    {
        return offers.Find(offer => offer != null && offer.offerType == ShopOfferType.Augment && offer.augment != null);
    }

    private static CardAugmentDef FindCompatibleAugment(RunContentRepository repository, List<OwnedCard> deck, out OwnedCard compatibleCard)
    {
        compatibleCard = null;
        foreach (CardAugmentDef augment in repository.Augments)
        {
            if (augment == null)
                continue;

            for (int i = 0; i < deck.Count; i++)
            {
                OwnedCard card = deck[i];
                if (card != null && card.CanApplyAugment(augment))
                {
                    compatibleCard = card;
                    return augment;
                }
            }
        }

        return null;
    }

    private static bool HasAppliedAugment(OwnedCard card, CardAugmentDef augment)
    {
        if (card?.AppliedAugments == null || augment == null)
            return false;

        for (int i = 0; i < card.AppliedAugments.Count; i++)
        {
            if (card.AppliedAugments[i] == augment)
                return true;
        }

        return false;
    }

    private static CardDef GetFirstRuntimeCard(RunContentRepository repository)
    {
        foreach (CardDef card in repository.Cards)
        {
            if (card != null)
                return card;
        }

        Assert.Fail("Expected at least one runtime card.");
        return null;
    }

    private static RunSaveData CreateSingleBossRun(string runId, string templateId, string encounterId, OwnedCard card, OwnedAugment ownedAugment = null)
    {
        return new RunSaveData
        {
            runId = runId,
            currentHealth = 20,
            maxHealth = 20,
            gold = 10,
            deck = new List<OwnedCard> { card },
            ownedAugments = ownedAugment != null ? new List<OwnedAugment> { ownedAugment } : new List<OwnedAugment>(),
            currentNodeId = "boss-node",
            completedNodeIds = new List<string>(),
            mapState = new RunMapStateData
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
                        encounterId = encounterId,
                        column = 0,
                        lane = 0,
                        nextNodeIds = new List<string>()
                    }
                }
            },
            pendingReward = null,
            queuedNextMapTemplateId = null,
            endRunAfterPendingReward = false,
            seed = 456
        };
    }
}
