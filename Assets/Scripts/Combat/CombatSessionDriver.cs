using Cards;
using UnityEngine;

namespace Combat
{
    public class CombatSessionDriver : MonoBehaviour
    {
        [Header("Player Setup")]
        [SerializeField] private int startingMana = 0;
        [SerializeField] private int maxMana = 20;
        [SerializeField] private float manaRegenPerSecond = 1f;
        [SerializeField] private int startingLives = 20;

        [Header("Battle Setup")]
        [SerializeField] private int openingHandSize = 5;

        private PlayerState playerState;
        private BattleFlowController battleFlowController;

        public PlayerState PlayerState => playerState;

        public void InitializeSession(HandController handController)
        {
            if (handController == null)
                return;

            playerState = new PlayerState(startingMana, maxMana, manaRegenPerSecond, startingLives);
            battleFlowController = new BattleFlowController(playerState, handController);
            battleFlowController.StartBattle(openingHandSize);
        }

        private void Update()
        {
            battleFlowController?.Update(Time.deltaTime);
        }

        public bool TryManualDraw(HandController handController, int drawCost)
        {
            if (handController == null || playerState == null)
                return false;

            return handController.TryManualDraw(playerState, drawCost);
        }

        public bool CanManuallyDraw(HandController handController, int drawCost)
        {
            if (handController == null || playerState == null)
                return false;

            return handController.CanManuallyDraw(playerState, drawCost);
        }

        public void LoseLives(int amount)
        {
            if (playerState == null || amount <= 0)
                return;

            playerState.LoseLives(amount);
            Debug.Log($"Player lost {amount} lives. Remaining: {playerState.Lives}");
        }

        public void GainMana(int amount)
        {
            if (playerState == null || amount <= 0)
                return;

            playerState.GainBurstMana(amount);
            Debug.Log($"Player gained {amount} mana. Current mana: {playerState.CurrentMana}");
        }
    }
}
