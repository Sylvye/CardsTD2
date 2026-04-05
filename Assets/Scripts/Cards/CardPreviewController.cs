using UnityEngine;
using Combat;

namespace Cards
{
    public class CardPreviewController : MonoBehaviour
    {
        [SerializeField] private GameObject primaryRadiusVisual;
        [SerializeField] private GameObject secondaryRadiusVisual;

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

            selectedCardController.OnSelectedCardChanged += HandleSelectedCardChanged;
            HideAll();
        }

        private void OnDestroy()
        {
            if (selectedCardController != null)
                selectedCardController.OnSelectedCardChanged -= HandleSelectedCardChanged;
        }

        private void Update()
        {
            if (selectedCardController == null || !selectedCardController.HasSelection)
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
            if (selectedCard == null)
                HideAll();
        }

        private void UpdatePreview(CardInstance card, Vector3 point, bool isValid)
        {
            if (card == null || card.Definition == null)
            {
                HideAll();
                return;
            }

            if (card.Type == CardType.Mod)
            {
                HideAll();
                return;
            }

            SpawnableObjectDef spawnable = card.Definition.spawnableObject;
            if (spawnable == null)
            {
                HideAll();
                return;
            }

            Color color = isValid ? Color.green : Color.red;

            if (card.Type == CardType.Spell)
            {
                ShowRing(primaryRadiusVisual, point, spawnable.effectRadius, color);
                HideRing(secondaryRadiusVisual);
                return;
            }

            if (card.Type == CardType.Tower)
            {
                ShowRing(primaryRadiusVisual, point, spawnable.placementRadius, color);
                ShowRing(secondaryRadiusVisual, point, spawnable.effectRadius, color);
                return;
            }

            HideAll();
        }

        private void ShowRing(GameObject ring, Vector3 point, float radius, Color color)
        {
            if (ring is null)
                return;

            ring.SetActive(true);
            ring.transform.position = point;
            ring.transform.localScale = new Vector3(radius * 2f, 1f, radius * 2f);

            Renderer renderer = ring.GetComponent<Renderer>();
            if (renderer is not null && renderer.material is not null)
                renderer.material.color = color;
        }

        private void HideRing(GameObject ring)
        {
            if (ring is not null)
                ring.SetActive(false);
        }

        private void HideAll()
        {
            HideRing(primaryRadiusVisual);
            HideRing(secondaryRadiusVisual);
        }
    }
}