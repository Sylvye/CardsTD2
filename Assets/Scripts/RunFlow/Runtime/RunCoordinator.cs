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

        public MapNodeDef GetNode(string nodeId)
        {
            return CurrentMapTemplate != null ? CurrentMapTemplate.FindNode(nodeId) : null;
        }

        public IEnumerable<MapNodeDef> GetMapNodes()
        {
            return CurrentMapTemplate != null ? CurrentMapTemplate.nodes : Array.Empty<MapNodeDef>();
        }

        public List<MapNodeDef> GetAvailableNodes()
        {
            List<MapNodeDef> availableNodes = new();
            if (CurrentMapTemplate == null || CurrentRun == null)
                return availableNodes;

            if (CurrentMapTemplate.startNode != null && !CurrentRun.HasCompletedNode(CurrentMapTemplate.startNode.NodeId))
            {
                availableNodes.Add(CurrentMapTemplate.startNode);
                return availableNodes;
            }

            HashSet<string> seenIds = new();
            if (CurrentRun.completedNodeIds == null)
                return availableNodes;

            for (int i = 0; i < CurrentRun.completedNodeIds.Count; i++)
            {
                MapNodeDef completedNode = GetNode(CurrentRun.completedNodeIds[i]);
                if (completedNode == null || completedNode.nextNodes == null)
                    continue;

                for (int nextIndex = 0; nextIndex < completedNode.nextNodes.Count; nextIndex++)
                {
                    MapNodeDef nextNode = completedNode.nextNodes[nextIndex];
                    if (nextNode == null || CurrentRun.HasCompletedNode(nextNode.NodeId) || !seenIds.Add(nextNode.NodeId))
                        continue;

                    availableNodes.Add(nextNode);
                }
            }

            return availableNodes;
        }

        public bool IsNodeAvailable(string nodeId)
        {
            List<MapNodeDef> availableNodes = GetAvailableNodes();
            for (int i = 0; i < availableNodes.Count; i++)
            {
                if (availableNodes[i] != null && availableNodes[i].NodeId == nodeId)
                    return true;
            }

            return false;
        }

        public void SelectNode(string nodeId)
        {
            if (CurrentRun == null || CurrentMapTemplate == null || !IsNodeAvailable(nodeId))
                return;

            MapNodeDef node = GetNode(nodeId);
            if (node == null)
                return;

            CurrentRun.currentNodeId = node.NodeId;
            SaveCurrentRun();

            switch (node.nodeType)
            {
                case MapNodeType.Fight:
                case MapNodeType.Miniboss:
                    CurrentCombatRequest = new CombatSceneRequest(node.NodeId, node.encounter, CurrentRun);
                    SaveCurrentRun();
                    loadScene?.Invoke(SceneNames.Combat);
                    break;

                case MapNodeType.Victory:
                    FinishRunVictory(node.NodeId);
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
            MapNodeDef node = GetNode(nodeId);
            if (node?.shopInventory?.offers == null || CurrentRun?.mapState == null)
                return offers;

            ShopPurchaseStateData shopState = CurrentRun.mapState.GetOrCreateShopState(nodeId);
            for (int i = 0; i < node.shopInventory.offers.Count; i++)
            {
                ShopOfferData offer = node.shopInventory.offers[i];
                if (offer != null && !shopState.HasPurchased(offer.OfferId))
                    offers.Add(offer);
            }

            return offers;
        }

        public bool TryPurchaseShopOffer(string nodeId, string offerId, string targetCardUniqueId = null)
        {
            if (CurrentRun == null)
                return false;

            MapNodeDef node = GetNode(nodeId);
            if (node?.shopInventory?.offers == null)
                return false;

            ShopOfferData offer = null;
            for (int i = 0; i < node.shopInventory.offers.Count; i++)
            {
                ShopOfferData candidate = node.shopInventory.offers[i];
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

            CompleteNode(result.nodeId, saveAfterComplete: false);

            if (result.encounter != null)
            {
                CurrentRun.gold += Mathf.Max(0, result.encounter.goldReward);
                Profile.metaCurrency += Mathf.Max(0, result.encounter.metaCurrencyReward);

                if (result.encounter.encounterKind == EncounterKind.Miniboss)
                    Profile.AddUnlock(FirstMinibossUnlockId);

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
            RunSaveData run = new()
            {
                runId = Guid.NewGuid().ToString("N"),
                currentHealth = Mathf.Max(1, template.startingHealth),
                maxHealth = Mathf.Max(1, template.maxHealth),
                gold = Mathf.Max(0, template.startingGold),
                currentNodeId = template.startNode != null ? template.startNode.NodeId : string.Empty,
                completedNodeIds = new List<string>(),
                mapState = new RunMapStateData
                {
                    mapTemplateId = template.TemplateId
                },
                seed = Environment.TickCount,
                deck = new List<OwnedCard>()
            };

            if (template.startNode != null)
                run.MarkNodeCompleted(template.startNode.NodeId);

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
