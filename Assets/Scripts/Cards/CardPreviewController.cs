using Combat;
using System;
using UnityEngine;

namespace Cards
{
    public class CardPreviewController : MonoBehaviour
    {
        [SerializeField] private GameObject primaryRadiusVisual;
        [SerializeField] private GameObject secondaryRadiusVisual;
        [SerializeField] private Color invalidColor;
        [SerializeField] private Color validColor;

        private SelectedCardController selectedCardController;
        private PlayFieldRaycaster playFieldRaycaster;
        private CardPlacementValidator validator;
        private Func<bool> isInputBlocked;

        public void Initialize(
            SelectedCardController selectedController,
            PlayFieldRaycaster raycaster,
            CardPlacementValidator placementValidator,
            Func<bool> inputBlocked = null)
        {
            selectedCardController = selectedController;
            playFieldRaycaster = raycaster;
            validator = placementValidator;
            isInputBlocked = inputBlocked;

            if (selectedCardController is not null)
                selectedCardController.OnSelectedCardChanged += HandleSelectedCardChanged;

            HideAll();
        }

        private void OnDestroy()
        {
            if (selectedCardController is not null)
                selectedCardController.OnSelectedCardChanged -= HandleSelectedCardChanged;
        }

        private void Update()
        {
            if (!ShouldShowPreview())
            {
                HideAll();
                return;
            }

            CardInstance card = selectedCardController.SelectedCard;

            if (!playFieldRaycaster.TryGetMouseWorldPoint(out Vector3 point))
            {
                HideAll();
                return;
            }

            bool isValid = validator.IsValid(card, point);
            UpdatePreview(card, point, isValid);
        }

        private void HandleSelectedCardChanged(CardInstance selectedCard)
        {
            if (selectedCard is null)
                HideAll();
        }

        private void UpdatePreview(CardInstance card, Vector3 point, bool isValid)
        {
            if (card is null || card.ResolvedData is null)
            {
                HideAll();
                return;
            }

            if (card.Type == CardType.Mod)
            {
                HideAll();
                return;
            }

            Color color = isValid ? validColor : invalidColor;
            float placementRadius = card.Definition != null ? card.Definition.GetPlacementRadius() : -1f;
            float effectRadius = GetEffectRadius(card);

            if (card.Type == CardType.Spell)
            {
                if (card.ResolvedData.SpawnableObject == null)
                {
                    HideAll();
                    return;
                }

                ShowCircle(secondaryRadiusVisual, point, effectRadius, color);
                return;
            }

            if (card.Type == CardType.Tower)
            {
                if (placementRadius < 0f)
                {
                    HideAll();
                    return;
                }

                ShowCircle(primaryRadiusVisual, point, placementRadius, color);
                ShowCircle(secondaryRadiusVisual, point, effectRadius, color);
                return;
            }

            HideAll();
        }

        private void ShowCircle(GameObject circle, Vector3 point, float radius, Color color)
        {
            if (circle is null)
                return;

            circle.SetActive(true);
            circle.transform.position = new Vector3(point.x, point.y, circle.transform.position.z);
            circle.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

            SpriteRenderer spriteRenderer = circle.GetComponent<SpriteRenderer>();
            if (spriteRenderer is not null)
                spriteRenderer.color = color;
        }

        private void HideCircle(GameObject circle)
        {
            if (circle is not null)
                circle.SetActive(false);
        }

        private void HideAll()
        {
            HideCircle(primaryRadiusVisual);
            HideCircle(secondaryRadiusVisual);
        }

        private float GetEffectRadius(CardInstance card)
        {
            if (card == null || card.ResolvedData == null)
                return 0f;

            if (card.ResolvedData.TowerDefinition != null)
                return card.ResolvedData.TowerDefinition.baseStats.range;

            if (card.Type == CardType.Spell)
                return SpawnableColliderUtility.GetPreviewRadius(card.ResolvedData.SpawnableObject);

            return card.ResolvedData.SpawnableObject != null ? card.ResolvedData.SpawnableObject.effectRadius : 0f;
        }

        private bool ShouldShowPreview()
        {
            return !IsInputBlocked() && selectedCardController is not null && selectedCardController.HasSelection;
        }

        private bool IsInputBlocked()
        {
            return isInputBlocked?.Invoke() ?? false;
        }
    }
}
