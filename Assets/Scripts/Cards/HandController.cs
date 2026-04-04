using System;
using UnityEngine;
using Combat;

namespace Cards
{
    public class HandController
    {
        private readonly CombatCardState cardState;
        private CardEffectResolver effectResolver;
        private readonly int maxHandSize;

        public int MaxHandSize => maxHandSize;

        public event Action OnHandChanged;

        public HandController(CombatCardState cardState, int maxHandSize = 5)
        {
            this.cardState = cardState;
            this.maxHandSize = maxHandSize;
        }

        public void SetEffectResolver(CardEffectResolver resolver)
        {
            effectResolver = resolver;
        }

        public void DrawCards(int amount)
        {
            bool changed = false;

            for (int i = 0; i < amount; i++)
            {
                if (cardState.Hand.Count >= maxHandSize)
                    break;

                CardInstance drawn = cardState.DrawOne();
                if (drawn == null)
                    break;

                Debug.Log($"Drew card: {drawn}");
                changed = true;
            }

            if (changed)
                OnHandChanged?.Invoke();
        }

        public bool CanPlay(CardInstance card, PlayerState playerState)
        {
            if (card == null || playerState == null)
                return false;

            if (!cardState.Hand.Contains(card))
                return false;

            return card.CurrentManaCost <= playerState.CurrentMana;
        }

        public bool PlayCard(CardInstance card, PlayerState playerState)
        {
            if (!CanPlay(card, playerState))
                return false;

            if (!playerState.SpendMana(card.CurrentManaCost))
                return false;

            cardState.Hand.Remove(card);

            Debug.Log($"Played card: {card}");

            effectResolver?.ResolveOnPlay(card, playerState);

            cardState.DiscardPile.Add(card);

            OnHandChanged?.Invoke();
            return true;
        }

        public void DiscardHand()
        {
            cardState.DiscardEntireHand();
            OnHandChanged?.Invoke();
        }
    }
}