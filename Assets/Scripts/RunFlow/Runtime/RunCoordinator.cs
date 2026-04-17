using System;
using System.Collections.Generic;
using Cards;
using UnityEngine;

namespace RunFlow
{
    public class RunCoordinator
    {
        private const string FirstMinibossUnlockId = "unlock.first_miniboss_clear";
        private const string FirstRunClearUnlockId = "unlock.first_run_clear";

        private readonly SaveService saveService;
        private readonly RunContentRepository contentRepository;
        private readonly RunMapGenerator mapGenerator;
        private readonly Action<string> loadScene;

        public event Action<bool> DebugUiChanged;

        public ProfileSaveData Profile { get; private set; }
        public RunSaveData CurrentRun { get; private set; }
        public MapTemplateDef CurrentMapTemplate { get; private set; }
        public CombatSceneRequest CurrentCombatRequest { get; private set; }
        public bool IsDebugUiEnabled => Profile?.debugUiEnabled ?? false;

        public bool CanContinueRun =>
            Profile != null &&
            !string.IsNullOrWhiteSpace(Profile.activeRunId) &&
            CurrentRun != null;

        public RunCoordinator(SaveService saveService, RunContentRepository contentRepository, Action<string> loadScene)
        {
            this.saveService = saveService;
            this.contentRepository = contentRepository;
            mapGenerator = new RunMapGenerator(contentRepository);
            this.loadScene = loadScene;

            RefreshLoadedState();
        }

        public void RefreshLoadedState()
        {
            contentRepository.Refresh();
            Profile = saveService.LoadProfile() ?? new ProfileSaveData();

            CurrentRun = null;
            CurrentMapTemplate = null;
            CurrentCombatRequest = null;

            if (Profile != null && !string.IsNullOrWhiteSpace(Profile.activeRunId))
            {
                CurrentRun = saveService.LoadRun(Profile.activeRunId);
                if (CurrentRun != null && CurrentRun.mapState != null)
                    CurrentMapTemplate = contentRepository.GetMapTemplateById(CurrentRun.mapState.mapTemplateId);
            }
        }

        public void StartNewRun()
        {
            MapTemplateDef template = contentRepository.GetDefaultMapTemplate();
            if (template == null)
            {
                Debug.LogError("Unable to start a run because no map template was found.");
                return;
            }

            CurrentMapTemplate = template;
            CurrentRun = CreateNewRun(template);
            CurrentCombatRequest = null;

            Profile ??= new ProfileSaveData();
            Profile.activeRunId = CurrentRun.runId;

            SaveAll();
            loadScene?.Invoke(SceneNames.RunMap);
        }

        public void ContinueRun()
        {
            if (!CanContinueRun)
                RefreshLoadedState();

            if (!CanContinueRun)
                return;

            loadScene?.Invoke(SceneNames.RunMap);
        }

        public void ReturnToMenu()
        {
            CurrentCombatRequest = null;
            loadScene?.Invoke(SceneNames.MainMenu);
        }

        public void SetDebugUiEnabled(bool enabled)
        {
            Profile ??= new ProfileSaveData();
            if (Profile.debugUiEnabled == enabled)
                return;

            Profile.debugUiEnabled = enabled;
            saveService.SaveProfile(Profile);
            DebugUiChanged?.Invoke(enabled);
        }

        public RunMapNodeData GetNode(string nodeId)
        {
            return CurrentRun?.mapState != null ? CurrentRun.mapState.FindNode(nodeId) : null;
        }

        public IEnumerable<RunMapNodeData> GetMapNodes()
        {
            return CurrentRun?.mapState?.nodes != null
                ? CurrentRun.mapState.nodes
                : Array.Empty<RunMapNodeData>();
        }

