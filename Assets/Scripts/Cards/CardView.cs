using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cards
{
    public class CardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text manaCostText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button playButton;

        private CardInstance boundCard;
        private Action<CardInstance> onClicked;

        public CardInstance BoundCard => boundCard;

        public void Bind(CardInstance card, Action<CardInstance> clickCallback)
        {
            boundCard = card;
            onClicked = clickCallback;
            Refresh();

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
        }

        private void HandleClicked()
        {
            if (boundCard == null)
                return;
            
            onClicked?.Invoke(boundCard);
        }
    }
}