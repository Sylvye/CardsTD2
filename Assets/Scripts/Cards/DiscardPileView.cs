using Combat;
using TMPro;
using UnityEngine;

namespace Cards
{
    public class DiscardPileView : MonoBehaviour
    {
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text exhaustCountText;

        private CombatCardState cardState;
        private HandController handController;

        public void Initialize(CombatCardState combatCardState, HandController controller)
        {
            cardState = combatCardState;
            handController = controller;

            if (handController != null)
                handController.OnHandChanged += Refresh;

            Refresh();
        }

        private void OnDestroy()
        {
            if (handController != null)
                handController.OnHandChanged -= Refresh;
        }

        private void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (cardState == null)
                return;

            if (countText is not null)
                countText.text = $"{cardState.DiscardPile.Count}";

            if (exhaustCountText is not null)
                exhaustCountText.text = $"{cardState.ExhaustPile.Count}";
        }
    }
}