        public List<RunMapNodeData> GetAvailableNodes()
        {
            List<RunMapNodeData> availableNodes = new();
            if (CurrentRun?.mapState == null)
                return availableNodes;

            RunMapNodeData currentNode = GetNode(CurrentRun.currentNodeId);
            if (currentNode == null && !string.IsNullOrWhiteSpace(CurrentRun.mapState.startNodeId))
                currentNode = GetNode(CurrentRun.mapState.startNodeId);

            if (currentNode == null)
                return availableNodes;

            if (!CurrentRun.HasCompletedNode(currentNode.nodeId))
            {
                availableNodes.Add(currentNode);
                return availableNodes;
            }

            if (currentNode.nextNodeIds == null)
                return availableNodes;

            for (int i = 0; i < currentNode.nextNodeIds.Count; i++)
            {
                RunMapNodeData nextNode = GetNode(currentNode.nextNodeIds[i]);
                if (nextNode == null || CurrentRun.HasCompletedNode(nextNode.nodeId))
                    continue;

                availableNodes.Add(nextNode);
            }

            return availableNodes;
        }

        public bool IsNodeAvailable(string nodeId)
        {
            List<RunMapNodeData> availableNodes = GetAvailableNodes();
            for (int i = 0; i < availableNodes.Count; i++)
            {
                if (availableNodes[i] != null && availableNodes[i].nodeId == nodeId)
                    return true;
            }

            return false;
        }

        public void SelectNode(string nodeId)
        {
            if (CurrentRun == null || CurrentMapTemplate == null || !IsNodeAvailable(nodeId))
                return;

            RunMapNodeData node = GetNode(nodeId);
            if (node == null)
                return;

            CurrentRun.currentNodeId = node.nodeId;
            SaveCurrentRun();

            switch (node.nodeType)
            {
                case MapNodeType.Fight:
                case MapNodeType.Miniboss:
                case MapNodeType.Boss:
                    EncounterDef encounter = contentRepository.GetEncounterById(node.encounterId);
                    CurrentCombatRequest = new CombatSceneRequest(node.nodeId, encounter, CurrentRun);
                    SaveCurrentRun();
                    loadScene?.Invoke(SceneNames.Combat);
                    break;
            }
        }

        public List<CardDef> GetPendingRewardCards()
        {
            List<CardDef> cards = new();
            List<PendingRewardEntry> rewards = GetPendingRewards();
            for (int i = 0; i < rewards.Count; i++)
            {
                PendingRewardEntry entry = rewards[i];
                if (entry == null || entry.rewardType != RunRewardType.Card)
                    continue;

                CardDef card = contentRepository.GetCardById(entry.contentId);
                if (card != null)
                    cards.Add(card);
            }

            return cards;
        }

        public List<PendingRewardEntry> GetPendingRewards()
        {
            List<PendingRewardEntry> rewards = new();
            if (CurrentRun?.pendingReward == null)
                return rewards;

            CurrentRun.pendingReward.MigrateLegacyEntries();
            if (CurrentRun.pendingReward.entries == null)
                return rewards;

            for (int i = 0; i < CurrentRun.pendingReward.entries.Count; i++)
            {
                PendingRewardEntry entry = CurrentRun.pendingReward.entries[i];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.contentId))
                    rewards.Add(entry);
            }

