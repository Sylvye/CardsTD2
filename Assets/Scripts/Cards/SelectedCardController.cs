using System;
using Combat;
using UnityEngine;

namespace Cards
{
    public class SelectedCardController
    {
        public CardInstance SelectedCard { get; private set; }

        public bool HasSelection => SelectedCard != null;

        public event Action<CardInstance> OnSelectedCardChanged;

        public void Select(CardInstance card)
        {
            if (SelectedCard == card)
                return;

            SelectedCard = card;
            OnSelectedCardChanged?.Invoke(SelectedCard);
        }

        public void Deselect()
        {
            if (SelectedCard == null)
                return;

            SelectedCard = null;
            OnSelectedCardChanged?.Invoke(null);
        }
    }
}