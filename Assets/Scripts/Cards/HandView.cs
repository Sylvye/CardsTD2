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
        private SelectedCardController selectedCardController;
        private Action<CardInstance> onCardClicked;
        private PlayFieldRaycaster targetLineRaycaster;
        private Func<bool> isInputBlocked;

        public void Initialize(
            CombatCardState combatCardState,
            HandController controller,
            SelectedCardController selectedController,
            Action<CardInstance> cardClickedCallback)
        {
            cardState = combatCardState;
            handController = controller;
            selectedCardController = selectedController;
            onCardClicked = cardClickedCallback;

            if (handController is not null)
                handController.OnHandChanged += Rebuild;

            if (selectedCardController is not null)
                selectedCardController.OnSelectedCardChanged += HandleSelectedCardChanged;

            Rebuild();
        }

        private void OnDestroy()
        {
            if (handController is not null)
                handController.OnHandChanged -= Rebuild;

            if (selectedCardController is not null)
                selectedCardController.OnSelectedCardChanged -= HandleSelectedCardChanged;
        }

        public void Rebuild()
        {
            if (cardContainer is null || cardViewPrefab is null || cardState is null)
                return;

            ClearExistingViews();

            foreach (CardInstance card in cardState.Hand.Cards)
            {
                CardView view = Instantiate(cardViewPrefab, cardContainer, false);
                view.Bind(card, HandleCardClicked);

                bool isSelected = selectedCardController is not null &&
                                  selectedCardController.HasSelection &&
                                  selectedCardController.SelectedCard == card;

                view.SetSelected(isSelected);
                ConfigureTargetLine(view);
            }
        }

        public void ConfigureTargetLines(PlayFieldRaycaster raycaster, Func<bool> inputBlocked)
        {
            targetLineRaycaster = raycaster;
            isInputBlocked = inputBlocked;

            if (cardContainer == null)
                return;

            for (int i = 0; i < cardContainer.childCount; i++)
                ConfigureTargetLine(cardContainer.GetChild(i).GetComponent<CardView>());
        }

        private void HandleCardClicked(CardInstance card)
        {
            if (card is null)
                return;

            onCardClicked?.Invoke(card);
        }

        private void HandleSelectedCardChanged(CardInstance selectedCard)
        {
            UpdateSelectionVisuals(selectedCard);
        }

        private void UpdateSelectionVisuals(CardInstance selectedCard)
        {
            for (int i = 0; i < cardContainer.childCount; i++)
            {
                CardView view = cardContainer.GetChild(i).GetComponent<CardView>();
                if (view is null)
                    continue;

                bool isSelected = selectedCard is not null && view.BoundCard == selectedCard;
                view.SetSelected(isSelected);
            }
        }

        private void ClearExistingViews()
        {
            for (int i = cardContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(cardContainer.GetChild(i).gameObject);
            }
        }

        private void ConfigureTargetLine(CardView view)
        {
            if (view == null)
                return;

            view.ConfigureTargetLine(selectedCardController, targetLineRaycaster, isInputBlocked);
        }
    }
}
