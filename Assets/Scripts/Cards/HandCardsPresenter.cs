using UnityEngine;

namespace Cards
{
    public class HandCardsPresenter
    {
        private readonly HandView handView;

        public SelectedCardController SelectedCardController { get; private set; }

        public HandCardsPresenter(HandView handView)
        {
            this.handView = handView;
        }

        public void Initialize(CombatCardState combatCardState, HandController handController)
        {
            SelectedCardController = new SelectedCardController();

            if (handView == null)
                return;

            handView.Initialize(
                combatCardState,
                handController,
                SelectedCardController,
                OnCardClicked
            );
        }

        private void OnCardClicked(CardInstance card)
        {
            if (card == null || SelectedCardController == null)
                return;

            if (SelectedCardController.HasSelection && SelectedCardController.SelectedCard == card)
            {
                SelectedCardController.Deselect();
                return;
            }

            SelectedCardController.Select(card);
        }
    }
}
