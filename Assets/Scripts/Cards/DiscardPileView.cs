using Combat;
using TMPro;
using UnityEngine;

namespace Cards
{
    public class DiscardPileView : MonoBehaviour
    {
        [SerializeField] private TMP_Text countText;

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
            if (cardState == null || countText is null)
                return;

            countText.text = $"{cardState.DiscardPile.Count}";
        }
    }
}