            return rewards;
        }

        public bool ClaimRewardCard(string cardId)
        {
            return ClaimPendingReward(RunRewardType.Card, cardId);
        }

        public bool ClaimPendingReward(RunRewardType rewardType, string contentId)
        {
            if (CurrentRun?.pendingReward == null || string.IsNullOrWhiteSpace(contentId))
                return false;

            CurrentRun.pendingReward.MigrateLegacyEntries();
            if (CurrentRun.pendingReward.entries == null)
                return false;

            PendingRewardEntry matchedEntry = null;
            for (int i = 0; i < CurrentRun.pendingReward.entries.Count; i++)
            {
                PendingRewardEntry candidate = CurrentRun.pendingReward.entries[i];
                if (candidate != null &&
                    candidate.rewardType == rewardType &&
                    candidate.contentId == contentId)
                {
                    matchedEntry = candidate;
                    break;
                }
            }

            if (matchedEntry == null)
                return false;

            switch (rewardType)
            {
                case RunRewardType.Card:
                    CardDef card = contentRepository.GetCardById(contentId);
                    if (card == null)
                        return false;

                    CurrentRun.deck.Add(new OwnedCard(card));
                    break;

                case RunRewardType.Augment:
                    CardAugmentDef augment = contentRepository.GetAugmentById(contentId);
                    if (augment == null)
                        return false;

                    CurrentRun.ownedAugments ??= new List<OwnedAugment>();
                    CurrentRun.ownedAugments.Add(new OwnedAugment(augment));
                    break;

                default:
                    return false;
            }

            CurrentRun.pendingReward = null;

            if (TryResolveQueuedBossOutcome())
                return true;

            SaveAll();
            return true;
        }

        public void SkipPendingReward()
        {
            if (CurrentRun == null)
                return;

            CurrentRun.pendingReward = null;

            if (TryResolveQueuedBossOutcome())
                return;

            SaveCurrentRun();
        }

        public List<ShopOfferData> GetAvailableShopOffers(string nodeId)
        {
            List<ShopOfferData> offers = new();
            RunMapNodeData node = GetNode(nodeId);
            ShopInventoryDef shopInventory = contentRepository.GetShopInventoryById(node?.shopInventoryId);
            if (shopInventory == null || CurrentRun?.mapState == null)
                return offers;

            ShopPurchaseStateData shopState = CurrentRun.mapState.GetOrCreateShopState(nodeId);
            List<ShopOfferData> rolledOffers = GetRolledShopOffers(shopInventory, nodeId);
            for (int i = 0; i < rolledOffers.Count; i++)
            {
                ShopOfferData offer = rolledOffers[i];
                if (offer != null && !shopState.HasPurchased(offer.OfferId))
                    offers.Add(offer);
            }

            return offers;
        }

        public bool TryPurchaseShopOffer(string nodeId, string offerId, string targetCardUniqueId = null)
        {
            if (CurrentRun == null)
                return false;

            RunMapNodeData node = GetNode(nodeId);
            ShopInventoryDef shopInventory = contentRepository.GetShopInventoryById(node?.shopInventoryId);
            if (shopInventory == null)
                return false;

            ShopOfferData offer = null;
            List<ShopOfferData> rolledOffers = GetRolledShopOffers(shopInventory, nodeId);
            for (int i = 0; i < rolledOffers.Count; i++)
            {
                ShopOfferData candidate = rolledOffers[i];
                if (candidate != null && candidate.OfferId == offerId)
                {
                    offer = candidate;
                    break;
                }
            }

            if (offer == null || CurrentRun.gold < offer.price)
                return false;

            ShopPurchaseStateData shopState = CurrentRun.mapState.GetOrCreateShopState(nodeId);
            if (shopState.HasPurchased(offer.OfferId))
                return false;

            if (!ApplyShopOffer(offer, targetCardUniqueId))
                return false;

            CurrentRun.gold -= Mathf.Max(0, offer.price);
            shopState.MarkPurchased(offer.OfferId);
            SaveAll();
            return true;
        }

        public void LeaveShop(string nodeId)
        {
            CompleteNode(nodeId);
        }

        public int GetRestHealAmount()
        {
            return CurrentRun == null ? 0 : Mathf.Max(1, Mathf.CeilToInt(CurrentRun.maxHealth * 0.3f));
        }

        public bool ApplyRestHeal(string nodeId)
        {
            if (CurrentRun == null)
                return false;

            CurrentRun.currentHealth = Mathf.Min(CurrentRun.maxHealth, CurrentRun.currentHealth + GetRestHealAmount());
            CompleteNode(nodeId);
            return true;
        }

        public List<OwnedCard> GetUpgradeableCards()
        {
            List<OwnedCard> cards = new();
            if (CurrentRun?.deck == null)
                return cards;

            for (int i = 0; i < CurrentRun.deck.Count; i++)
            {
                OwnedCard card = CurrentRun.deck[i];
                if (card != null && card.CanUpgrade())
                    cards.Add(card);
            }

            return cards;
        }

        public List<OwnedAugment> GetOwnedAugments()
        {
            List<OwnedAugment> ownedAugments = new();
            if (CurrentRun?.ownedAugments == null)
                return ownedAugments;

            for (int i = 0; i < CurrentRun.ownedAugments.Count; i++)
            {
                OwnedAugment ownedAugment = CurrentRun.ownedAugments[i];
                if (ownedAugment?.Definition != null)
                    ownedAugments.Add(ownedAugment);
            }

            return ownedAugments;
        }

        public OwnedAugment GetOwnedAugment(string uniqueAugmentId)
        {
            return FindOwnedAugmentByUniqueId(uniqueAugmentId);
        }

        public List<OwnedCard> GetValidAugmentTargets(string uniqueAugmentId)
        {
            List<OwnedCard> targets = new();
            OwnedAugment ownedAugment = FindOwnedAugmentByUniqueId(uniqueAugmentId);
            if (ownedAugment?.Definition == null || CurrentRun?.deck == null)
                return targets;

            for (int i = 0; i < CurrentRun.deck.Count; i++)
            {
                OwnedCard card = CurrentRun.deck[i];
                if (card != null && card.CanApplyAugment(ownedAugment.Definition))
                    targets.Add(card);
            }

            return targets;
        }

        public bool ApplyRestUpgrade(string nodeId, string uniqueCardId)
        {
            OwnedCard card = FindCardByUniqueId(uniqueCardId);
            if (card == null || !card.TryUpgrade())
                return false;

            CompleteNode(nodeId);
            return true;
        }

        public bool ApplyRestAugment(string nodeId, string uniqueAugmentId, string uniqueCardId)
        {
            if (CurrentRun == null)
                return false;

            OwnedAugment ownedAugment = FindOwnedAugmentByUniqueId(uniqueAugmentId);
            OwnedCard targetCard = FindCardByUniqueId(uniqueCardId);
            if (ownedAugment?.Definition == null || targetCard == null)
                return false;

            int applicationCost = Mathf.Max(0, ownedAugment.Definition.applicationCost);
            if (CurrentRun.gold < applicationCost || !targetCard.TryApplyAugment(ownedAugment.Definition))
                return false;

            CurrentRun.gold -= applicationCost;
            CurrentRun.ownedAugments.Remove(ownedAugment);
            CompleteNode(nodeId);
            return true;
        }

        public void HandleCombatResult(CombatSceneResult result)
        {
            if (CurrentRun == null || result == null)
                return;

            Profile ??= new ProfileSaveData();
            CurrentCombatRequest = null;
            CurrentRun.currentHealth = Mathf.Clamp(result.remainingHealth, 0, CurrentRun.maxHealth);

            if (!result.victory)
            {
                FailRun();
                return;
            }

            RunMapNodeData node = GetNode(result.nodeId);
            CompleteNode(result.nodeId, saveAfterComplete: false);

            if (result.encounter != null)
            {
                CurrentRun.gold += Mathf.Max(0, result.encounter.goldReward);
                Profile.metaCurrency += Mathf.Max(0, result.encounter.metaCurrencyReward);

                if (result.encounter.encounterKind == EncounterKind.Miniboss)
                    Profile.AddUnlock(FirstMinibossUnlockId);
            }

            bool isBossVictory = node?.nodeType == MapNodeType.Boss || result.encounter?.encounterKind == EncounterKind.Boss;
            PopulatePendingRewards(result.encounter, result.nodeId);

            if (isBossVictory)
            {
                QueueBossOutcome(CurrentMapTemplate != null ? CurrentMapTemplate.nextActTemplate : null);

                if (CurrentRun.pendingReward != null)
                {
                    SaveAll();
                    loadScene?.Invoke(SceneNames.RunMap);
                    return;
                }

                if (TryResolveQueuedBossOutcome())
                    return;
            }

            SaveAll();
            loadScene?.Invoke(SceneNames.RunMap);
        }

        private RunSaveData CreateNewRun(MapTemplateDef template)
        {
            int seed = Environment.TickCount;
            RunMapStateData mapState = mapGenerator.Generate(template, seed);
            RunSaveData run = new()
            {
                runId = Guid.NewGuid().ToString("N"),
                currentHealth = Mathf.Max(1, template.startingHealth),
                maxHealth = Mathf.Max(1, template.maxHealth),
                gold = Mathf.Max(0, template.startingGold),
                currentNodeId = mapState.startNodeId,
                completedNodeIds = new List<string>(),
                mapState = mapState,
                queuedNextMapTemplateId = null,
                endRunAfterPendingReward = false,
                seed = seed,
                deck = new List<OwnedCard>(),
                ownedAugments = new List<OwnedAugment>()
            };

            if (!string.IsNullOrWhiteSpace(mapState.startNodeId))
                run.MarkNodeCompleted(mapState.startNodeId);

            if (template.startingDeck != null && template.startingDeck.Count > 0)
            {
                for (int i = 0; i < template.startingDeck.Count; i++)
                {
                    CardDef card = template.startingDeck[i];
                    if (card != null)
                        run.deck.Add(new OwnedCard(card));
                }
            }
            else
            {
                foreach (CardDef card in contentRepository.Cards)
                {
                    if (card == null)
                        continue;

                    run.deck.Add(new OwnedCard(card));
                    if (run.deck.Count >= 5)
                        break;
                }
            }

            return run;
        }

        private bool ApplyShopOffer(ShopOfferData offer, string targetCardUniqueId)
        {
            switch (offer.offerType)
            {
                case ShopOfferType.Card:
                    if (offer.card == null)
                        return false;

                    CurrentRun.deck.Add(new OwnedCard(offer.card));
                    return true;

                case ShopOfferType.Augment:
                    if (offer.augment == null)
                        return false;

                    CurrentRun.ownedAugments ??= new List<OwnedAugment>();
                    CurrentRun.ownedAugments.Add(new OwnedAugment(offer.augment));
                    return true;

                case ShopOfferType.Heal:
                    CurrentRun.currentHealth = Mathf.Min(CurrentRun.maxHealth, CurrentRun.currentHealth + Mathf.Max(0, offer.healAmount));
                    return true;
            }

            return false;
        }

        private OwnedCard FindCardByUniqueId(string uniqueId)
        {
            if (CurrentRun?.deck == null || string.IsNullOrWhiteSpace(uniqueId))
                return null;

            for (int i = 0; i < CurrentRun.deck.Count; i++)
            {
                OwnedCard card = CurrentRun.deck[i];
                if (card != null && card.UniqueId == uniqueId)
                    return card;
            }

            return null;
        }

        private OwnedAugment FindOwnedAugmentByUniqueId(string uniqueId)
        {
            if (CurrentRun?.ownedAugments == null || string.IsNullOrWhiteSpace(uniqueId))
                return null;

            for (int i = 0; i < CurrentRun.ownedAugments.Count; i++)
            {
                OwnedAugment ownedAugment = CurrentRun.ownedAugments[i];
                if (ownedAugment != null && ownedAugment.UniqueId == uniqueId)
                    return ownedAugment;
            }

            return null;
        }

        private List<ShopOfferData> GetRolledShopOffers(ShopInventoryDef shopInventory, string nodeId)
        {
            if (shopInventory == null || CurrentRun == null)
                return new List<ShopOfferData>();

            return shopInventory.GetRandomOffers(CurrentRun.seed, nodeId);
        }

        private void CompleteNode(string nodeId, bool saveAfterComplete = true)
        {
            if (CurrentRun == null || string.IsNullOrWhiteSpace(nodeId))
                return;

            CurrentRun.MarkNodeCompleted(nodeId);
            CurrentRun.currentNodeId = nodeId;

            if (saveAfterComplete)
                SaveCurrentRun();
        }

        private void FinishRunVictory(string nodeId)
        {
            if (CurrentRun == null)
                return;

            ClearQueuedBossOutcome();
            Profile.AddUnlock(FirstRunClearUnlockId);
            Profile.activeRunId = null;

            saveService.DeleteRun(CurrentRun.runId);
            saveService.SaveProfile(Profile);

            CurrentRun = null;
            CurrentMapTemplate = null;
            CurrentCombatRequest = null;

            loadScene?.Invoke(SceneNames.MainMenu);
        }

        private void PopulatePendingRewards(EncounterDef encounter, string nodeId)
        {
            CurrentRun.pendingReward = null;
            if (encounter?.rewardPool == null)
                return;

            List<PendingRewardEntry> rewardChoices = encounter.rewardPool.GetRandomChoices(CurrentRun.seed, nodeId);
            PendingRewardData pendingReward = new()
            {
                sourceNodeId = nodeId,
                entries = new List<PendingRewardEntry>(),
                offeredCardIds = new List<string>()
            };

            for (int i = 0; i < rewardChoices.Count; i++)
            {
                PendingRewardEntry rewardEntry = rewardChoices[i];
                if (rewardEntry != null && !string.IsNullOrWhiteSpace(rewardEntry.contentId))
                {
                    pendingReward.entries.Add(new PendingRewardEntry
                    {
                        rewardType = rewardEntry.rewardType,
                        contentId = rewardEntry.contentId
                    });
                }
            }

            if (pendingReward.entries.Count > 0)
                CurrentRun.pendingReward = pendingReward;
        }

        private void QueueBossOutcome(MapTemplateDef nextActTemplate)
        {
            CurrentRun.queuedNextMapTemplateId = nextActTemplate != null ? nextActTemplate.TemplateId : null;
            CurrentRun.endRunAfterPendingReward = string.IsNullOrWhiteSpace(CurrentRun.queuedNextMapTemplateId);
        }

        private void ClearQueuedBossOutcome()
        {
            if (CurrentRun == null)
                return;

            CurrentRun.queuedNextMapTemplateId = null;
            CurrentRun.endRunAfterPendingReward = false;
        }

        private bool TryResolveQueuedBossOutcome()
        {
            if (CurrentRun == null)
                return false;

            string nextTemplateId = CurrentRun.queuedNextMapTemplateId;
            bool shouldEndRun = CurrentRun.endRunAfterPendingReward;
            if (string.IsNullOrWhiteSpace(nextTemplateId) && !shouldEndRun)
                return false;

            if (shouldEndRun)
            {
                FinishRunVictory(CurrentRun.currentNodeId);
                return true;
            }

            MapTemplateDef nextTemplate = contentRepository.GetMapTemplateById(nextTemplateId);
            if (nextTemplate == null)
            {
                Debug.LogError($"Unable to transition to next act because map template '{nextTemplateId}' was not found. Finishing the run instead.");
                FinishRunVictory(CurrentRun.currentNodeId);
                return true;
            }

            TransitionToNextAct(nextTemplate);
            return true;
        }

        private void TransitionToNextAct(MapTemplateDef nextTemplate)
        {
            if (CurrentRun == null || nextTemplate == null)
                return;

            int nextSeed = Environment.TickCount;
            RunMapStateData mapState = mapGenerator.Generate(nextTemplate, nextSeed);

            CurrentMapTemplate = nextTemplate;
            CurrentCombatRequest = null;
            CurrentRun.pendingReward = null;
            ClearQueuedBossOutcome();
            CurrentRun.seed = nextSeed;
            CurrentRun.mapState = mapState;
            CurrentRun.currentNodeId = mapState.startNodeId;
            CurrentRun.completedNodeIds = new List<string>();

            if (!string.IsNullOrWhiteSpace(mapState.startNodeId))
                CurrentRun.MarkNodeCompleted(mapState.startNodeId);

            SaveAll();
            loadScene?.Invoke(SceneNames.RunMap);
        }

        private void FailRun()
        {
            if (Profile != null)
                Profile.activeRunId = null;

            if (CurrentRun != null)
                saveService.DeleteRun(CurrentRun.runId);

            saveService.SaveProfile(Profile);
            CurrentRun = null;
            CurrentMapTemplate = null;
            CurrentCombatRequest = null;
            loadScene?.Invoke(SceneNames.MainMenu);
        }

        private void SaveAll()
        {
            saveService.SaveProfile(Profile);
            SaveCurrentRun();
        }

        private void SaveCurrentRun()
        {
            if (CurrentRun != null)
                saveService.SaveRun(CurrentRun);
        }
    }
}
