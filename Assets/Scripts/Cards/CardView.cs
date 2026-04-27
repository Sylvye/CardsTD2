using System;
using System.Collections.Generic;
using Combat;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cards
{
    public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {   
        [Header("UI")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text manaCostText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Image iconImage;
        [SerializeField] private RectTransform augmentBadgeContainer;
        [SerializeField] private Image augmentBadgeTemplate;
        [SerializeField] private Button playButton;
        [SerializeField] private CardTargetLineController targetLineController;
        
        [Header("Motion")]
        [SerializeField] private float vertOffset = 0f;
        [SerializeField] private float hoverRaiseAmount = 30f;
        [SerializeField] private float moveSpeed = 12f;
        [SerializeField] private float augmentBadgeVerticalSpacing = 6f;

        private CardInstance boundCard;
        private Action<CardInstance> onClicked;
        private RectTransform rectTransform;
        private Vector2 baseLocalPosition;
        private bool isHovered;
        private bool isSelected;
        private readonly List<Image> augmentBadgePool = new();

        public CardInstance BoundCard => boundCard;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            baseLocalPosition = rectTransform.localPosition;

            if (augmentBadgeTemplate != null && !augmentBadgePool.Contains(augmentBadgeTemplate))
                augmentBadgePool.Add(augmentBadgeTemplate);

            if (targetLineController == null)
                targetLineController = GetComponentInChildren<CardTargetLineController>(true);
        }

        private void Update()
        {
            float targetY = vertOffset;

            if (isSelected)
                targetY += hoverRaiseAmount;
            else if (isHovered)
                targetY += hoverRaiseAmount * 0.8f;

            Vector2 current = rectTransform.localPosition;
            float newY = Mathf.Lerp(current.y, baseLocalPosition.y + targetY, Time.deltaTime * moveSpeed);
            rectTransform.localPosition = new Vector2(current.x, newY);

            targetLineController?.RefreshLine();
        }

        public void Bind(CardInstance card, Action<CardInstance> clickCallback)
        {
            boundCard = card;
            onClicked = clickCallback;
            
            isHovered = false;
            isSelected = false;

            if (rectTransform is null)
                rectTransform = GetComponent<RectTransform>();
            
            Refresh();
            targetLineController?.HideLine();

            if (playButton is not null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(HandleClicked);
            }
            
        }

        public void Refresh()
        {
            if (boundCard == null)
            {
                if (nameText is not null) nameText.text = "NULL";
                if (manaCostText is not null) manaCostText.text = "-";
                if (descriptionText is not null) descriptionText.text = "";
                if (iconImage is not null) iconImage.sprite = null;
                HideAllAugmentBadges();
                return;
            }

            if (nameText is not null)
                nameText.text = boundCard.DisplayName;

            if (manaCostText is not null)
                manaCostText.text = boundCard.CurrentManaCost.ToString();

            if (descriptionText is not null)
                descriptionText.text = boundCard.Description;
            
            if  (iconImage is not null)
                iconImage.sprite = boundCard.Icon;

            RefreshAugmentBadges();
        }
        
        public void SetSelected(bool selected)
        {
            isSelected = selected;
        }

        public void ConfigureTargetLine(
            SelectedCardController selectedCardController,
            PlayFieldRaycaster playFieldRaycaster,
            Func<bool> inputBlocked)
        {
            if (targetLineController == null)
                targetLineController = GetComponentInChildren<CardTargetLineController>(true);

            if (targetLineController == null)
                return;

            targetLineController.Initialize(this, selectedCardController, playFieldRaycaster, inputBlocked);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
        }

        private void HandleClicked()
        {
            if (boundCard == null)
                return;
            
            onClicked?.Invoke(boundCard);
        }

        private void RefreshAugmentBadges()
        {
            if (augmentBadgeTemplate is null)
                return;

            HideAllAugmentBadges();

            IReadOnlyList<Sprite> augmentIcons = boundCard != null ? boundCard.AugmentIcons : null;
            if (augmentIcons == null)
                return;

            for (int i = 0; i < augmentIcons.Count; i++)
            {
                Sprite icon = augmentIcons[i];
                if (icon == null)
                    continue;

                Image badge = GetOrCreateAugmentBadge(i);
                badge.sprite = icon;
                badge.enabled = true;
                badge.gameObject.SetActive(true);
                PositionBadge(badge.rectTransform, i);
            }
        }

        private Image GetOrCreateAugmentBadge(int index)
        {
            while (augmentBadgePool.Count <= index)
            {
                Image badge = augmentBadgePool.Count == 0
                    ? augmentBadgeTemplate
                    : Instantiate(augmentBadgeTemplate, augmentBadgeTemplate.transform.parent);

                badge.name = $"AugmentBadge{augmentBadgePool.Count}";
                badge.raycastTarget = false;
                augmentBadgePool.Add(badge);
            }

            return augmentBadgePool[index];
        }

        private void HideAllAugmentBadges()
        {
            if (augmentBadgeTemplate != null && !augmentBadgePool.Contains(augmentBadgeTemplate))
                augmentBadgePool.Add(augmentBadgeTemplate);

            for (int i = 0; i < augmentBadgePool.Count; i++)
            {
                if (augmentBadgePool[i] == null)
                    continue;

                augmentBadgePool[i].gameObject.SetActive(false);
                augmentBadgePool[i].sprite = null;
            }

            if (augmentBadgeContainer != null)
                augmentBadgeContainer.gameObject.SetActive(true);
        }

        private void PositionBadge(RectTransform badgeRect, int index)
        {
            if (badgeRect == null)
                return;

            float badgeHeight = badgeRect.sizeDelta.y;
            badgeRect.anchoredPosition = new Vector2(0f, -(badgeHeight + augmentBadgeVerticalSpacing) * index);
        }
    }
}
