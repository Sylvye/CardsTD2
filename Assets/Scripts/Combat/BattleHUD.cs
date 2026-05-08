using TMPro;
using RunFlow;
using Towers;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Combat
{
    public class BattleHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text manaText;
        [FormerlySerializedAs("livesText")]
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text resolvedSessionText;
        [SerializeField] private Button speedButton;
        [SerializeField] private TMP_Text speedButtonText;
        [Header("Tower Inspector")]
        [SerializeField] private CombatTowerInspectorView towerInspectorView;

        private PlayerState playerState;
        private CombatSessionDriver combatSessionDriver;
        private RunCoordinator coordinator;
        private TowerAgent inspectedTower;

        internal RectTransform TowerInspectorRoot => towerInspectorView != null ? towerInspectorView.Root : null;
        internal TMP_Text TowerNameLabel => towerInspectorView != null ? towerInspectorView.TowerNameText : null;
        internal TMP_Text AugmentEmptyLabel => towerInspectorView != null ? towerInspectorView.AugmentEmptyText : null;
        internal TMP_Text ActiveEffectsEmptyLabel => towerInspectorView != null ? towerInspectorView.ActiveEffectsEmptyText : null;
        internal TMP_Text PermanentModifiersEmptyLabel => towerInspectorView != null ? towerInspectorView.PermanentModifiersEmptyText : null;
        internal Button TargetingButtonControl => towerInspectorView != null ? towerInspectorView.TargetingButton : null;
        internal TMP_Text TargetingButtonLabel => towerInspectorView != null ? towerInspectorView.TargetingButtonText : null;

        private void Awake()
        {
            EnsureInspectorUi();
        }

        public void Initialize(PlayerState playerState, CombatSessionDriver sessionDriver)
        {
            this.playerState = playerState;
            combatSessionDriver = sessionDriver;
            SetCoordinator(GameFlowRoot.Instance != null ? GameFlowRoot.Instance.Coordinator : null);
            EnsureInspectorUi();

            if (speedButton != null)
            {
                speedButton.onClick.RemoveListener(HandleSpeedButtonClicked);
                speedButton.onClick.AddListener(HandleSpeedButtonClicked);
            }

            ApplyDebugVisibility();
            Refresh();
        }

        private void OnDestroy()
        {
            SetCoordinator(null);

            if (speedButton != null)
                speedButton.onClick.RemoveListener(HandleSpeedButtonClicked);
        }

        private void OnEnable()
        {
            ApplyDebugVisibility();
        }

        private void Update()
        {
            Refresh();
        }

        public void ShowTowerInspector(TowerAgent tower)
        {
            if (tower == null || tower.IsDead)
            {
                HideTowerInspector();
                return;
            }

            EnsureInspectorUi();
            inspectedTower = tower;
            RefreshTowerInspector();
        }

        public void HideTowerInspector()
        {
            inspectedTower = null;
            towerInspectorView?.Hide();
        }

        public void EnsureInspectorUi()
        {
            if (towerInspectorView != null)
            {
                if (towerInspectorView.TryGetMissingFieldReport(out string report))
                {
                    towerInspectorView.Initialize(HandleTargetingButtonClicked);
                    if (inspectedTower == null)
                        towerInspectorView.Hide();
                    return;
                }

                Debug.LogError(report, towerInspectorView);
                if (towerInspectorView.gameObject != null)
                    DestroyUnityObject(towerInspectorView.gameObject);

                towerInspectorView = null;
            }

            Transform parent = transform.parent != null ? transform.parent : transform;
            CleanupLegacyInspectorObjects(parent);

            CombatTowerInspectorView prefab = Resources.Load<CombatTowerInspectorView>(CombatTowerInspectorView.ResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"Missing TowerInspector prefab at Resources/{CombatTowerInspectorView.ResourcePath}.");
                return;
            }

            if (!prefab.TryGetMissingFieldReport(out string missingFieldReport))
            {
                Debug.LogError(missingFieldReport, prefab);
                return;
            }

            towerInspectorView = Instantiate(prefab, parent, false);
            towerInspectorView.name = "TowerInspector";
            towerInspectorView.Initialize(HandleTargetingButtonClicked);

            if (inspectedTower == null)
                towerInspectorView.Hide();
        }

        private static void CleanupLegacyInspectorObjects(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child == null || child.name != "TowerInspector")
                    continue;

                if (child.GetComponent<CombatTowerInspectorView>() != null)
                    continue;

                DestroyUnityObject(child.gameObject);
            }
        }

        private static void DestroyUnityObject(Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        private void HandleSpeedButtonClicked()
        {
            combatSessionDriver?.CycleSimulationSpeed();
            Refresh();
        }

        private void HandleTargetingButtonClicked()
        {
            if (inspectedTower == null || inspectedTower.IsDead)
                return;

            inspectedTower.CycleTargetPriority();
            RefreshTowerInspector();
        }

        private void Refresh()
        {
            if (manaText is not null && playerState != null)
                manaText.text = $"Mana: {playerState.CurrentMana}";

            if (healthText is not null && playerState != null)
                healthText.text = $"Health: {playerState.CurrentHealth}/{playerState.MaxHealth}";

            if (resolvedSessionText is not null)
            {
                bool isDebugUiEnabled = IsDebugUiEnabled();
                resolvedSessionText.gameObject.SetActive(isDebugUiEnabled);

                if (isDebugUiEnabled)
                    resolvedSessionText.text = BuildResolvedSessionText();
            }

            if (speedButtonText is not null)
            {
                float speedMultiplier = combatSessionDriver != null ? combatSessionDriver.CurrentSpeedMultiplier : 1f;
                speedButtonText.text = $"Speed: {speedMultiplier:0.#}x";
            }

            RefreshTowerInspector();
        }

        private void RefreshTowerInspector()
        {
            if (towerInspectorView == null)
                return;

            if (inspectedTower == null || inspectedTower.IsDead)
            {
                towerInspectorView.Hide();
                return;
            }

            towerInspectorView.Show(inspectedTower);
        }

        private string BuildResolvedSessionText()
        {
            CombatSessionSetup setup = combatSessionDriver != null ? combatSessionDriver.ResolvedSetup : null;
            if (setup == null)
                return "Resolved Session: Unavailable";

            int currentHealth = playerState != null ? playerState.CurrentHealth : setup.CurrentHealth;
            int maxHealth = playerState != null ? playerState.MaxHealth : setup.MaxHealth;

            return
                "Resolved Session\n" +
                $"Starting Mana: {setup.StartingMana}\n" +
                $"Max Mana: {setup.MaxMana}\n" +
                $"Mana Regen: {setup.ManaRegenPerSecond:0.##}/s\n" +
                $"Health: {currentHealth}/{maxHealth}\n" +
                $"Opening Hand: {setup.OpeningHandSize}\n" +
                $"Max Hand: {setup.MaxHandSize}";
        }

        private void HandleDebugUiChanged(bool enabled)
        {
            ApplyDebugVisibility();
        }

        private void ApplyDebugVisibility()
        {
            if (resolvedSessionText != null)
                resolvedSessionText.gameObject.SetActive(IsDebugUiEnabled());
        }

        private bool IsDebugUiEnabled()
        {
            return coordinator != null && coordinator.IsDebugUiEnabled;
        }

        private void SetCoordinator(RunCoordinator newCoordinator)
        {
            if (coordinator == newCoordinator)
                return;

            if (coordinator != null)
                coordinator.DebugUiChanged -= HandleDebugUiChanged;

            coordinator = newCoordinator;

            if (coordinator != null)
                coordinator.DebugUiChanged += HandleDebugUiChanged;
        }
    }
}
