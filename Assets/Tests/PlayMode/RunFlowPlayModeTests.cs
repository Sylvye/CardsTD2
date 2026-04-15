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
        Assert.That(coordinator.GetPendingRewardCards().Count, Is.GreaterThan(0));
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

        int goldBefore = coordinator.CurrentRun.gold;
        Assert.True(coordinator.TryPurchaseShopOffer(shopNode.nodeId, offer.OfferId));
        Assert.Less(coordinator.CurrentRun.gold, goldBefore);

        yield return SceneManager.LoadSceneAsync(SceneNames.RunMap);
        Assert.That(coordinator.GetAvailableShopOffers(shopNode.nodeId).Count, Is.EqualTo(offers.Count - 1));
    }

    [UnityTest]
    public IEnumerator RestUpgrade_PersistsAcrossSceneReload()
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
    }

    [UnityTest]
    public IEnumerator BossVictory_EndsRunAtMainMenu()
    {
        yield return ConfigureRuntime("BossVictory");

        RunCoordinator coordinator = GameFlowRoot.Instance.Coordinator;
        coordinator.StartNewRun();
        yield return WaitForScene(SceneNames.RunMap);

        RunMapNodeData bossNode = null;
        yield return AdvanceUntilNodeTypeAvailable(coordinator, MapNodeType.Boss, node => bossNode = node);
        Assert.NotNull(bossNode);

        coordinator.SelectNode(bossNode.nodeId);
        yield return WaitForScene(SceneNames.Combat);
        Assert.That(coordinator.CurrentCombatRequest.encounter.encounterKind, Is.EqualTo(EncounterKind.Boss));

        coordinator.HandleCombatResult(new CombatSceneResult(bossNode.nodeId, coordinator.CurrentCombatRequest.encounter, true, coordinator.CurrentRun.currentHealth));
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

                coordinator.ApplyRestHeal(nextNode.nodeId);
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
}
