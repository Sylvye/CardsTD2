using System.Collections.Generic;
using UnityEngine;
using Towers;

namespace Cards
{
    public class HandViewDriver : MonoBehaviour
    {
        [Header("Deck Setup")]
        [SerializeField] private List<CardDef> startingDeck = new();
        [SerializeField] private int manualDrawCost = 2;

        [Header("Gameplay")]
        [SerializeField] private TowerManager towerManager;
        [SerializeField] private HandGameplayDriver handGameplayDriver;

        [Header("UI")]
        [SerializeField] private HandView handView;

        private CombatCardState combatCardState;
        private HandController handController;
        private CardEffectResolver effectResolver;
        private HandCardsPresenter handCardsPresenter;

        private void Start()
        {
            combatCardState = new CombatCardState();
            combatCardState.BuildDrawPileFromDefs(startingDeck);

            handController = new HandController(combatCardState, 5);
            effectResolver = new CardEffectResolver(combatCardState, handController, towerManager);
            handController.SetEffectResolver(effectResolver);

            handCardsPresenter = new HandCardsPresenter(handView);
            handCardsPresenter.Initialize(combatCardState, handController);

            handGameplayDriver?.Initialize(
                combatCardState,
                handController,
                handCardsPresenter.SelectedCardController,
                manualDrawCost
            );
        }
    }
}
