using UnityEngine;
using Cards;

namespace Combat
{
    public class BattleFlowController
    {
        private readonly PlayerState playerState;
        private readonly HandController handController;

        public BattleFlowController(PlayerState playerState, HandController handController)
        {
            this.playerState = playerState;
            this.handController = handController;
        }

        public void StartBattle(int openingHandSize)
        {
            handController.DrawCards(openingHandSize);
        }

        public void Update(float deltaTime)
        {
            playerState.UpdateMana(deltaTime);
        }
    }
}