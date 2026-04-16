using System.Collections.Generic;
using Cards;
using UnityEngine;
using UnityEngine.UI;

namespace RunFlow
{
    public class RunMapSceneController : MonoBehaviour
    {
        private const float MapPadding = 96f;
        private const float ColumnSpacing = 240f;
        private const float LaneSpacing = 164f;
        private const float NodeWidth = 154f;
        private const float NodeHeight = 84f;
        private const float BossWidth = 182f;
        private const float BossHeight = 96f;
        private const float ConnectionThickness = 8f;

        private Text headerText;
        private Text detailText;
        private RectTransform detailRoot;
        private ScrollRect mapScrollRect;
        private RectTransform mapContent;
        private RectTransform connectionLayer;
        private RectTransform nodeLayer;
        private string selectedRestAugmentUniqueId;

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
            leftLayout.flexibleWidth = 1.35f;
            SimpleUiFactory.AddVerticalLayout(leftColumn, spacing: 12, padding: 24);

            RectTransform rightColumn = SimpleUiFactory.CreatePanel(content.transform, "RightColumn");
            LayoutElement rightLayout = rightColumn.gameObject.AddComponent<LayoutElement>();
            rightLayout.flexibleWidth = 1.15f;

            headerText = SimpleUiFactory.CreateText(leftColumn, string.Empty, 30);
            detailText = SimpleUiFactory.CreateText(leftColumn, string.Empty, 22);
            detailRoot = SimpleUiFactory.CreateScrollContent(rightColumn, "DetailScroll", spacing: 12, padding: 24);
            BuildMapScrollView(leftColumn);
        }

