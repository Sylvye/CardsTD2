using TMPro;
using RunFlow;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Combat
{
    public class BattleHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text manaText;
        [FormerlySerializedAs("livesText")]
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text resolvedSessionText;
        [SerializeField] private Button speedButton;
        [SerializeField] private TMP_Text speedButtonText;

        private PlayerState playerState;
        private CombatSessionDriver combatSessionDriver;
        private RunCoordinator coordinator;

        public void Initialize(PlayerState playerState, CombatSessionDriver sessionDriver)
        {
            this.playerState = playerState;
            combatSessionDriver = sessionDriver;
            SetCoordinator(GameFlowRoot.Instance != null ? GameFlowRoot.Instance.Coordinator : null);

            if (speedButton != null)
            {
                speedButton.onClick.RemoveListener(HandleSpeedButtonClicked);
                speedButton.onClick.AddListener(HandleSpeedButtonClicked);
            }

            ApplyDebugVisibility();
            Refresh();
        }

        private void OnDestroy()
        {
            SetCoordinator(null);

            if (speedButton != null)
                speedButton.onClick.RemoveListener(HandleSpeedButtonClicked);
        }

        private void OnEnable()
        {
            ApplyDebugVisibility();
        }

        private void Update()
        {
            Refresh();
        }

        private void HandleSpeedButtonClicked()
        {
            combatSessionDriver?.CycleSimulationSpeed();
            Refresh();
        }

        private void Refresh()
        {
            if (manaText is not null && playerState != null)
                manaText.text = $"Mana: {playerState.CurrentMana}";

            if (healthText is not null && playerState != null)
                healthText.text = $"Health: {playerState.CurrentHealth}/{playerState.MaxHealth}";

            if (resolvedSessionText is not null)
            {
                bool isDebugUiEnabled = IsDebugUiEnabled();
                resolvedSessionText.gameObject.SetActive(isDebugUiEnabled);

                if (isDebugUiEnabled)
                    resolvedSessionText.text = BuildResolvedSessionText();
            }

            if (speedButtonText is not null)
            {
                float speedMultiplier = combatSessionDriver != null ? combatSessionDriver.CurrentSpeedMultiplier : 1f;
                speedButtonText.text = $"Speed: {speedMultiplier:0.#}x";
            }
        }

        private string BuildResolvedSessionText()
        {
            CombatSessionSetup setup = combatSessionDriver != null ? combatSessionDriver.ResolvedSetup : null;
            if (setup == null)
                return "Resolved Session: Unavailable";

            int currentHealth = playerState != null ? playerState.CurrentHealth : setup.CurrentHealth;
            int maxHealth = playerState != null ? playerState.MaxHealth : setup.MaxHealth;

            return
                "Resolved Session\n" +
                $"Starting Mana: {setup.StartingMana}\n" +
                $"Max Mana: {setup.MaxMana}\n" +
                $"Mana Regen: {setup.ManaRegenPerSecond:0.##}/s\n" +
                $"Health: {currentHealth}/{maxHealth}\n" +
                $"Opening Hand: {setup.OpeningHandSize}\n" +
                $"Manual Draw Cost: {setup.ManualDrawCost}";
        }

        private void HandleDebugUiChanged(bool enabled)
        {
            ApplyDebugVisibility();
        }

        private void ApplyDebugVisibility()
        {
            if (resolvedSessionText != null)
                resolvedSessionText.gameObject.SetActive(IsDebugUiEnabled());
        }

        private bool IsDebugUiEnabled()
        {
            return coordinator != null && coordinator.IsDebugUiEnabled;
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
