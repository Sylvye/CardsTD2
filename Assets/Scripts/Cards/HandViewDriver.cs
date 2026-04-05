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
        [SerializeField] private int openingHandSize = 5;
        [SerializeField] private int manualDrawCost = 2;

        [Header("Player Setup")]
        [SerializeField] private int startingMana = 0;
        [SerializeField] private int maxMana = 20;
        [SerializeField] private float manaRegenPerSecond = 1f;
        [SerializeField] private int startingLives = 20;
        
        [Header("Combat Setup")]
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
        private PlayerState playerState;
        private BattleFlowController battleFlowController;
        private SelectedCardController selectedCardController;
        private CardPlacementValidator cardPlacementValidator;

        public int CurrentMana => playerState != null ? playerState.CurrentMana : 0;

        private void Start()
        {
            combatCardState = new CombatCardState();
            combatCardState.BuildDrawPileFromDefs(startingDeck);

            playerState = new PlayerState(startingMana, maxMana, manaRegenPerSecond, startingLives);

            handController = new HandController(combatCardState, 5);
            effectResolver = new CardEffectResolver(combatCardState, handController, towerManager);
            handController.SetEffectResolver(effectResolver);

            battleFlowController = new BattleFlowController(playerState, handController);
            selectedCardController = new SelectedCardController();
            cardPlacementValidator = new CardPlacementValidator(towerManager);

            if (handView != null)
            {
                handView.Initialize(
                    combatCardState,
                    handController,
                    () => playerState.CurrentMana,
                    OnCardClicked
                );
            }

            if (drawPileView != null)
            {
                drawPileView.Initialize(
                    combatCardState,
                    handController,
                    () => manualDrawCost,
                    () => handController.CanManuallyDraw(playerState, manualDrawCost),
                    TryManualDraw
                );
            }

            if (discardPileView != null)
            {
                discardPileView.Initialize(combatCardState, handController);
            }

            if (battleHUD != null)
            {
                battleHUD.Initialize(playerState);
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
                    playerState,
                    cardPlacementValidator
                );
            }

            battleFlowController.StartBattle(openingHandSize);

            Debug.Log($"Starting mana: {playerState.CurrentMana}");
        }

        private void Update()
        {
            battleFlowController?.Update(Time.deltaTime);
        }
        
        // OUTDATED, COULD POSSIBLY BE USED FOR NON-TARGETED CARDS THOUGH
        // public void TryPlayCard(CardInstance card)
        // {
        //     if (card == null || playerState == null)
        //         return;
        //
        //     bool played = handController.PlayCard(card, playerState);
        //
        //     if (played)
        //         Debug.Log($"Mana after play: {playerState.CurrentMana}");
        // }

        public void TryManualDraw()
        {
            if (playerState == null)
                return;

            bool drew = handController.TryManualDraw(playerState, manualDrawCost);

            if (drew)
                Debug.Log($"Mana after manual draw: {playerState.CurrentMana}");
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