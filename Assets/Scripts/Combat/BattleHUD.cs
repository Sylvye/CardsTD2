using TMPro;
using UnityEngine;

namespace Combat
{
    public class BattleHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text manaText;
        [SerializeField] private TMP_Text livesText;

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

            if (livesText is not null)
                livesText.text = $"Lives: {playerState.Lives}";
        }
    }
}