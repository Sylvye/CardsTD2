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
        private RectTransform debugConsoleRoot;
        private Text titleText;
        private Text debugToggleText;
        private Text closeButtonText;
        private Text debugResultText;
        private InputField debugCommandInputField;
        private DebugConsoleCommandProcessor debugConsoleCommandProcessor;

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

            RectTransform panel = SimpleUiFactory.CreateDialogPanel(overlayRoot, "PauseMenuPanel", new Vector2(640f, 460f));
            SimpleUiFactory.AddVerticalLayout(panel, spacing: 18, padding: 24);

            titleText = SimpleUiFactory.CreateText(panel, string.Empty, 34, TextAnchor.MiddleCenter);
            SimpleUiFactory.CreateText(panel, "Press Esc to close this menu.", 20, TextAnchor.MiddleCenter);

            Button debugToggleButton = SimpleUiFactory.CreateButton(panel, string.Empty, ToggleDebugUi);
            debugToggleText = debugToggleButton.GetComponentInChildren<Text>();

            Button closeButton = SimpleUiFactory.CreateButton(panel, string.Empty, CloseMenu);
            closeButtonText = closeButton.GetComponentInChildren<Text>();

            BuildDebugConsole(panel);

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

            if (debugConsoleRoot != null)
                debugConsoleRoot.gameObject.SetActive(IsDebugUiEnabled());

            if (debugResultText != null && string.IsNullOrWhiteSpace(debugResultText.text) && IsDebugUiEnabled())
                debugResultText.text = "Enter a debug command.";
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

        private void BuildDebugConsole(RectTransform panel)
        {
            debugConsoleRoot = SimpleUiFactory.CreateSection(panel, "DebugConsole", 10);

            Text headerText = SimpleUiFactory.CreateText(debugConsoleRoot, "Debug Console", 24);
            headerText.color = new Color(0.82f, 0.88f, 0.98f, 1f);

            GameObject inputRowObject = new("DebugConsoleInputRow", typeof(RectTransform));
            inputRowObject.transform.SetParent(debugConsoleRoot, false);
            RectTransform inputRow = inputRowObject.GetComponent<RectTransform>();
            SimpleUiFactory.AddHorizontalLayout(inputRow, spacing: 10, padding: 0);

            debugCommandInputField = SimpleUiFactory.CreateInputField(inputRow, "gain currency 50");
            debugCommandInputField.onEndEdit.AddListener(HandleDebugCommandEndEdit);
            Button executeButton = SimpleUiFactory.CreateButton(inputRow, "Execute", ExecuteDebugCommand);
            LayoutElement executeLayout = executeButton.gameObject.GetComponent<LayoutElement>();
            executeLayout.flexibleWidth = 0f;
            executeLayout.preferredWidth = 150f;

            debugResultText = SimpleUiFactory.CreateText(debugConsoleRoot, string.Empty, 20);
            debugResultText.color = new Color(0.7f, 0.78f, 0.9f, 1f);
        }

        private void ExecuteDebugCommand()
        {
            ExecuteDebugCommand(debugCommandInputField != null ? debugCommandInputField.text : string.Empty);
        }

        private void ExecuteDebugCommand(string commandText)
        {
            if (debugConsoleCommandProcessor == null || debugCommandInputField == null || debugResultText == null)
                return;

            DebugConsoleCommandResult result = debugConsoleCommandProcessor.Execute(commandText);
            debugResultText.text = result.Message;
            debugResultText.color = result.Success
                ? new Color(0.62f, 0.9f, 0.66f, 1f)
                : new Color(1f, 0.66f, 0.66f, 1f);

            debugCommandInputField.text = string.Empty;
            debugCommandInputField.ActivateInputField();
        }

        private void HandleDebugCommandEndEdit(string commandText)
        {
            if (!IsOpen || !IsSubmitKeyPressed())
                return;

            ExecuteDebugCommand(commandText);
        }

        private static bool IsSubmitKeyPressed()
        {
            return Keyboard.current?.enterKey.wasPressedThisFrame == true ||
                   Keyboard.current?.numpadEnterKey.wasPressedThisFrame == true;
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
            debugConsoleCommandProcessor = coordinator != null ? new DebugConsoleCommandProcessor(coordinator) : null;

            if (coordinator != null)
                coordinator.DebugUiChanged += HandleDebugUiChanged;
        }
    }
}
