using Cards;
using UnityEngine;
using UnityEngine.Serialization;

namespace Combat
{
    public class CombatSessionDriver : MonoBehaviour, IPlayerEffects
    {
        [Header("Player Setup")]
        [SerializeField] private int defaultStartingMana = 0;
        [SerializeField] private int defaultMaxMana = 20;
        [SerializeField] private float defaultManaRegenPerSecond = 1f;
        [FormerlySerializedAs("startingLives")]
        [SerializeField] private int defaultCurrentHealth = 20;
        [SerializeField] private int defaultMaxHealth = 20;
        [SerializeField] private int defaultOpeningHandSize = 5;

        private PlayerState playerState;
        private BattleFlowController battleFlowController;
        private CombatSessionSetup sessionSetup;

        public PlayerState PlayerState => playerState;

        public void ConfigureSession(CombatSessionSetup setup)
        {
            sessionSetup = setup;
        }

        public void InitializeSession(HandController handController)
        {
            if (handController == null)
                return;

            CombatSessionSetup setup = sessionSetup ?? BuildDefaultSetup();
            playerState = new PlayerState(
                setup.StartingMana,
                setup.MaxMana,
                setup.ManaRegenPerSecond,
                setup.CurrentHealth,
                setup.MaxHealth
            );

            battleFlowController = new BattleFlowController(playerState, handController);
            battleFlowController.StartBattle(setup.OpeningHandSize);
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

        public void LoseHealth(int amount)
        {
            if (playerState == null || amount <= 0)
                return;

            playerState.LoseHealth(amount);
            Debug.Log($"Player lost {amount} health. Remaining: {playerState.CurrentHealth}");
        }

        public void GainMana(int amount)
        {
            if (playerState == null || amount <= 0)
                return;

            playerState.GainBurstMana(amount);
            Debug.Log($"Player gained {amount} mana. Current mana: {playerState.CurrentMana}");
        }

        private CombatSessionSetup BuildDefaultSetup()
        {
            return new CombatSessionSetup
            {
                StartingMana = defaultStartingMana,
                MaxMana = defaultMaxMana,
                ManaRegenPerSecond = defaultManaRegenPerSecond,
                CurrentHealth = defaultCurrentHealth,
                MaxHealth = defaultMaxHealth,
                OpeningHandSize = defaultOpeningHandSize
            };
        }
    }
}
