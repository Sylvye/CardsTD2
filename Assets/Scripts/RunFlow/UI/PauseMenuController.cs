using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RunFlow
{
    public class PauseMenuController : MonoBehaviour
    {
        private RunCoordinator coordinator;
        private bool pauseGameplay;
        private Action<bool> onMenuOpenChanged;
        private RectTransform overlayRoot;
        private Text titleText;
        private Text debugToggleText;
        private Text closeButtonText;

        public bool IsOpen => overlayRoot != null && overlayRoot.gameObject.activeSelf;

        public void Initialize(RunCoordinator coordinator, bool pauseGameplay, Action<bool> onMenuOpenChanged = null)
        {
            SetCoordinator(coordinator ?? ResolveCoordinator());
            this.pauseGameplay = pauseGameplay;
            this.onMenuOpenChanged = onMenuOpenChanged;

            EnsureMenuBuilt();
            RefreshMenu();
            CloseMenu();
        }

        private void OnDestroy()
        {
            SetCoordinator(null);
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame != true)
                return;

            ToggleMenu();
        }

        public void ToggleMenu()
        {
            SetMenuOpen(!IsOpen);
        }

        public void OpenMenu()
        {
            SetMenuOpen(true);
        }

        public void CloseMenu()
        {
            SetMenuOpen(false);
        }

        private void SetMenuOpen(bool open)
        {
            EnsureMenuBuilt();
            if (overlayRoot == null)
                return;

            if (overlayRoot.gameObject.activeSelf == open)
            {
                RefreshMenu();
                return;
            }

            overlayRoot.gameObject.SetActive(open);
            if (open)
                overlayRoot.SetAsLastSibling();

            RefreshMenu();
            onMenuOpenChanged?.Invoke(open);
        }

        private void EnsureMenuBuilt()
        {
            if (overlayRoot != null)
                return;

            Canvas canvas = SimpleUiFactory.EnsureCanvas();
            overlayRoot = SimpleUiFactory.CreateFullscreenBlocker(
                canvas.transform,
                "PauseMenuOverlay",
                new Color(0f, 0f, 0f, 0.72f));

            RectTransform panel = SimpleUiFactory.CreateDialogPanel(overlayRoot, "PauseMenuPanel", new Vector2(520f, 320f));
            SimpleUiFactory.AddVerticalLayout(panel, spacing: 18, padding: 24);

            titleText = SimpleUiFactory.CreateText(panel, string.Empty, 34, TextAnchor.MiddleCenter);
            SimpleUiFactory.CreateText(panel, "Press Esc to close this menu.", 20, TextAnchor.MiddleCenter);

            Button debugToggleButton = SimpleUiFactory.CreateButton(panel, string.Empty, ToggleDebugUi);
            debugToggleText = debugToggleButton.GetComponentInChildren<Text>();

            Button closeButton = SimpleUiFactory.CreateButton(panel, string.Empty, CloseMenu);
            closeButtonText = closeButton.GetComponentInChildren<Text>();

            overlayRoot.gameObject.SetActive(false);
        }

        private void RefreshMenu()
        {
            if (titleText != null)
                titleText.text = pauseGameplay ? "Paused" : "Settings";

            if (debugToggleText != null)
                debugToggleText.text = $"Debug UI: {(IsDebugUiEnabled() ? "On" : "Off")}";

            if (closeButtonText != null)
                closeButtonText.text = pauseGameplay ? "Resume" : "Close";
        }

        private void ToggleDebugUi()
        {
            if (coordinator == null)
                return;

            coordinator.SetDebugUiEnabled(!coordinator.IsDebugUiEnabled);
            RefreshMenu();
        }

        private bool IsDebugUiEnabled()
        {
            return coordinator != null && coordinator.IsDebugUiEnabled;
        }

        private void HandleDebugUiChanged(bool enabled)
        {
            RefreshMenu();
        }

        private RunCoordinator ResolveCoordinator()
        {
            if (GameFlowRoot.Instance != null)
                return GameFlowRoot.Instance.Coordinator;

            GameFlowRoot root = GameFlowRoot.EnsureInstance();
            return root != null ? root.Coordinator : null;
        }

        private void SetCoordinator(RunCoordinator newCoordinator)
        {
            if (coordinator == newCoordinator)
                return;

            if (coordinator != null)
                coordinator.DebugUiChanged -= HandleDebugUiChanged;

            coordinator = newCoordinator;

            if (coordinator != null)
                coordinator.DebugUiChanged += HandleDebugUiChanged;
        }
    }
}
