using Combat;
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

        public void Initialize(
            SelectedCardController selectedController,
            PlayFieldRaycaster raycaster,
            CardPlacementValidator placementValidator)
        {
            selectedCardController = selectedController;
            playFieldRaycaster = raycaster;
            validator = placementValidator;

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
            if (selectedCardController is null || !selectedCardController.HasSelection)
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
            if (card is null || card.Definition is null)
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
            float placementRadius = card.Definition.GetPlacementRadius();
            float effectRadius = card.Definition.GetEffectRadius();

            if (card.Type == CardType.Spell)
            {
                if (card.Definition.spawnableObject == null)
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
    }
}
