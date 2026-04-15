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

        public ProfileSaveData Profile { get; private set; }
        public RunSaveData CurrentRun { get; private set; }
        public MapTemplateDef CurrentMapTemplate { get; private set; }
        public CombatSceneRequest CurrentCombatRequest { get; private set; }

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
            Profile = saveService.LoadProfile();

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
            if (CurrentRun?.pendingReward?.offeredCardIds == null)
                return cards;

            for (int i = 0; i < CurrentRun.pendingReward.offeredCardIds.Count; i++)
            {
                CardDef card = contentRepository.GetCardById(CurrentRun.pendingReward.offeredCardIds[i]);
                if (card != null)
                    cards.Add(card);
            }

            return cards;
        }

        public bool ClaimRewardCard(string cardId)
        {
            if (CurrentRun?.pendingReward == null)
                return false;

            CardDef card = contentRepository.GetCardById(cardId);
            if (card == null)
                return false;

            CurrentRun.deck.Add(new OwnedCard(card));
            CurrentRun.pendingReward = null;
            SaveAll();
            return true;
        }

        public void SkipPendingReward()
        {
            if (CurrentRun == null)
                return;

            CurrentRun.pendingReward = null;
            SaveCurrentRun();
        }

        public List<ShopOfferData> GetAvailableShopOffers(string nodeId)
        {
            List<ShopOfferData> offers = new();
            RunMapNodeData node = GetNode(nodeId);
            ShopInventoryDef shopInventory = contentRepository.GetShopInventoryById(node?.shopInventoryId);
            if (shopInventory?.offers == null || CurrentRun?.mapState == null)
                return offers;

            ShopPurchaseStateData shopState = CurrentRun.mapState.GetOrCreateShopState(nodeId);
            for (int i = 0; i < shopInventory.offers.Count; i++)
            {
                ShopOfferData offer = shopInventory.offers[i];
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
            if (shopInventory?.offers == null)
                return false;

            ShopOfferData offer = null;
            for (int i = 0; i < shopInventory.offers.Count; i++)
            {
                ShopOfferData candidate = shopInventory.offers[i];
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

        public bool ApplyRestUpgrade(string nodeId, string uniqueCardId)
        {
            OwnedCard card = FindCardByUniqueId(uniqueCardId);
            if (card == null || !card.TryUpgrade())
                return false;

            CompleteNode(nodeId);
            return true;
        }

        public void HandleCombatResult(CombatSceneResult result)
        {
            if (CurrentRun == null || result == null)
                return;

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

                if (node?.nodeType == MapNodeType.Boss || result.encounter.encounterKind == EncounterKind.Boss)
                {
                    CurrentRun.pendingReward = null;
                    SaveAll();
                    FinishRunVictory(result.nodeId);
                    return;
                }

                if (result.encounter.rewardPool != null)
                {
                    List<CardDef> rewardChoices = result.encounter.rewardPool.GetRandomChoices(CurrentRun.seed, result.nodeId);
                    CurrentRun.pendingReward = new PendingRewardData
                    {
                        sourceNodeId = result.nodeId,
                        offeredCardIds = new List<string>()
                    };

                    for (int i = 0; i < rewardChoices.Count; i++)
                    {
                        string cardId = contentRepository.GetCardId(rewardChoices[i]);
                        if (!string.IsNullOrWhiteSpace(cardId))
                            CurrentRun.pendingReward.offeredCardIds.Add(cardId);
                    }
                }
                else
                {
                    CurrentRun.pendingReward = null;
                }
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
                seed = seed,
                deck = new List<OwnedCard>()
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
                    OwnedCard targetCard = FindCardByUniqueId(targetCardUniqueId);
                    return targetCard != null &&
                           offer.augment != null &&
                           targetCard.TryApplyAugment(offer.augment);

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

            CompleteNode(nodeId, saveAfterComplete: false);
            Profile.AddUnlock(FirstRunClearUnlockId);
            Profile.activeRunId = null;

            saveService.DeleteRun(CurrentRun.runId);
            saveService.SaveProfile(Profile);

            CurrentRun = null;
            CurrentMapTemplate = null;
            CurrentCombatRequest = null;

            loadScene?.Invoke(SceneNames.MainMenu);
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
