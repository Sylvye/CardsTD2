using UnityEngine;
using UnityEngine.UI;

namespace RunFlow
{
    public class MainMenuSceneController : MonoBehaviour
    {
        private Text metaCurrencyText;
        private Button continueButton;

        private void Start()
        {
            BuildUi();
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
        }

        private void RefreshUi()
        {
            if (metaCurrencyText == null || GameFlowRoot.Instance == null)
                return;

            RunCoordinator coordinator = GameFlowRoot.Instance.Coordinator;
            metaCurrencyText.text = $"Meta Currency: {coordinator.Profile.metaCurrency}";

            if (continueButton != null)
                continueButton.interactable = coordinator.CanContinueRun;
        }

        private void StartNewRun()
        {
            GameFlowRoot.Instance.Coordinator.StartNewRun();
        }

        private void ContinueRun()
        {
            GameFlowRoot.Instance.Coordinator.ContinueRun();
        }
    }
}
