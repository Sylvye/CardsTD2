using TMPro;
using UnityEngine;
using Cards;

namespace Combat
{
    public class BattleHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text manaText;
        [SerializeField] private TMP_Text drawPileText;
        [SerializeField] private TMP_Text discardPileText;
        [SerializeField] private TMP_Text livesText;

        private PlayerState playerState;
        private CombatCardState cardState;

        public void Initialize(PlayerState playerState, CombatCardState cardState)
        {
            this.playerState = playerState;
            this.cardState = cardState;
        }

        private void Update()
        {
            if (playerState == null || cardState == null)
                return;

            if (manaText is not null)
                manaText.text = $"Mana: {playerState.CurrentMana}";

            if (drawPileText is not null)
                drawPileText.text = $"Draw: {cardState.DrawPile.Count}";

            if (discardPileText is not null)
                discardPileText.text = $"Discard: {cardState.DiscardPile.Count}";

            if (livesText is not null)
                livesText.text = $"Lives: {playerState.Lives}";
        }
    }
}