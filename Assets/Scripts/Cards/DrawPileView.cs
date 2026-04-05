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
        [SerializeField] private TMP_Text drawCostText;
        [SerializeField] private Button drawButton;

        private CombatCardState cardState;
        private HandController handController;
        private Func<int> getDrawCost;
        private Func<bool> canDraw;
        private Action onDrawRequested;

        public void Initialize(
            CombatCardState combatCardState,
            HandController controller,
            Func<int> drawCostGetter,
            Func<bool> canDrawGetter,
            Action drawRequestedCallback)
        {
            cardState = combatCardState;
            handController = controller;
            getDrawCost = drawCostGetter;
            canDraw = canDrawGetter;
            onDrawRequested = drawRequestedCallback;

            if (handController != null)
                handController.OnHandChanged += Refresh;

            if (drawButton != null)
            {
                drawButton.onClick.RemoveAllListeners();
                drawButton.onClick.AddListener(HandleClicked);
            }

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

            if (drawCostText is not null && getDrawCost != null)
                drawCostText.text = $"Draw ({getDrawCost()})";

            if (drawButton is not null && canDraw != null)
                drawButton.interactable = canDraw();
        }

        private void HandleClicked()
        {
            onDrawRequested?.Invoke();
        }
    }
}