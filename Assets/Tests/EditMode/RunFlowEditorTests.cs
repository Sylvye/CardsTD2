using System.Collections.Generic;
using System.IO;
using Cards;
using NUnit.Framework;
using RunFlow;
using UnityEngine;

public class RunFlowEditorTests
{
    private string saveDirectory;
    private RunContentRepository contentRepository;

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
        if (Directory.Exists(saveDirectory))
            Directory.Delete(saveDirectory, true);
    }

    [Test]
    public void SaveService_RoundTripsProfileAndRunData()
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

        MapTemplateDef template = contentRepository.GetDefaultMapTemplate();
        Assert.NotNull(template);
        Assert.IsNotEmpty(template.startingDeck);

        OwnedCard firstCard = new(template.startingDeck[0]);
        if (firstCard.CanApplyAugment(contentRepository.GetAugmentById("damage-augment")))
            firstCard.TryApplyAugment(contentRepository.GetAugmentById("damage-augment"));

        RunSaveData run = new()
        {
            runId = "edit-run",
            currentHealth = 17,
            maxHealth = 20,
            gold = 21,
            deck = new List<OwnedCard> { firstCard },
            currentNodeId = "node-fight-1",
            completedNodeIds = new List<string> { "node-start" },
            mapState = new RunMapStateData
            {
                mapTemplateId = template.TemplateId
            },
            pendingReward = new PendingRewardData
            {
                sourceNodeId = "node-fight-1",
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
        Assert.That(loadedRun.deck.Count, Is.EqualTo(1));
        Assert.That(loadedRun.deck[0].CurrentDefinition, Is.EqualTo(template.startingDeck[0]));
        Assert.That(loadedRun.pendingReward.offeredCardIds.Count, Is.EqualTo(1));
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

        MapNodeDef firstFight = coordinator.GetAvailableNodes().Find(node => node.nodeType == MapNodeType.Fight);
        Assert.NotNull(firstFight);

        coordinator.SelectNode(firstFight.NodeId);
        Assert.That(loadedScene, Is.EqualTo(SceneNames.Combat));
        Assert.NotNull(coordinator.CurrentCombatRequest);

        coordinator.HandleCombatResult(new CombatSceneResult(firstFight.NodeId, coordinator.CurrentCombatRequest.encounter, true, 18));
        Assert.That(loadedScene, Is.EqualTo(SceneNames.RunMap));
        Assert.True(coordinator.CurrentRun.HasCompletedNode(firstFight.NodeId));
        Assert.NotNull(coordinator.CurrentRun.pendingReward);

        coordinator.SkipPendingReward();
        MapNodeDef secondFight = coordinator.GetAvailableNodes().Find(node => node.nodeType == MapNodeType.Fight);
        Assert.NotNull(secondFight);

        coordinator.SelectNode(secondFight.NodeId);
        coordinator.HandleCombatResult(new CombatSceneResult(secondFight.NodeId, coordinator.CurrentCombatRequest.encounter, false, 0));

        Assert.That(loadedScene, Is.EqualTo(SceneNames.MainMenu));
        Assert.IsNull(coordinator.CurrentRun);
        Assert.IsNull(saveService.LoadRun(runId));
    }
}
