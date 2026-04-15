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
        [SerializeField] private bool autoInitializeOnStart = true;
        [SerializeField] private List<OwnedCard> startingDeck = new();
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
            if (autoInitializeOnStart)
                Initialize(startingDeck, manualDrawCost);
        }

        public void Initialize(IReadOnlyList<OwnedCard> deck, int drawCost, IPlayerEffects overridePlayerEffects = null)
        {
            if (handController != null)
                return;

            playerEffects = ResolvePlayerEffects(overridePlayerEffects);
            combatCardState = new CombatCardState();
            combatCardState.BuildDrawPileFromOwnedCards(deck ?? startingDeck);

            handController = new HandController(combatCardState, 5);
            effectResolver = new CardEffectResolver(combatCardState, handController, towerManager, enemyManager, playerEffects);
            handController.SetEffectResolver(effectResolver);

            handCardsPresenter = new HandCardsPresenter(handView);
            handCardsPresenter.Initialize(combatCardState, handController);

            handGameplayDriver?.Initialize(
                combatCardState,
                handController,
                handCardsPresenter.SelectedCardController,
                drawCost
            );
        }

        private IPlayerEffects ResolvePlayerEffects(IPlayerEffects overridePlayerEffects)
        {
            if (overridePlayerEffects != null)
                return overridePlayerEffects;

            IPlayerEffects resolvedEffects = playerEffectsSource as IPlayerEffects;
            if (playerEffectsSource != null && resolvedEffects == null)
            {
                Debug.LogError($"{nameof(HandViewDriver)} requires {nameof(playerEffectsSource)} to implement {nameof(IPlayerEffects)}.");
            }

            if (resolvedEffects == null)
                resolvedEffects = FindAnyObjectByType<CombatSessionDriver>();

            return resolvedEffects;
        }
    }
}
