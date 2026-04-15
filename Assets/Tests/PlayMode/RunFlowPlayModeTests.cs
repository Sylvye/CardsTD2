using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cards;
using NUnit.Framework;
using RunFlow;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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

        MapNodeDef firstFight = coordinator.GetAvailableNodes().Find(node => node.nodeType == MapNodeType.Fight);
        Assert.NotNull(firstFight);

        coordinator.SelectNode(firstFight.NodeId);
        yield return WaitForScene(SceneNames.Combat);

        CombatSceneRequest request = coordinator.CurrentCombatRequest;
        Assert.NotNull(request);
        Assert.That(request.encounter.encounterKind, Is.EqualTo(EncounterKind.RegularFight));

        coordinator.HandleCombatResult(new CombatSceneResult(firstFight.NodeId, request.encounter, true, coordinator.CurrentRun.currentHealth));
        yield return WaitForScene(SceneNames.RunMap);

        Assert.NotNull(coordinator.CurrentRun.pendingReward);
        Assert.That(coordinator.GetPendingRewardCards().Count, Is.GreaterThan(0));
    }

    [UnityTest]
    public IEnumerator RegularFight_And_Miniboss_Reuse_CombatScene()
    {
        yield return ConfigureRuntime("CombatReuse");

        RunCoordinator coordinator = GameFlowRoot.Instance.Coordinator;
        coordinator.StartNewRun();
        yield return WaitForScene(SceneNames.RunMap);

        yield return CompleteFightAndSkipReward(coordinator);
        yield return CompleteFightAndSkipReward(coordinator);

        MapNodeDef shopNode = coordinator.GetAvailableNodes().Find(node => node.nodeType == MapNodeType.Shop);
        Assert.NotNull(shopNode);
        coordinator.SelectNode(shopNode.NodeId);
        yield return null;
        coordinator.LeaveShop(shopNode.NodeId);

        yield return CompleteFightAndSkipReward(coordinator);

        MapNodeDef minibossNode = coordinator.GetAvailableNodes().Find(node => node.nodeType == MapNodeType.Miniboss);
        Assert.NotNull(minibossNode);

        coordinator.SelectNode(minibossNode.NodeId);
        yield return WaitForScene(SceneNames.Combat);

        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNames.Combat));
        Assert.That(coordinator.CurrentCombatRequest.encounter.encounterKind, Is.EqualTo(EncounterKind.Miniboss));
    }

    [UnityTest]
    public IEnumerator ShopPurchase_PersistsAcrossSceneReload()
    {
        yield return ConfigureRuntime("ShopPersistence");

        RunCoordinator coordinator = GameFlowRoot.Instance.Coordinator;
        coordinator.StartNewRun();
        yield return WaitForScene(SceneNames.RunMap);

        yield return CompleteFightAndSkipReward(coordinator);
        yield return CompleteFightAndSkipReward(coordinator);

        MapNodeDef shopNode = coordinator.GetAvailableNodes().Find(node => node.nodeType == MapNodeType.Shop);
        Assert.NotNull(shopNode);
        coordinator.SelectNode(shopNode.NodeId);
        yield return null;

        List<ShopOfferData> offers = coordinator.GetAvailableShopOffers(shopNode.NodeId);
        ShopOfferData offer = offers.Find(candidate => candidate.offerType != ShopOfferType.Augment);
        Assert.NotNull(offer);

        int goldBefore = coordinator.CurrentRun.gold;
        Assert.True(coordinator.TryPurchaseShopOffer(shopNode.NodeId, offer.OfferId));
        Assert.Less(coordinator.CurrentRun.gold, goldBefore);

        yield return SceneManager.LoadSceneAsync(SceneNames.RunMap);
        Assert.That(coordinator.GetAvailableShopOffers(shopNode.NodeId).Count, Is.EqualTo(offers.Count - 1));
    }

    [UnityTest]
    public IEnumerator RestUpgrade_PersistsAcrossSceneReload()
    {
        yield return ConfigureRuntime("RestUpgradePersistence");

        RunCoordinator coordinator = GameFlowRoot.Instance.Coordinator;
        coordinator.StartNewRun();
        yield return WaitForScene(SceneNames.RunMap);

        yield return CompleteFightAndSkipReward(coordinator);
        yield return CompleteFightAndSkipReward(coordinator);

        MapNodeDef restNode = coordinator.GetAvailableNodes().Find(node => node.nodeType == MapNodeType.Rest);
        Assert.NotNull(restNode);
        coordinator.SelectNode(restNode.NodeId);
        yield return null;

        List<OwnedCard> upgradeableCards = coordinator.GetUpgradeableCards();
        Assert.IsNotEmpty(upgradeableCards);
        OwnedCard card = upgradeableCards[0];
        string cardId = card.UniqueId;
        CardDef originalDefinition = card.CurrentDefinition;

        Assert.True(coordinator.ApplyRestUpgrade(restNode.NodeId, cardId));

        yield return SceneManager.LoadSceneAsync(SceneNames.RunMap);

        OwnedCard persistedCard = coordinator.CurrentRun.deck.Find(entry => entry.UniqueId == cardId);
        Assert.NotNull(persistedCard);
        Assert.AreNotEqual(originalDefinition, persistedCard.CurrentDefinition);
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

    private static IEnumerator CompleteFightAndSkipReward(RunCoordinator coordinator)
    {
        MapNodeDef fightNode = coordinator.GetAvailableNodes().Find(node => node.nodeType == MapNodeType.Fight);
        Assert.NotNull(fightNode);

        coordinator.SelectNode(fightNode.NodeId);
        yield return WaitForScene(SceneNames.Combat);
        coordinator.HandleCombatResult(new CombatSceneResult(fightNode.NodeId, coordinator.CurrentCombatRequest.encounter, true, coordinator.CurrentRun.currentHealth));
        yield return WaitForScene(SceneNames.RunMap);
        coordinator.SkipPendingReward();
    }
}
