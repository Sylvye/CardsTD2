using System.Collections.Generic;
using UnityEngine;
using Combat;
using Towers;

namespace Cards
{
    public class HandViewDriver : MonoBehaviour
    {
        [Header("Deck Setup")]
        [SerializeField] private List<CardDef> startingDeck = new();
        [SerializeField] private int manualDrawCost = 2;

        [Header("Combat Setup")]
        [SerializeField] private CombatSessionDriver combatSessionDriver;
        [SerializeField] private TowerManager towerManager;
        [SerializeField] private PlayFieldRaycaster playFieldRaycaster;
        [SerializeField] private CardPreviewController cardPreviewController;
        [SerializeField] private FieldCardUseController fieldCardUseController;

        [Header("UI")]
        [SerializeField] private HandView handView;
        [SerializeField] private DrawPileView drawPileView;
        [SerializeField] private DiscardPileView discardPileView;
        [SerializeField] private BattleHUD battleHUD;

        private CombatCardState combatCardState;
        private HandController handController;
        private CardEffectResolver effectResolver;
        private SelectedCardController selectedCardController;
        private CardPlacementValidator cardPlacementValidator;

        private void Start()
        {
            combatCardState = new CombatCardState();
            combatCardState.BuildDrawPileFromDefs(startingDeck);

            handController = new HandController(combatCardState, 5);
            effectResolver = new CardEffectResolver(combatCardState, handController, towerManager);
            handController.SetEffectResolver(effectResolver);
            combatSessionDriver?.InitializeSession(handController);

            selectedCardController = new SelectedCardController();
            cardPlacementValidator = new CardPlacementValidator(towerManager);

            if (handView != null)
            {
                handView.Initialize(
                    combatCardState,
                    handController,
                    selectedCardController,
                    OnCardClicked
                );
            }

            if (drawPileView != null)
            {
                drawPileView.Initialize(
                    combatCardState,
                    handController,
                    () => manualDrawCost,
                    () => combatSessionDriver != null && combatSessionDriver.CanManuallyDraw(handController, manualDrawCost),
                    TryManualDraw
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
        
        public void TryManualDraw()
        {
            if (combatSessionDriver == null)
                return;

            combatSessionDriver.TryManualDraw(handController, manualDrawCost);
        }
        
        public void OnCardClicked(CardInstance card)
        {
            if (card == null)
                return;

            if (selectedCardController.HasSelection && selectedCardController.SelectedCard == card)
            {
                selectedCardController.Deselect();
                return;
            }
            
            selectedCardController.Select(card);
        }
        
    }
}
