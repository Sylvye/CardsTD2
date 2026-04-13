using System.Collections.Generic;
using Combat;
using Enemies;
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
        [SerializeField] private EnemyManager enemyManager;
        [SerializeField] private MonoBehaviour playerEffectsSource;
        [SerializeField] private HandGameplayDriver handGameplayDriver;

        [Header("UI")]
        [SerializeField] private HandView handView;

        private CombatCardState combatCardState;
        private HandController handController;
        private CardEffectResolver effectResolver;
        private HandCardsPresenter handCardsPresenter;
        private IPlayerEffects playerEffects;

        private void Start()
        {
            playerEffects = playerEffectsSource as IPlayerEffects;
            if (playerEffectsSource != null && playerEffects == null)
            {
                Debug.LogError($"{nameof(HandViewDriver)} requires {nameof(playerEffectsSource)} to implement {nameof(IPlayerEffects)}.");
            }

            if (playerEffects == null)
                playerEffects = FindAnyObjectByType<CombatSessionDriver>();

            combatCardState = new CombatCardState();
            combatCardState.BuildDrawPileFromDefs(startingDeck);

            handController = new HandController(combatCardState, 5);
            effectResolver = new CardEffectResolver(combatCardState, handController, towerManager, enemyManager, playerEffects);
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
