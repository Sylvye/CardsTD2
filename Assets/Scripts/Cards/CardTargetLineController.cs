using Combat;
using System;
using UnityEngine;

namespace Cards
{
    [RequireComponent(typeof(LineRenderer))]
    public class CardTargetLineController : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField, Min(0.01f)] private float lineWidth = 0.08f;
        [SerializeField] private Color lineColor = new(1f, 0.74f, 0.18f, 0.85f);
        [SerializeField] private int sortingOrder = 20;

        private CardView ownerCardView;
        private SelectedCardController selectedCardController;
        private PlayFieldRaycaster playFieldRaycaster;
        private Func<bool> isInputBlocked;

        public void Initialize(
            CardView owner,
            SelectedCardController selectedController,
            PlayFieldRaycaster raycaster,
            Func<bool> inputBlocked = null)
        {
            ownerCardView = owner;
            selectedCardController = selectedController;
            playFieldRaycaster = raycaster;
            isInputBlocked = inputBlocked;

            ConfigureRenderer();
            HideLine();
        }

        private void Awake()
        {
            ConfigureRenderer();
        }

        public void RefreshLine()
        {
            if (!ShouldShowLine())
            {
                HideLine();
                return;
            }

            if (!TryGetCardWorldPosition(out Vector3 cardWorldPosition) ||
                !playFieldRaycaster.TryGetMouseWorldPoint(out Vector3 mouseWorldPosition, requirePlayField: false))
            {
                HideLine();
                return;
            }

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, cardWorldPosition);
            lineRenderer.SetPosition(1, mouseWorldPosition);
        }

        public void HideLine()
        {
            if (lineRenderer != null)
                lineRenderer.enabled = false;

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private bool TryGetCardWorldPosition(out Vector3 worldPosition)
        {
            worldPosition = default;

            RectTransform cardRect = ownerCardView != null ? ownerCardView.transform as RectTransform : null;
            if (cardRect == null || playFieldRaycaster == null)
                return false;

            Canvas canvas = cardRect.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            Vector3 cardCenter = cardRect.TransformPoint(cardRect.rect.center);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, cardCenter);
            return playFieldRaycaster.TryScreenToWorldPoint(screenPoint, out worldPosition);
        }

        private bool ShouldShowLine()
        {
            return !IsInputBlocked() &&
                   selectedCardController != null &&
                   selectedCardController.HasSelection &&
                   ownerCardView != null &&
                   selectedCardController.SelectedCard == ownerCardView.BoundCard &&
                   playFieldRaycaster != null &&
                   ownerCardView.BoundCard != null;
        }

        private bool IsInputBlocked()
        {
            return isInputBlocked?.Invoke() ?? false;
        }

        private void ConfigureRenderer()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            if (lineRenderer == null)
                return;

            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.widthMultiplier = lineWidth;
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
            lineRenderer.sortingOrder = sortingOrder;
            lineRenderer.enabled = false;
        }
    }
}
