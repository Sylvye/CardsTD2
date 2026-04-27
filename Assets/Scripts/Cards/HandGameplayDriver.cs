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
            HandView handView)
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
                    GetManualDrawCost,
                    () => combatSessionDriver != null && combatSessionDriver.CanManuallyDraw(handController, GetManualDrawCost()),
                    () => TryManualDraw(handController)
                );
            }

            if (discardPileView != null)
            {
                discardPileView.Initialize(combatCardState, handController);
            }

            if (battleHUD != null)
            {
                battleHUD.Initialize(
                    combatSessionDriver != null ? combatSessionDriver.PlayerState : null,
                    combatSessionDriver
                );
            }

            if (cardPreviewController != null)
            {
                cardPreviewController.Initialize(
                    selectedCardController,
                    playFieldRaycaster,
                    cardPlacementValidator,
                    IsGameplayInputBlocked
                );
            }

            handView?.ConfigureTargetLines(playFieldRaycaster, IsGameplayInputBlocked);

            if (fieldCardUseController != null)
            {
                fieldCardUseController.Initialize(
                    selectedCardController,
                    handController,
                    combatSessionDriver != null ? combatSessionDriver.PlayerState : null,
                    cardPlacementValidator,
                    IsGameplayInputBlocked
                );
            }
        }

        private int GetManualDrawCost()
        {
            return combatSessionDriver != null ? combatSessionDriver.ManualDrawCost : 0;
        }

        private void TryManualDraw(HandController handController)
        {
            if (combatSessionDriver == null)
                return;

            combatSessionDriver.TryManualDraw(handController, GetManualDrawCost());
        }

        private bool IsGameplayInputBlocked()
        {
            return combatSessionDriver != null && combatSessionDriver.IsPaused;
        }
    }
}
