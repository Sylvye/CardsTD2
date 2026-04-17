using Cards;
using UnityEngine;
using UnityEngine.Serialization;

namespace Combat
{
    public class CombatSessionDriver : MonoBehaviour, IPlayerEffects
    {
        private static readonly float[] SimulationSpeedMultipliers = { 1f, 2f, 4f };

        [Header("Player Setup")]
        [SerializeField] private int defaultStartingMana = 0;
        [SerializeField] private int defaultMaxMana = 20;
        [SerializeField] private float defaultManaRegenPerSecond = 1f;
        [FormerlySerializedAs("startingLives")]
        [SerializeField] private int defaultCurrentHealth = 20;
        [SerializeField] private int defaultMaxHealth = 20;
        [SerializeField] private int defaultOpeningHandSize = 5;
        [SerializeField] private int defaultManualDrawCost = 2;

        private PlayerState playerState;
        private BattleFlowController battleFlowController;
        private CombatSessionSetup sessionSetup;
        private int currentSimulationSpeedIndex;

        public PlayerState PlayerState => playerState;
        public float CurrentSpeedMultiplier => SimulationSpeedMultipliers[currentSimulationSpeedIndex];
        public int ManualDrawCost => sessionSetup?.ManualDrawCost ?? defaultManualDrawCost;

        private void OnEnable()
        {
            ResetSimulationSpeed();
        }

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

            ResetSimulationSpeed();
            battleFlowController = new BattleFlowController(playerState, handController);
            battleFlowController.StartBattle(setup.OpeningHandSize);
        }

        private void FixedUpdate()
        {
            battleFlowController?.FixedTick(Time.fixedDeltaTime);
        }

        private void OnDisable()
        {
            ResetSimulationSpeed();
        }

        private void OnDestroy()
        {
            ResetSimulationSpeed();
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

        public void CycleSimulationSpeed()
        {
            currentSimulationSpeedIndex = (currentSimulationSpeedIndex + 1) % SimulationSpeedMultipliers.Length;
            ApplySimulationSpeed();
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
                OpeningHandSize = defaultOpeningHandSize,
                ManualDrawCost = defaultManualDrawCost
            };
        }

        private void ResetSimulationSpeed()
        {
            currentSimulationSpeedIndex = 0;
            ApplySimulationSpeed();
        }

        private void ApplySimulationSpeed()
        {
            Time.timeScale = CurrentSpeedMultiplier;
        }
    }
}
