using Combat;
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

        public void Initialize(
            SelectedCardController selectedController,
            HandController handController,
            PlayerState playerState,
            CardPlacementValidator placementValidator)
        {
            selectedCardController = selectedController;
            this.handController = handController;
            this.playerState = playerState;
            validator = placementValidator;
        }

        private void Update()
        {
            if (selectedCardController == null || !selectedCardController.HasSelection)
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
    }
}