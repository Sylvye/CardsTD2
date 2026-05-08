using System;
using Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cards
{
    public class DrawPileView : MonoBehaviour
    {
        [SerializeField] private TMP_Text countText;

        private CombatCardState cardState;
        private HandController handController;

        public void Initialize(
            CombatCardState combatCardState,
            HandController controller)
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
                countText.text = $"{cardState.DrawPile.Count}";
        }
    }
}
