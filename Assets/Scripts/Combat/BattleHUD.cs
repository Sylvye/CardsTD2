using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Combat
{
    public class BattleHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text manaText;
        [FormerlySerializedAs("livesText")]
        [SerializeField] private TMP_Text healthText;

        private PlayerState playerState;

        public void Initialize(PlayerState playerState)
        {
            this.playerState = playerState;
        }

        private void Update()
        {
            if (playerState == null)
                return;

            if (manaText is not null)
                manaText.text = $"Mana: {playerState.CurrentMana}";

            if (healthText is not null)
                healthText.text = $"Health: {playerState.CurrentHealth}/{playerState.MaxHealth}";
        }
    }
}
