using System;
using Combat;
using UnityEngine;

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

        public bool PlayCard(CardInstance card, PlayerState playerState, CardPlayContext playContext)
        {
            if (!CanPlay(card, playerState))
                return false;

            if (!playerState.SpendMana(card.CurrentManaCost))
                return false;

            cardState.Hand.Remove(card);

            Debug.Log($"Played card: {card}");

            effectResolver?.ResolveOnPlay(card, playerState, playContext);

            cardState.DiscardPile.Add(card);

            OnHandChanged?.Invoke();
            return true;
        }

        public bool CanManuallyDraw(PlayerState playerState, int drawCost)
        {
            if (playerState == null)
                return false;

            if (cardState.Hand.Count >= maxHandSize)
                return false;

            bool hasDrawableCards = cardState.DrawPile.Count > 0 || cardState.DiscardPile.Count > 0;
            if (!hasDrawableCards)
                return false;

            return playerState.CurrentMana >= drawCost;
        }

        public bool TryManualDraw(PlayerState playerState, int drawCost)
        {
            if (!CanManuallyDraw(playerState, drawCost))
                return false;

            if (!playerState.SpendMana(drawCost))
                return false;

            CardInstance drawn = cardState.DrawOne();

            if (drawn == null)
            {
                Debug.LogWarning("Manual draw failed: no card could be drawn.");
                return false;
            }

            Debug.Log($"Manually drew card: {drawn} for {drawCost} mana.");
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