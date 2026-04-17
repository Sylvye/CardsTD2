using Combat;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cards
{
    public class FieldCardUseController : MonoBehaviour
    {
        [SerializeField] private PlayFieldRaycaster playFieldRaycaster;

        private SelectedCardController selectedCardController;
        private HandController handController;
        private PlayerState playerState;
        private CardPlacementValidator validator;
        private Func<bool> isInputBlocked;

        public void Initialize(
            SelectedCardController selectedController,
            HandController handController,
            PlayerState playerState,
            CardPlacementValidator placementValidator,
            Func<bool> inputBlocked = null)
        {
            selectedCardController = selectedController;
            this.handController = handController;
            this.playerState = playerState;
            validator = placementValidator;
            isInputBlocked = inputBlocked;
        }

        private void Update()
        {
            if (!ShouldProcessInput())
                return;

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                selectedCardController.Deselect();
                return;
            }

            if (!Mouse.current.leftButton.wasPressedThisFrame)
                return;

            if (!playFieldRaycaster.TryGetMouseWorldPoint(out Vector3 point))
                return;

            CardInstance card = selectedCardController.SelectedCard;

            if (!validator.IsValid(card, point))
                return;

            CardPlayContext playContext = new CardPlayContext(point);

            if (!handController.PlayCard(card, playerState, playContext))
                return;

            selectedCardController.Deselect();
        }

        private bool ShouldProcessInput()
        {
            return !IsInputBlocked() && selectedCardController != null && selectedCardController.HasSelection;
        }

        private bool IsInputBlocked()
        {
            return isInputBlocked?.Invoke() ?? false;
        }
    }
}
