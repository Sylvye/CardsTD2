using TMPro;
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
        [SerializeField] private Button speedButton;
        [SerializeField] private TMP_Text speedButtonText;

        private PlayerState playerState;
        private CombatSessionDriver combatSessionDriver;

        public void Initialize(PlayerState playerState, CombatSessionDriver sessionDriver)
        {
            this.playerState = playerState;
            combatSessionDriver = sessionDriver;

            if (speedButton != null)
            {
                speedButton.onClick.RemoveListener(HandleSpeedButtonClicked);
                speedButton.onClick.AddListener(HandleSpeedButtonClicked);
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (speedButton != null)
                speedButton.onClick.RemoveListener(HandleSpeedButtonClicked);
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

            if (speedButtonText is not null)
            {
                float speedMultiplier = combatSessionDriver != null ? combatSessionDriver.CurrentSpeedMultiplier : 1f;
                speedButtonText.text = $"Speed: {speedMultiplier:0.#}x";
            }
        }
    }
}