        private void BuildMapScrollView(Transform parent)
        {
            GameObject scrollObject = new("MapScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            scrollObject.transform.SetParent(parent, false);

            LayoutElement layoutElement = scrollObject.GetComponent<LayoutElement>();
            layoutElement.flexibleHeight = 1f;
            layoutElement.minHeight = 620f;

            Image background = scrollObject.GetComponent<Image>();
            background.color = new Color(0.05f, 0.08f, 0.12f, 0.92f);

            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = Vector2.zero;
            scrollRectTransform.offsetMax = Vector2.zero;

            GameObject viewportObject = new("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportObject.transform.SetParent(scrollObject.transform, false);

            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0.08f, 0.12f, 0.16f, 0.98f);

            RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8f, 8f);
            viewportRect.offsetMax = new Vector2(-8f, -8f);

            GameObject contentObject = new("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewportObject.transform, false);
            mapContent = contentObject.GetComponent<RectTransform>();
            mapContent.anchorMin = new Vector2(0f, 1f);
            mapContent.anchorMax = new Vector2(0f, 1f);
            mapContent.pivot = new Vector2(0f, 1f);
            mapContent.anchoredPosition = Vector2.zero;

            connectionLayer = CreateLayer(mapContent, "Connections");
            nodeLayer = CreateLayer(mapContent, "Nodes");

            mapScrollRect = scrollObject.GetComponent<ScrollRect>();
            mapScrollRect.viewport = viewportRect;
            mapScrollRect.content = mapContent;
            mapScrollRect.horizontal = true;
            mapScrollRect.vertical = true;
            mapScrollRect.movementType = ScrollRect.MovementType.Clamped;
            mapScrollRect.scrollSensitivity = 32f;
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
                $"Augments: {coordinator.GetOwnedAugments().Count}\n" +
                $"Meta Currency: {coordinator.Profile.metaCurrency}";

            int nodeCount = 0;
            foreach (RunMapNodeData _ in coordinator.GetMapNodes())
                nodeCount++;

            detailText.text = coordinator.CurrentMapTemplate != null
                ? $"Map: {coordinator.CurrentMapTemplate.displayName}\nNodes: {nodeCount}\nSeed: {coordinator.CurrentRun.seed}"
                : "No map loaded.";

            RebuildMapGraph(coordinator);

            if (coordinator.CurrentRun.pendingReward != null)
            {
                selectedRestAugmentUniqueId = null;
                ShowRewardPanel(coordinator);
                return;
            }

            RunMapNodeData currentNode = coordinator.GetNode(coordinator.CurrentRun.currentNodeId);
            if (currentNode != null && !coordinator.CurrentRun.HasCompletedNode(currentNode.nodeId))
            {
                if (currentNode.nodeType == MapNodeType.Shop)
                {
                    selectedRestAugmentUniqueId = null;
                    ShowShopPanel(coordinator, currentNode);
                    return;
                }

                if (currentNode.nodeType == MapNodeType.Rest)
                {
                    ShowRestPanel(coordinator, currentNode);
                    return;
                }
            }

            selectedRestAugmentUniqueId = null;
            ShowInfoPanel("Select an available node.");
        }

        private void RebuildMapGraph(RunCoordinator coordinator)
        {
            if (mapContent == null || connectionLayer == null || nodeLayer == null)
                return;

            SimpleUiFactory.ClearChildren(connectionLayer);
            SimpleUiFactory.ClearChildren(nodeLayer);

            List<RunMapNodeData> nodes = new();
            foreach (RunMapNodeData node in coordinator.GetMapNodes())
            {
                if (node != null)
                    nodes.Add(node);
            }

            if (nodes.Count == 0)
            {
                mapContent.sizeDelta = new Vector2(640f, 320f);
                return;
            }

            nodes.Sort((left, right) =>
            {
                int columnComparison = left.column.CompareTo(right.column);
                return columnComparison != 0 ? columnComparison : left.lane.CompareTo(right.lane);
            });

            HashSet<string> completedIds = new(coordinator.CurrentRun.completedNodeIds ?? new List<string>());
            HashSet<string> availableIds = new();
            List<RunMapNodeData> availableNodes = coordinator.GetAvailableNodes();
            for (int i = 0; i < availableNodes.Count; i++)
            {
                RunMapNodeData node = availableNodes[i];
                if (node != null)
                    availableIds.Add(node.nodeId);
            }

            Dictionary<int, int> nodesPerColumn = new();
            int maxColumn = 0;
            int maxRows = Mathf.Max(1, coordinator.CurrentMapTemplate != null ? coordinator.CurrentMapTemplate.maxActivePaths : 1);
            for (int i = 0; i < nodes.Count; i++)
            {
                RunMapNodeData node = nodes[i];
                maxColumn = Mathf.Max(maxColumn, node.column);
                if (!nodesPerColumn.TryGetValue(node.column, out int count))
                    count = 0;

                count++;
                nodesPerColumn[node.column] = count;
                maxRows = Mathf.Max(maxRows, count);
            }

            float contentWidth = (maxColumn * ColumnSpacing) + (MapPadding * 2f) + BossWidth;
            float contentHeight = ((maxRows - 1) * LaneSpacing) + (MapPadding * 2f) + BossHeight;
            mapContent.sizeDelta = new Vector2(contentWidth, contentHeight);
            StretchToParent(connectionLayer);
            StretchToParent(nodeLayer);

            Dictionary<string, Vector2> positions = new();
            for (int i = 0; i < nodes.Count; i++)
            {
                RunMapNodeData node = nodes[i];
                positions[node.nodeId] = GetNodePosition(node, nodesPerColumn, maxRows);
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                RunMapNodeData node = nodes[i];
                if (node.nextNodeIds == null)
                    continue;

                for (int nextIndex = 0; nextIndex < node.nextNodeIds.Count; nextIndex++)
                {
                    if (!positions.TryGetValue(node.nodeId, out Vector2 startPosition) ||
                        !positions.TryGetValue(node.nextNodeIds[nextIndex], out Vector2 endPosition))
                    {
                        continue;
                    }

                    CreateConnection(startPosition, endPosition, GetConnectionColor(node, node.nextNodeIds[nextIndex], completedIds, availableIds));
                }
            }

            bool disableSelection = coordinator.CurrentRun.pendingReward != null;
            for (int i = 0; i < nodes.Count; i++)
            {
                RunMapNodeData node = nodes[i];
                bool isCompleted = completedIds.Contains(node.nodeId);
                bool isAvailable = availableIds.Contains(node.nodeId);
                bool isCurrent = coordinator.CurrentRun.currentNodeId == node.nodeId && !isCompleted;
                CreateNodeButton(node, positions[node.nodeId], isCompleted, isAvailable, isCurrent, disableSelection);
            }

            FocusScrollOnCurrentNode(coordinator, positions, contentWidth, contentHeight);
        }

        private void OnNodeSelected(RunMapNodeData node)
        {
            if (node == null)
                return;

            RunCoordinator coordinator = GameFlowRoot.Instance.Coordinator;
            coordinator.SelectNode(node.nodeId);

            if (node.nodeType == MapNodeType.Shop)
            {
                selectedRestAugmentUniqueId = null;
                ShowShopPanel(coordinator, node);
                RebuildMapGraph(coordinator);
                return;
            }

            if (node.nodeType == MapNodeType.Rest)
            {
                selectedRestAugmentUniqueId = null;
                ShowRestPanel(coordinator, node);
                RebuildMapGraph(coordinator);
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
            SimpleUiFactory.CreateText(detailRoot, "Choose one reward to keep, or skip it.", 22);

            RectTransform rewardsSection = SimpleUiFactory.CreateSection(detailRoot, "RewardChoices");
            List<PendingRewardEntry> rewards = coordinator.GetPendingRewards();
            if (rewards.Count == 0)
            {
                SimpleUiFactory.CreateText(rewardsSection, "No rewards are available.", 20);
            }

            for (int i = 0; i < rewards.Count; i++)
            {
                PendingRewardEntry rewardEntry = rewards[i];
                if (!TryGetRewardPresentation(rewardEntry, out Sprite icon, out string title, out string subtitle, out string detail))
                    continue;

                RunRewardType rewardType = rewardEntry.rewardType;
                string contentId = rewardEntry.contentId;
                SimpleUiFactory.CreateItemTile(rewardsSection, icon, title, subtitle, detail, () =>
                {
                    if (coordinator.ClaimPendingReward(rewardType, contentId))
                        RefreshUi();
                });
            }

            SimpleUiFactory.CreateButton(detailRoot, "Skip", () =>
            {
                coordinator.SkipPendingReward();
                RefreshUi();
            });
        }

        private void ShowRestPanel(RunCoordinator coordinator, RunMapNodeData node)
        {
            SimpleUiFactory.ClearChildren(detailRoot);
            SimpleUiFactory.CreateText(detailRoot, node.DisplayNameOrFallback, 28);
            SimpleUiFactory.CreateText(detailRoot, "Choose exactly one rest action.", 22);
            SimpleUiFactory.CreateText(detailRoot, $"Gold: {coordinator.CurrentRun.gold}", 20);

            OwnedAugment selectedAugment = coordinator.GetOwnedAugment(selectedRestAugmentUniqueId);
            if (selectedAugment?.Definition != null)
            {
                ShowRestAugmentTargetPanel(coordinator, node, selectedAugment);
                return;
            }

            selectedRestAugmentUniqueId = null;

            RectTransform actionSection = SimpleUiFactory.CreateSection(detailRoot, "RestActions", 10);
            int healAmount = coordinator.GetRestHealAmount();
            SimpleUiFactory.CreateItemTile(
                actionSection,
                null,
                $"Heal {healAmount}",
                "Rest Action",
                "Recover health and end this rest stop.",
                () =>
                {
                    coordinator.ApplyRestHeal(node.nodeId);
                    RefreshUi();
                });

            List<OwnedCard> upgradeableCards = coordinator.GetUpgradeableCards();
            if (upgradeableCards.Count > 0)
            {
                SimpleUiFactory.CreateText(detailRoot, "Upgradeable Cards", 24);
                RectTransform upgradeSection = SimpleUiFactory.CreateSection(detailRoot, "UpgradeableCards", 10);
                for (int i = 0; i < upgradeableCards.Count; i++)
                {
                    OwnedCard card = upgradeableCards[i];
                    if (card?.CurrentDefinition == null)
                        continue;

                    string uniqueId = card.UniqueId;
                    SimpleUiFactory.CreateItemTile(
                        upgradeSection,
                        card.CurrentDefinition.icon,
                        card.CurrentDefinition.displayName,
                        "Upgrade Card",
                        GetCardRestDetail(card),
                        () =>
                        {
                            if (coordinator.ApplyRestUpgrade(node.nodeId, uniqueId))
                                RefreshUi();
                        });
                }
            }

            SimpleUiFactory.CreateText(detailRoot, "Owned Augments", 24);
            RectTransform augmentSection = SimpleUiFactory.CreateSection(detailRoot, "OwnedAugments", 10);
            List<OwnedAugment> ownedAugments = coordinator.GetOwnedAugments();
            if (ownedAugments.Count == 0)
            {
                SimpleUiFactory.CreateText(augmentSection, "No augments stored yet.", 20);
                return;
            }

            for (int i = 0; i < ownedAugments.Count; i++)
            {
                OwnedAugment ownedAugment = ownedAugments[i];
                if (ownedAugment?.Definition == null)
                    continue;

                string uniqueAugmentId = ownedAugment.UniqueId;
                List<OwnedCard> validTargets = coordinator.GetValidAugmentTargets(uniqueAugmentId);
                bool hasValidTargets = validTargets.Count > 0;
                string detail = $"{ownedAugment.Definition.description}\nApply for {Mathf.Max(0, ownedAugment.Definition.applicationCost)} gold.";
                if (!hasValidTargets)
                    detail += "\nNo valid cards right now.";

                SimpleUiFactory.CreateItemTile(
                    augmentSection,
                    ownedAugment.Definition.icon,
                    ownedAugment.Definition.displayName,
                    "Stored Augment",
                    detail,
                    () =>
                    {
                        selectedRestAugmentUniqueId = uniqueAugmentId;
                        RefreshUi();
                    },
                    interactable: hasValidTargets);
            }
        }

        private void ShowShopPanel(RunCoordinator coordinator, RunMapNodeData node)
        {
            SimpleUiFactory.ClearChildren(detailRoot);
            SimpleUiFactory.CreateText(detailRoot, node.DisplayNameOrFallback, 28);
            SimpleUiFactory.CreateText(detailRoot, $"Gold: {coordinator.CurrentRun.gold}", 22);

            RectTransform offersSection = SimpleUiFactory.CreateSection(detailRoot, "ShopOffers", 10);
            List<ShopOfferData> offers = coordinator.GetAvailableShopOffers(node.nodeId);
            if (offers.Count == 0)
            {
                SimpleUiFactory.CreateText(offersSection, "No offers remain. Leave when ready.", 22);
            }

            for (int i = 0; i < offers.Count; i++)
            {
                ShopOfferData offer = offers[i];
                if (offer == null)
                    continue;

                string purchaseOfferId = offer.OfferId;
                SimpleUiFactory.CreateItemTile(
                    offersSection,
                    GetOfferIcon(offer),
                    offer.GetDisplayName(),
                    GetOfferSubtitle(offer),
                    GetOfferDetail(offer),
                    () =>
                    {
                        if (coordinator.TryPurchaseShopOffer(node.nodeId, purchaseOfferId))
                            RefreshUi();
                    },
                    interactable: coordinator.CurrentRun.gold >= offer.price);
            }

            SimpleUiFactory.CreateButton(detailRoot, "Leave Shop", () =>
            {
                coordinator.LeaveShop(node.nodeId);
                RefreshUi();
            });
        }

        private void ShowRestAugmentTargetPanel(RunCoordinator coordinator, RunMapNodeData node, OwnedAugment selectedAugment)
        {
            int applicationCost = Mathf.Max(0, selectedAugment.Definition.applicationCost);
            bool canAfford = coordinator.CurrentRun.gold >= applicationCost;

            SimpleUiFactory.CreateText(detailRoot, "Selected Augment", 24);
            SimpleUiFactory.CreateItemTile(
                detailRoot,
                selectedAugment.Definition.icon,
                selectedAugment.Definition.displayName,
                "Choose a compatible card",
                $"{selectedAugment.Definition.description}\nApply for {applicationCost} gold.",
                null,
                interactable: false);

            if (!canAfford)
                SimpleUiFactory.CreateText(detailRoot, $"You need {applicationCost} gold to apply this augment.", 20);

            SimpleUiFactory.CreateButton(detailRoot, "Back", () =>
            {
                selectedRestAugmentUniqueId = null;
                RefreshUi();
            });

            RectTransform targetsSection = SimpleUiFactory.CreateSection(detailRoot, "AugmentTargets", 10);
            List<OwnedCard> targets = coordinator.GetValidAugmentTargets(selectedAugment.UniqueId);
            if (targets.Count == 0)
            {
                SimpleUiFactory.CreateText(targetsSection, "No cards can take this augment right now.", 20);
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                OwnedCard card = targets[i];
                if (card?.CurrentDefinition == null)
                    continue;

                string cardId = card.UniqueId;
                string augmentId = selectedAugment.UniqueId;
                SimpleUiFactory.CreateItemTile(
                    targetsSection,
                    card.CurrentDefinition.icon,
                    card.CurrentDefinition.displayName,
                    "Compatible Card",
                    GetCardRestDetail(card),
                    () =>
                    {
                        if (coordinator.ApplyRestAugment(node.nodeId, augmentId, cardId))
                        {
                            selectedRestAugmentUniqueId = null;
                            RefreshUi();
                        }
                    },
                    interactable: canAfford);
            }
        }

        private bool TryGetRewardPresentation(PendingRewardEntry rewardEntry, out Sprite icon, out string title, out string subtitle, out string detail)
        {
            icon = null;
            title = null;
            subtitle = null;
            detail = null;

            if (rewardEntry == null || GameFlowRoot.Instance == null)
                return false;

            RunContentRepository contentRepository = GameFlowRoot.Instance.ContentRepository;
            switch (rewardEntry.rewardType)
            {
                case RunRewardType.Card:
                    CardDef card = contentRepository.GetCardById(rewardEntry.contentId);
                    if (card == null)
                        return false;

                    icon = card.icon;
                    title = card.displayName;
                    subtitle = "Card Reward";
                    detail = GetCardRewardDetail(card);
                    return true;

                case RunRewardType.Augment:
                    CardAugmentDef augment = contentRepository.GetAugmentById(rewardEntry.contentId);
                    if (augment == null)
                        return false;

                    icon = augment.icon;
                    title = augment.displayName;
                    subtitle = "Augment Reward";
                    detail = GetAugmentRewardDetail(augment);
                    return true;
            }

            return false;
        }

        private static Sprite GetOfferIcon(ShopOfferData offer)
        {
            return offer?.offerType switch
            {
                ShopOfferType.Card => offer.card != null ? offer.card.icon : null,
                ShopOfferType.Augment => offer.augment != null ? offer.augment.icon : null,
                _ => null
            };
        }

        private static string GetOfferSubtitle(ShopOfferData offer)
        {
            return offer?.offerType switch
            {
                ShopOfferType.Card => "Card Offer",
                ShopOfferType.Augment => "Augment Offer",
                ShopOfferType.Heal => "Shop Service",
                _ => "Offer"
            };
        }

        private static string GetOfferDetail(ShopOfferData offer)
        {
            if (offer == null)
                return string.Empty;

            return offer.offerType switch
            {
                ShopOfferType.Card when offer.card != null =>
                    $"{offer.card.description}\nBuy for {offer.price} gold.",
                ShopOfferType.Augment when offer.augment != null =>
                    $"{offer.augment.description}\nBuy for {offer.price} gold. Apply later for {Mathf.Max(0, offer.augment.applicationCost)} gold.",
                ShopOfferType.Heal =>
                    $"Recover {offer.healAmount} health for {offer.price} gold.",
                _ => $"{offer.price} gold"
            };
        }

        private static string GetCardRewardDetail(CardDef card)
        {
            if (card == null)
                return string.Empty;

            return $"{card.description}\nMana Cost {card.baseManaCost}";
        }

        private static string GetAugmentRewardDetail(CardAugmentDef augment)
        {
            if (augment == null)
                return string.Empty;

            return $"{augment.description}\nApply later for {Mathf.Max(0, augment.applicationCost)} gold.";
        }

        private static string GetCardRestDetail(OwnedCard card)
        {
            if (card?.CurrentDefinition == null)
                return string.Empty;

            return $"Augments {card.AppliedAugments.Count}/{card.GetTotalAugmentSlots()}\n{card.CurrentDefinition.description}";
        }

        private static RectTransform CreateLayer(Transform parent, string name)
        {
            GameObject layerObject = new(name, typeof(RectTransform));
            layerObject.transform.SetParent(parent, false);
            RectTransform layerRect = layerObject.GetComponent<RectTransform>();
            StretchToParent(layerRect);
            return layerRect;
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static Vector2 GetNodePosition(RunMapNodeData node, Dictionary<int, int> nodesPerColumn, int maxRows)
        {
            int nodesInColumn = nodesPerColumn.TryGetValue(node.column, out int count) ? Mathf.Max(1, count) : 1;
            float x = MapPadding + (node.column * ColumnSpacing);
            float columnOffset = ((maxRows - nodesInColumn) * LaneSpacing) * 0.5f;
            float y = MapPadding + columnOffset + (node.lane * LaneSpacing);
            return new Vector2(x, y);
        }

        private void CreateConnection(Vector2 logicalStart, Vector2 logicalEnd, Color color)
        {
            GameObject lineObject = new("Connection", typeof(RectTransform), typeof(Image));
            lineObject.transform.SetParent(connectionLayer, false);

            Image image = lineObject.GetComponent<Image>();
            image.color = color;

            RectTransform rectTransform = lineObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 0.5f);

            Vector2 start = new(logicalStart.x, -logicalStart.y);
            Vector2 end = new(logicalEnd.x, -logicalEnd.y);
            Vector2 delta = end - start;
            float length = delta.magnitude;
            rectTransform.sizeDelta = new Vector2(length, ConnectionThickness);
            rectTransform.anchoredPosition = start;
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private void CreateNodeButton(RunMapNodeData node, Vector2 logicalPosition, bool isCompleted, bool isAvailable, bool isCurrent, bool disableSelection)
        {
            Vector2 size = node.nodeType == MapNodeType.Boss
                ? new Vector2(BossWidth, BossHeight)
                : new Vector2(NodeWidth, NodeHeight);

            GameObject buttonObject = new(node.nodeId, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(nodeLayer, false);

            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = new Vector2(logicalPosition.x, -logicalPosition.y);

            Image background = buttonObject.GetComponent<Image>();
            background.color = GetNodeColor(node, isCompleted, isAvailable, isCurrent);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            button.interactable = isAvailable && !disableSelection;
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = button.colors;
            colors.normalColor = background.color;
            colors.highlightedColor = Color.Lerp(background.color, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(background.color, Color.black, 0.15f);
            colors.disabledColor = Color.Lerp(background.color, Color.black, 0.35f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.onClick.AddListener(() => OnNodeSelected(node));

            Text label = SimpleUiFactory.CreateText(buttonObject.transform, node.DisplayNameOrFallback, 20, TextAnchor.MiddleCenter);
            label.color = Color.white;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 20;

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 10f);
            labelRect.offsetMax = new Vector2(-10f, -10f);
        }

        private void FocusScrollOnCurrentNode(RunCoordinator coordinator, Dictionary<string, Vector2> positions, float contentWidth, float contentHeight)
        {
            if (mapScrollRect == null || mapScrollRect.viewport == null)
                return;

            RunMapNodeData currentNode = coordinator.GetNode(coordinator.CurrentRun.currentNodeId);
            if (currentNode == null && !string.IsNullOrWhiteSpace(coordinator.CurrentRun.mapState?.startNodeId))
                currentNode = coordinator.GetNode(coordinator.CurrentRun.mapState.startNodeId);

            if (currentNode == null || !positions.TryGetValue(currentNode.nodeId, out Vector2 logicalPosition))
                return;

            Canvas.ForceUpdateCanvases();

            float viewportWidth = mapScrollRect.viewport.rect.width;
            float viewportHeight = mapScrollRect.viewport.rect.height;
            float maxOffsetX = Mathf.Max(0f, contentWidth - viewportWidth);
            float maxOffsetY = Mathf.Max(0f, contentHeight - viewportHeight);
            float targetOffsetX = Mathf.Clamp(logicalPosition.x - (viewportWidth * 0.5f), 0f, maxOffsetX);
            float targetOffsetY = Mathf.Clamp(logicalPosition.y - (viewportHeight * 0.5f), 0f, maxOffsetY);

            mapScrollRect.horizontalNormalizedPosition = maxOffsetX <= 0f ? 0f : targetOffsetX / maxOffsetX;
            mapScrollRect.verticalNormalizedPosition = maxOffsetY <= 0f ? 1f : 1f - (targetOffsetY / maxOffsetY);
        }

        private static Color GetConnectionColor(RunMapNodeData fromNode, string toNodeId, HashSet<string> completedIds, HashSet<string> availableIds)
        {
            bool fromCompleted = completedIds.Contains(fromNode.nodeId);
            bool toCompleted = completedIds.Contains(toNodeId);
            bool toAvailable = availableIds.Contains(toNodeId);

            if (fromCompleted && toCompleted)
                return new Color(0.88f, 0.8f, 0.54f, 0.92f);

            if (fromCompleted && toAvailable)
                return new Color(0.58f, 0.84f, 0.98f, 0.92f);

            return new Color(0.3f, 0.38f, 0.46f, 0.72f);
        }

        private static Color GetNodeColor(RunMapNodeData node, bool isCompleted, bool isAvailable, bool isCurrent)
        {
            if (isCompleted)
                return node.nodeType == MapNodeType.Boss
                    ? new Color(0.62f, 0.28f, 0.18f, 1f)
                    : new Color(0.26f, 0.34f, 0.22f, 1f);

            if (isCurrent)
                return new Color(0.34f, 0.54f, 0.8f, 1f);

            if (isAvailable)
            {
                return node.nodeType switch
                {
                    MapNodeType.Shop => new Color(0.26f, 0.46f, 0.62f, 1f),
                    MapNodeType.Rest => new Color(0.22f, 0.52f, 0.36f, 1f),
                    MapNodeType.Miniboss => new Color(0.68f, 0.42f, 0.18f, 1f),
                    MapNodeType.Boss => new Color(0.74f, 0.22f, 0.18f, 1f),
                    _ => new Color(0.38f, 0.52f, 0.3f, 1f)
                };
            }

            return node.nodeType switch
            {
                MapNodeType.Shop => new Color(0.14f, 0.24f, 0.3f, 1f),
                MapNodeType.Rest => new Color(0.14f, 0.26f, 0.18f, 1f),
                MapNodeType.Miniboss => new Color(0.28f, 0.18f, 0.12f, 1f),
                MapNodeType.Boss => new Color(0.34f, 0.12f, 0.12f, 1f),
                MapNodeType.Start => new Color(0.18f, 0.2f, 0.26f, 1f),
                _ => new Color(0.18f, 0.22f, 0.26f, 1f)
            };
        }
    }
}
