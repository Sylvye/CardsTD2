using System.Collections.Generic;
using Cards;
using UnityEngine;
using UnityEngine.UI;

namespace RunFlow
{
    public class RunMapSceneController : MonoBehaviour
    {
        private Text headerText;
        private Text detailText;
        private Transform nodeListRoot;
        private Transform detailRoot;

        private void Start()
        {
            BuildUi();
            RefreshUi();
        }

        private void OnEnable()
        {
            if (headerText != null)
                RefreshUi();
        }

        private void BuildUi()
        {
            Canvas canvas = SimpleUiFactory.EnsureCanvas();
            RectTransform panel = SimpleUiFactory.CreatePanel(canvas.transform, "RunMapPanel");

            GameObject content = new("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            content.transform.SetParent(panel, false);

            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup layout = content.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            RectTransform leftColumn = SimpleUiFactory.CreatePanel(content.transform, "LeftColumn");
            LayoutElement leftLayout = leftColumn.gameObject.AddComponent<LayoutElement>();
            leftLayout.flexibleWidth = 1f;
            SimpleUiFactory.AddVerticalLayout(leftColumn, spacing: 12, padding: 24);

            RectTransform rightColumn = SimpleUiFactory.CreatePanel(content.transform, "RightColumn");
            LayoutElement rightLayout = rightColumn.gameObject.AddComponent<LayoutElement>();
            rightLayout.flexibleWidth = 1.4f;
            SimpleUiFactory.AddVerticalLayout(rightColumn, spacing: 12, padding: 24);

            headerText = SimpleUiFactory.CreateText(leftColumn, string.Empty, 30);
            detailText = SimpleUiFactory.CreateText(leftColumn, string.Empty, 22);
            detailRoot = rightColumn;
            nodeListRoot = new GameObject("NodeList", typeof(RectTransform)).transform;
            nodeListRoot.SetParent(leftColumn, false);
            SimpleUiFactory.AddVerticalLayout(nodeListRoot, spacing: 10, padding: 0);
        }

        private void RefreshUi()
        {
            if (GameFlowRoot.Instance == null)
                return;

            RunCoordinator coordinator = GameFlowRoot.Instance.Coordinator;
            if (coordinator.CurrentRun == null)
            {
                coordinator.ReturnToMenu();
                return;
            }

            headerText.text =
                $"Health: {coordinator.CurrentRun.currentHealth}/{coordinator.CurrentRun.maxHealth}\n" +
                $"Gold: {coordinator.CurrentRun.gold}\n" +
                $"Meta Currency: {coordinator.Profile.metaCurrency}";

            detailText.text = coordinator.CurrentMapTemplate != null
                ? $"Map: {coordinator.CurrentMapTemplate.displayName}"
                : "No map loaded.";

            RebuildNodeButtons(coordinator);

            if (coordinator.CurrentRun.pendingReward != null)
            {
                ShowRewardPanel(coordinator);
                return;
            }

            MapNodeDef currentNode = coordinator.GetNode(coordinator.CurrentRun.currentNodeId);
            if (currentNode != null && !coordinator.CurrentRun.HasCompletedNode(currentNode.NodeId))
            {
                if (currentNode.nodeType == MapNodeType.Shop)
                {
                    ShowShopPanel(coordinator, currentNode);
                    return;
                }

                if (currentNode.nodeType == MapNodeType.Rest)
                {
                    ShowRestPanel(coordinator, currentNode);
                    return;
                }
            }

            ShowInfoPanel("Select an available node.");
        }

        private void RebuildNodeButtons(RunCoordinator coordinator)
        {
            SimpleUiFactory.ClearChildren(nodeListRoot);
            HashSet<string> availableIds = new();
            List<MapNodeDef> availableNodes = coordinator.GetAvailableNodes();
            for (int i = 0; i < availableNodes.Count; i++)
            {
                if (availableNodes[i] != null)
                    availableIds.Add(availableNodes[i].NodeId);
            }

            foreach (MapNodeDef node in coordinator.GetMapNodes())
            {
                if (node == null)
                    continue;

                string status = coordinator.CurrentRun.HasCompletedNode(node.NodeId)
                    ? "Completed"
                    : availableIds.Contains(node.NodeId) ? "Available" : "Locked";

                Button button = SimpleUiFactory.CreateButton(
                    nodeListRoot,
                    $"{node.DisplayNameOrFallback} [{status}]",
                    () => OnNodeSelected(node)
                );

                button.interactable = availableIds.Contains(node.NodeId) && coordinator.CurrentRun.pendingReward == null;
            }
        }

        private void OnNodeSelected(MapNodeDef node)
        {
            if (node == null)
                return;

            RunCoordinator coordinator = GameFlowRoot.Instance.Coordinator;
            coordinator.SelectNode(node.NodeId);

            if (node.nodeType == MapNodeType.Shop)
            {
                ShowShopPanel(coordinator, node);
                return;
            }

            if (node.nodeType == MapNodeType.Rest)
            {
                ShowRestPanel(coordinator, node);
                return;
            }

            RefreshUi();
        }

        private void ShowInfoPanel(string message)
        {
            SimpleUiFactory.ClearChildren(detailRoot);
            SimpleUiFactory.CreateText(detailRoot, "Details", 28);
            SimpleUiFactory.CreateText(detailRoot, message, 22);
        }

        private void ShowRewardPanel(RunCoordinator coordinator)
        {
            SimpleUiFactory.ClearChildren(detailRoot);
            SimpleUiFactory.CreateText(detailRoot, "Reward", 28);
            SimpleUiFactory.CreateText(detailRoot, "Choose one card reward or skip.", 22);

            List<CardDef> rewardCards = coordinator.GetPendingRewardCards();
            for (int i = 0; i < rewardCards.Count; i++)
            {
                CardDef rewardCard = rewardCards[i];
                if (rewardCard == null)
                    continue;

                string rewardCardId = GameFlowRoot.Instance.ContentRepository.GetCardId(rewardCard);
                SimpleUiFactory.CreateButton(detailRoot, $"Take {rewardCard.displayName}", () =>
                {
                    if (coordinator.ClaimRewardCard(rewardCardId))
                        RefreshUi();
                });
            }

            SimpleUiFactory.CreateButton(detailRoot, "Skip", () =>
            {
                coordinator.SkipPendingReward();
                RefreshUi();
            });
        }

        private void ShowRestPanel(RunCoordinator coordinator, MapNodeDef node)
        {
            SimpleUiFactory.ClearChildren(detailRoot);
            SimpleUiFactory.CreateText(detailRoot, node.DisplayNameOrFallback, 28);
            SimpleUiFactory.CreateText(detailRoot, "Choose exactly one rest action.", 22);

            int healAmount = coordinator.GetRestHealAmount();
            SimpleUiFactory.CreateButton(detailRoot, $"Heal {healAmount}", () =>
            {
                coordinator.ApplyRestHeal(node.NodeId);
                RefreshUi();
            });

            List<OwnedCard> upgradeableCards = coordinator.GetUpgradeableCards();
            for (int i = 0; i < upgradeableCards.Count; i++)
            {
                OwnedCard card = upgradeableCards[i];
                if (card?.CurrentDefinition == null)
                    continue;

                string uniqueId = card.UniqueId;
                string displayName = card.CurrentDefinition.displayName;
                SimpleUiFactory.CreateButton(detailRoot, $"Upgrade {displayName}", () =>
                {
                    if (coordinator.ApplyRestUpgrade(node.NodeId, uniqueId))
                        RefreshUi();
                });
            }
        }

        private void ShowShopPanel(RunCoordinator coordinator, MapNodeDef node)
        {
            SimpleUiFactory.ClearChildren(detailRoot);
            SimpleUiFactory.CreateText(detailRoot, node.DisplayNameOrFallback, 28);
            SimpleUiFactory.CreateText(detailRoot, $"Gold: {coordinator.CurrentRun.gold}", 22);

            List<ShopOfferData> offers = coordinator.GetAvailableShopOffers(node.NodeId);
            if (offers.Count == 0)
            {
                SimpleUiFactory.CreateText(detailRoot, "No offers remain. Leave when ready.", 22);
            }

            for (int i = 0; i < offers.Count; i++)
            {
                ShopOfferData offer = offers[i];
                if (offer == null)
                    continue;

                if (offer.offerType == ShopOfferType.Augment)
                {
                    List<OwnedCard> targets = GetAugmentTargets(coordinator.CurrentRun.deck, offer.augment);
                    if (targets.Count == 0)
                    {
                        SimpleUiFactory.CreateText(detailRoot, $"{offer.GetDisplayName()} [{offer.price}] - No valid targets", 20);
                        continue;
                    }

                    string offerId = offer.OfferId;
                    string offerName = offer.GetDisplayName();
                    int offerPrice = offer.price;
                    for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                    {
                        OwnedCard target = targets[targetIndex];
                        string targetId = target.UniqueId;
                        string targetName = target.CurrentDefinition.displayName;
                        SimpleUiFactory.CreateButton(detailRoot, $"{offerName} -> {targetName} [{offerPrice}]", () =>
                        {
                            if (coordinator.TryPurchaseShopOffer(node.NodeId, offerId, targetId))
                                RefreshUi();
                        });
                    }

                    continue;
                }

                string purchaseOfferId = offer.OfferId;
                string buttonLabel = $"{offer.GetDisplayName()} [{offer.price}]";
                SimpleUiFactory.CreateButton(detailRoot, buttonLabel, () =>
                {
                    if (coordinator.TryPurchaseShopOffer(node.NodeId, purchaseOfferId))
                        RefreshUi();
                });
            }

            SimpleUiFactory.CreateButton(detailRoot, "Leave Shop", () =>
            {
                coordinator.LeaveShop(node.NodeId);
                RefreshUi();
            });
        }

        private static List<OwnedCard> GetAugmentTargets(List<OwnedCard> deck, CardAugmentDef augment)
        {
            List<OwnedCard> targets = new();
            if (deck == null || augment == null)
                return targets;

            for (int i = 0; i < deck.Count; i++)
            {
                OwnedCard card = deck[i];
                if (card != null && card.CanApplyAugment(augment))
                    targets.Add(card);
            }

            return targets;
        }
    }
}
