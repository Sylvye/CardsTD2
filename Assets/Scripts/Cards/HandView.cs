using System;
using Combat;
using UnityEngine;

namespace Cards
{
    public class HandView : MonoBehaviour
    {
        [SerializeField] private Transform cardContainer;
        [SerializeField] private CardView cardViewPrefab;

        private CombatCardState cardState;
        private HandController handController;
        private Func<int> getCurrentMana;
        private Action<CardInstance> onPlayRequested;

        public void Initialize(
            CombatCardState combatCardState,
            HandController controller,
            Func<int> manaGetter,
            Action<CardInstance> playRequestedCallback)
        {
            cardState = combatCardState;
            handController = controller;
            getCurrentMana = manaGetter;
            onPlayRequested = playRequestedCallback;

            handController.OnHandChanged += Rebuild;
            Rebuild();
        }

        private void OnDestroy()
        {
            if (handController != null)
                handController.OnHandChanged -= Rebuild;
        }

        public void Rebuild()
        {
            if (cardContainer == null || cardViewPrefab == null || cardState == null || handController == null)
                return;

            ClearExistingViews();

            foreach (CardInstance card in cardState.Hand.Cards)
            {
                CardView view = Instantiate(cardViewPrefab, cardContainer, false);
                view.Bind(card, HandleCardClicked);
            }
        }

        private void HandleCardClicked(CardInstance card)
        {
            if (card == null || handController == null || getCurrentMana == null)
                return;

            int mana = getCurrentMana();

            if (card.CurrentManaCost <= mana)
            {
                onPlayRequested?.Invoke(card);
            }
            else
            {
                Debug.Log($"Cannot play {card.DisplayName}. Not enough mana.");
            }
        }

        private void ClearExistingViews()
        {
            for (int i = cardContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(cardContainer.GetChild(i).gameObject);
            }
        }
    }
}