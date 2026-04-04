using System.Collections.Generic;
using UnityEngine;
using Combat;

namespace Cards
{
    public class HandViewDriver : MonoBehaviour
    {
        [Header("Deck Setup")]
        [SerializeField] private List<CardDef> startingDeck = new();
        [SerializeField] private int openingHandSize = 5;

        [Header("Player Setup")]
        [SerializeField] private int startingMana = 0;
        [SerializeField] private int maxMana = 20;
        [SerializeField] private float manaRegenPerSecond = 1f;
        [SerializeField] private int startingLives = 20;

        [Header("UI")]
        [SerializeField] private HandView handView;
        [SerializeField] private BattleHUD battleHUD;

        private CombatCardState combatCardState;
        private HandController handController;
        private CardEffectResolver effectResolver;
        private PlayerState playerState;
        private BattleFlowController battleFlowController;

        private void Start()
        {
            combatCardState = new CombatCardState();
            combatCardState.BuildDrawPileFromDefs(startingDeck);

            playerState = new PlayerState(startingMana, maxMana, manaRegenPerSecond, startingLives);

            handController = new HandController(combatCardState, 5);
            effectResolver = new CardEffectResolver(combatCardState, handController);
            handController.SetEffectResolver(effectResolver);

            battleFlowController = new BattleFlowController(playerState, handController);

            if (handView != null)
            {
                handView.Initialize(
                    combatCardState,
                    handController,
                    () => playerState.CurrentMana,
                    TryPlayCard
                );
            }

            if (battleHUD != null)
            {
                battleHUD.Initialize(playerState, combatCardState);
            }

            battleFlowController.StartBattle(openingHandSize);
        }

        private void Update()
        {
            battleFlowController?.Update(Time.deltaTime);
        }

        public void TryPlayCard(CardInstance card)
        {
            handController.PlayCard(card, playerState);
        }
    }
}