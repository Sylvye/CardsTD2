using System.Collections.Generic;
using Combat;
using Enemies;
using Relics;
using UnityEngine;
using Towers;

namespace Cards
{
    public class HandViewDriver : MonoBehaviour
    {
        [Header("Deck Setup")]
        [SerializeField] private bool autoInitializeOnStart = true;
        [SerializeField] private List<OwnedCard> startingDeck = new();

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
                Initialize(startingDeck);
        }

        public void Initialize(IReadOnlyList<OwnedCard> deck, IPlayerEffects overridePlayerEffects = null, IReadOnlyList<OwnedRelic> activeRelics = null)
        {
            print("Initializing deck in HandViewDriver");
            if (handController != null)
                return;

            playerEffects = ResolvePlayerEffects(overridePlayerEffects);
            combatCardState = new CombatCardState();
            combatCardState.BuildDrawPileFromOwnedCards(deck ?? startingDeck, activeRelics);

            handController = new HandController(combatCardState, ResolveMaxHandSize(playerEffects));
            effectResolver = new CardEffectResolver(combatCardState, handController, towerManager, enemyManager, playerEffects);
            handController.SetEffectResolver(effectResolver);

            handCardsPresenter = new HandCardsPresenter(handView);
            handCardsPresenter.Initialize(combatCardState, handController);

            handGameplayDriver?.Initialize(
                combatCardState,
                handController,
                handCardsPresenter.SelectedCardController,
                handView
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

        private static int ResolveMaxHandSize(IPlayerEffects resolvedEffects)
        {
            return resolvedEffects is CombatSessionDriver combatSessionDriver
                ? combatSessionDriver.MaxHandSize
                : 5;
        }
    }
}
