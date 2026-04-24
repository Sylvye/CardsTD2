using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RunFlow
{
    public class MainMenuSceneController : MonoBehaviour
    {
        private Text metaCurrencyText;
        private Button continueButton;
        private RectTransform unlockShopOverlayRoot;
        private Text unlockShopCurrencyText;
        private RectTransform unlockShopContent;
        private PauseMenuController pauseMenuController;

        private void Start()
        {
            BuildUi();
            EnsurePauseMenu();
            RefreshUi();
        }

        private void OnEnable()
        {
            RefreshUi();
        }

        private void BuildUi()
        {
            Canvas canvas = SimpleUiFactory.EnsureCanvas();
            RectTransform panel = SimpleUiFactory.CreatePanel(canvas.transform, "MainMenuPanel");
            SimpleUiFactory.AddVerticalLayout(panel, spacing: 16, padding: 40);

            SimpleUiFactory.CreateText(panel, "CardsTD2", 42, TextAnchor.MiddleCenter);
            SimpleUiFactory.CreateText(panel, "Run Flow Prototype", 24, TextAnchor.MiddleCenter);
            metaCurrencyText = SimpleUiFactory.CreateText(panel, string.Empty, 24, TextAnchor.MiddleCenter);

            SimpleUiFactory.CreateButton(panel, "New Run", StartNewRun);
            continueButton = SimpleUiFactory.CreateButton(panel, "Continue Run", ContinueRun);
            SimpleUiFactory.CreateButton(panel, "Unlock Shop", ShowUnlockShop);
        }

        private void RefreshUi()
        {
            if (metaCurrencyText == null || GameFlowRoot.Instance == null)
                return;

            RunCoordinator coordinator = GameFlowRoot.Instance.Coordinator;
            metaCurrencyText.text = $"Meta Currency: {coordinator.Profile.metaCurrency}";

            if (continueButton != null)
                continueButton.interactable = coordinator.CanContinueRun;

            if (unlockShopOverlayRoot != null)
                RebuildUnlockShop(coordinator);
        }

        private void StartNewRun()
        {
            GameFlowRoot.Instance.Coordinator.StartNewRun();
        }

        private void ContinueRun()
        {
            GameFlowRoot.Instance.Coordinator.ContinueRun();
        }

        private void ShowUnlockShop()
        {
            if (unlockShopOverlayRoot != null)
                return;

            Canvas canvas = SimpleUiFactory.EnsureCanvas();
            unlockShopOverlayRoot = SimpleUiFactory.CreateFullscreenBlocker(
                canvas.transform,
                "UnlockShopOverlay",
                new Color(0f, 0f, 0f, 0.62f));

            RectTransform panel = SimpleUiFactory.CreateDialogPanel(unlockShopOverlayRoot, "UnlockShopPanel", new Vector2(900f, 760f));
            SimpleUiFactory.AddVerticalLayout(panel, spacing: 14, padding: 24);

            SimpleUiFactory.CreateText(panel, "Unlock Shop", 34, TextAnchor.MiddleCenter);
            unlockShopCurrencyText = SimpleUiFactory.CreateText(panel, string.Empty, 24, TextAnchor.MiddleCenter);

            unlockShopContent = SimpleUiFactory.CreateScrollContent(panel, "UnlockShopScroll", spacing: 12, padding: 16);
            LayoutElement scrollLayout = unlockShopContent.parent.parent.gameObject.AddComponent<LayoutElement>();
            scrollLayout.minHeight = 520f;
            scrollLayout.flexibleHeight = 1f;

            SimpleUiFactory.CreateButton(panel, "Back", CloseUnlockShop);
            RefreshUi();
        }

        private void RebuildUnlockShop(RunCoordinator coordinator)
        {
            if (unlockShopContent == null || coordinator == null)
                return;

            if (unlockShopCurrencyText != null)
                unlockShopCurrencyText.text = $"Meta Currency: {coordinator.Profile.metaCurrency}";

            SimpleUiFactory.ClearChildren(unlockShopContent);

            var unlocks = coordinator.GetMetaUnlocks();
            if (unlocks.Count == 0)
            {
                SimpleUiFactory.CreateText(unlockShopContent, "No unlocks are configured yet.", 22, TextAnchor.MiddleCenter);
                return;
            }

            for (int i = 0; i < unlocks.Count; i++)
            {
                MetaUnlockEntry unlock = unlocks[i];
                if (unlock == null)
                    continue;

                string unlockId = unlock.UnlockId;
                bool isPurchased = coordinator.IsMetaUnlockPurchased(unlockId);
                bool isPlaceholder = unlock.type == MetaUnlockType.Relic;
                bool prerequisitesMet = coordinator.AreMetaUnlockPrerequisitesMet(unlock);
                bool canAfford = coordinator.Profile.metaCurrency >= Mathf.Max(0, unlock.cost);
                bool canPurchase = !isPurchased && !isPlaceholder && prerequisitesMet && canAfford;
                string status = GetUnlockStatus(unlock, isPurchased, canAfford, prerequisitesMet, coordinator.GetMetaUnlockRequirementText(unlock));
                string detail = BuildUnlockDetail(coordinator, unlock);

                SimpleUiFactory.CreateItemTile(
                    unlockShopContent,
                    unlock.GetIcon(),
                    unlock.GetDisplayName(),
                    $"{GetUnlockTypeLabel(unlock.type)} - {status}",
                    detail,
                    () =>
                    {
                        if (coordinator.TryPurchaseMetaUnlock(unlockId))
                            RefreshUi();
                    },
                    interactable: canPurchase);
            }
        }

        private void CloseUnlockShop()
        {
            if (unlockShopOverlayRoot != null)
                Destroy(unlockShopOverlayRoot.gameObject);

            unlockShopOverlayRoot = null;
            unlockShopCurrencyText = null;
            unlockShopContent = null;
            RefreshUi();
        }

        private static string GetUnlockTypeLabel(MetaUnlockType type)
        {
            return type switch
            {
                MetaUnlockType.Card => "Card Unlock",
                MetaUnlockType.Relic => "Relic Unlock",
                MetaUnlockType.UnlockGroup => "Unlock Group",
                _ => "Unlock"
            };
        }

        private static string GetUnlockStatus(MetaUnlockEntry unlock, bool isPurchased, bool canAfford, bool prerequisitesMet, string requirementText)
        {
            if (isPurchased)
                return "Unlocked";

            if (unlock != null && unlock.type == MetaUnlockType.Relic)
                return "Coming Soon";

            if (!prerequisitesMet)
                return string.IsNullOrWhiteSpace(requirementText) ? "Locked" : requirementText;

            return canAfford ? "Available" : "Need More Currency";
        }

        private static string BuildUnlockDetail(RunCoordinator coordinator, MetaUnlockEntry unlock)
        {
            string detail = $"{unlock.GetDescription()}\nCost: {Mathf.Max(0, unlock.cost)} Meta Currency";
            if (unlock.type != MetaUnlockType.UnlockGroup)
                return detail;

            var contents = coordinator.GetUnlockContents(unlock);
            if (contents.Count == 0)
                return detail;

            List<string> names = new();
            for (int i = 0; i < contents.Count; i++)
            {
                MetaUnlockContent content = contents[i];
                if (content != null)
                    names.Add(content.GetDisplayName());
            }

            return names.Count > 0
                ? $"{detail}\nIncludes: {string.Join(", ", names)}"
                : detail;
        }

        private void EnsurePauseMenu()
        {
            pauseMenuController ??= gameObject.GetComponent<PauseMenuController>();
            pauseMenuController ??= gameObject.AddComponent<PauseMenuController>();
            pauseMenuController.Initialize(GameFlowRoot.Instance != null ? GameFlowRoot.Instance.Coordinator : null, false);
        }
    }
}
