using Cards;
using UnityEngine;
using UnityEngine.Serialization;

namespace Combat
{
    public class CombatSessionDriver : MonoBehaviour, IPlayerEffects
    {
        private static readonly float[] SimulationSpeedMultipliers = { 1f, 2f, 4f };

        [Header("Player Setup")]
        [SerializeField] private CombatSessionSetup defaultSetup = new();

        private PlayerState playerState;
        private BattleFlowController battleFlowController;
        private CombatSessionSetup sessionSetup;
        private CombatSessionSetup resolvedSessionSetup;
        private int currentSimulationSpeedIndex;
        private bool isPaused;

        public PlayerState PlayerState => playerState;
        public float CurrentSpeedMultiplier => SimulationSpeedMultipliers[currentSimulationSpeedIndex];
        public int ManualDrawCost => GetActiveSetup().ManualDrawCost;
        public int MaxHandSize => GetActiveSetup().MaxHandSize;
        public CombatSessionSetup ConfiguredSetup => GetConfiguredSetup().Clone();
        public CombatSessionSetup ResolvedSetup => GetActiveSetup().Clone();
        public bool IsPaused => isPaused;

        private void OnEnable()
        {
            ResetSimulationSpeed();
        }

        public void ConfigureSession(CombatSessionSetup setup)
        {
            sessionSetup = setup?.Clone();
            resolvedSessionSetup = null;
        }

        public void InitializeSession(HandController handController)
        {
            if (handController == null)
                return;

            CombatSessionSetup setup = GetActiveSetup().Clone();
            resolvedSessionSetup = setup.Clone();
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

        public void SetPaused(bool paused)
        {
            if (isPaused == paused)
                return;

            isPaused = paused;
            ApplySimulationSpeed();
        }

        private CombatSessionSetup GetConfiguredSetup()
        {
            defaultSetup ??= new CombatSessionSetup();
            return defaultSetup;
        }

        private CombatSessionSetup GetActiveSetup()
        {
            return resolvedSessionSetup ?? sessionSetup ?? GetConfiguredSetup();
        }

        private void ResetSimulationSpeed()
        {
            currentSimulationSpeedIndex = 0;
            isPaused = false;
            ApplySimulationSpeed();
        }

        private void ApplySimulationSpeed()
        {
            Time.timeScale = isPaused ? 0f : CurrentSpeedMultiplier;
        }
    }
}
