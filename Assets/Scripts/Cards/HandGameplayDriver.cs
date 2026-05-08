using Combat;
using Towers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Cards
{
    public class HandGameplayDriver : MonoBehaviour
    {
        [Header("Combat Setup")]
        [SerializeField] private CombatSessionDriver combatSessionDriver;
        [SerializeField] private TowerManager towerManager;
        [SerializeField] private SupportManager supportManager;
        [SerializeField] private PlayFieldRaycaster playFieldRaycaster;
        [SerializeField] private CardPreviewController cardPreviewController;
        [SerializeField] private FieldCardUseController fieldCardUseController;

        [Header("UI")]
        [SerializeField] private DrawPileView drawPileView;
        [SerializeField] private DiscardPileView discardPileView;
        [SerializeField] private BattleHUD battleHUD;

        private CardPlacementValidator cardPlacementValidator;
        private SelectedCardController selectedCardController;

        public void Initialize(
            CombatCardState combatCardState,
            HandController handController,
            SelectedCardController selectedCardController,
            HandView handView)
        {
            if (handController == null)
                return;

            if (this.selectedCardController != null)
                this.selectedCardController.OnSelectedCardChanged -= HandleSelectedCardChanged;

            this.selectedCardController = selectedCardController;
            if (this.selectedCardController != null)
                this.selectedCardController.OnSelectedCardChanged += HandleSelectedCardChanged;

            if (supportManager == null)
                supportManager = FindAnyObjectByType<SupportManager>();

            combatSessionDriver?.InitializeSession(handController);
            cardPlacementValidator = new CardPlacementValidator(towerManager, supportManager);

            if (drawPileView != null)
                drawPileView.Initialize(combatCardState, handController);

            if (discardPileView != null)
            {
                discardPileView.Initialize(combatCardState, handController);
            }

            if (battleHUD != null)
            {
                battleHUD.Initialize(
                    combatSessionDriver != null ? combatSessionDriver.PlayerState : null,
                    combatSessionDriver
                );
            }

            if (cardPreviewController != null)
            {
                cardPreviewController.Initialize(
                    selectedCardController,
                    playFieldRaycaster,
                    cardPlacementValidator,
                    supportManager,
                    IsGameplayInputBlocked
                );
            }

            handView?.ConfigureTargetLines(playFieldRaycaster, IsGameplayInputBlocked);

            if (fieldCardUseController != null)
            {
                fieldCardUseController.Initialize(
                    selectedCardController,
                    handController,
                    combatSessionDriver != null ? combatSessionDriver.PlayerState : null,
                    cardPlacementValidator,
                    IsGameplayInputBlocked
                );
            }
        }

        private void OnDestroy()
        {
            if (selectedCardController != null)
                selectedCardController.OnSelectedCardChanged -= HandleSelectedCardChanged;
        }

        private void Update()
        {
            if (battleHUD == null || selectedCardController == null || playFieldRaycaster == null)
                return;

            if (IsGameplayInputBlocked() || selectedCardController.HasSelection)
                return;

            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (!playFieldRaycaster.TryGetMouseWorldPoint(out Vector3 point))
                return;

            TowerAgent tower = FindTowerAtPoint(point);
            if (tower != null)
            {
                battleHUD.ShowTowerInspector(tower);
                cardPreviewController?.ShowTowerRange(tower);
                return;
            }

            battleHUD.HideTowerInspector();
            cardPreviewController?.HideTowerRange();
        }

        private bool IsGameplayInputBlocked()
        {
            return combatSessionDriver != null && combatSessionDriver.IsPaused;
        }

        private void HandleSelectedCardChanged(CardInstance selectedCard)
        {
            if (selectedCard != null)
            {
                battleHUD?.HideTowerInspector();
                cardPreviewController?.HideTowerRange();
            }
        }

        private static TowerAgent FindTowerAtPoint(Vector3 point)
        {
            Collider2D[] hits = Physics2D.OverlapPointAll(point);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null)
                    continue;

                TowerAgent tower = hit.GetComponentInParent<TowerAgent>();
                if (tower != null && !tower.IsDead)
                    return tower;
            }

            return null;
        }
    }
}
