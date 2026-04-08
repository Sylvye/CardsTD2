using Combat;
using Towers;
using UnityEngine;

namespace Cards
{
    public class HandGameplayDriver : MonoBehaviour
    {
        [Header("Combat Setup")]
        [SerializeField] private CombatSessionDriver combatSessionDriver;
        [SerializeField] private TowerManager towerManager;
        [SerializeField] private PlayFieldRaycaster playFieldRaycaster;
        [SerializeField] private CardPreviewController cardPreviewController;
        [SerializeField] private FieldCardUseController fieldCardUseController;

        [Header("UI")]
        [SerializeField] private DrawPileView drawPileView;
        [SerializeField] private DiscardPileView discardPileView;
        [SerializeField] private BattleHUD battleHUD;

        private CardPlacementValidator cardPlacementValidator;

        public void Initialize(
            CombatCardState combatCardState,
            HandController handController,
            SelectedCardController selectedCardController,
            int manualDrawCost)
        {
            if (handController == null)
                return;

            combatSessionDriver?.InitializeSession(handController);
            cardPlacementValidator = new CardPlacementValidator(towerManager);

            if (drawPileView != null)
            {
                drawPileView.Initialize(
                    combatCardState,
                    handController,
                    () => manualDrawCost,
                    () => combatSessionDriver != null && combatSessionDriver.CanManuallyDraw(handController, manualDrawCost),
                    () => TryManualDraw(handController, manualDrawCost)
                );
            }

            if (discardPileView != null)
            {
                discardPileView.Initialize(combatCardState, handController);
            }

            if (battleHUD != null)
            {
                battleHUD.Initialize(combatSessionDriver != null ? combatSessionDriver.PlayerState : null);
            }

            if (cardPreviewController != null)
            {
                cardPreviewController.Initialize(
                    selectedCardController,
                    playFieldRaycaster,
                    cardPlacementValidator
                );
            }

            if (fieldCardUseController != null)
            {
                fieldCardUseController.Initialize(
                    selectedCardController,
                    handController,
                    combatSessionDriver != null ? combatSessionDriver.PlayerState : null,
                    cardPlacementValidator
                );
            }
        }

        private void TryManualDraw(HandController handController, int drawCost)
        {
            if (combatSessionDriver == null)
                return;

            combatSessionDriver.TryManualDraw(handController, drawCost);
        }
    }
}
