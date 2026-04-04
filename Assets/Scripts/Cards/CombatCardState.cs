namespace Cards
{
    using System.Collections.Generic;
    using UnityEngine;

    public class CombatCardState
    {
        public CardZone DrawPile { get; private set; }
        public CardZone Hand { get; private set; }
        public CardZone DiscardPile { get; private set; }
        public CardZone ExhaustPile { get; private set; }

        private int nextRuntimeId = 1;

        public CombatCardState()
        {
            DrawPile = new CardZone("Draw Pile");
            Hand = new CardZone("Hand");
            DiscardPile = new CardZone("Discard Pile");
            ExhaustPile = new CardZone("Exhaust Pile");
        }

        public void BuildDrawPileFromDefs(IEnumerable<CardDef> cardDefs)
        {
            DrawPile.Clear();
            Hand.Clear();
            DiscardPile.Clear();
            ExhaustPile.Clear();

            foreach (CardDef def in cardDefs)
            {
                if (def == null)
                {
                    Debug.LogWarning("Null CardDef found while building draw pile.");
                    continue;
                }

                CardInstance instance = new CardInstance(def, nextRuntimeId++);
                DrawPile.Add(instance);
            }

            DrawPile.Shuffle();
        }

        public CardInstance DrawOne()
        {
            if (DrawPile.Count == 0)
            {
                ReshuffleDiscardIntoDraw();
            }

            if (DrawPile.Count == 0)
            {
                return null;
            }

            CardInstance drawn = DrawPile.DrawTop();
            Hand.Add(drawn);
            return drawn;
        }

        public List<CardInstance> DrawCards(int amount)
        {
            List<CardInstance> drawnCards = new();

            for (int i = 0; i < amount; i++)
            {
                CardInstance drawn = DrawOne();
                if (drawn == null)
                    break;

                drawnCards.Add(drawn);
            }

            return drawnCards;
        }

        public bool DiscardFromHand(CardInstance card)
        {
            if (!Hand.Remove(card))
                return false;

            DiscardPile.Add(card);
            return true;
        }

        public bool ExhaustFromHand(CardInstance card)
        {
            if (!Hand.Remove(card))
                return false;

            ExhaustPile.Add(card);
            return true;
        }

        public bool MoveToDiscard(CardInstance card)
        {
            if (card == null) return false;

            if (Hand.Remove(card) || DrawPile.Remove(card) || ExhaustPile.Remove(card))
            {
                DiscardPile.Add(card);
                return true;
            }

            return false;
        }

        public bool MoveToExhaust(CardInstance card)
        {
            if (card == null) return false;

            if (Hand.Remove(card) || DrawPile.Remove(card) || DiscardPile.Remove(card))
            {
                ExhaustPile.Add(card);
                return true;
            }

            return false;
        }

        public void DiscardEntireHand()
        {
            List<CardInstance> handCards = Hand.GetCardsCopy();
            foreach (CardInstance card in handCards)
            {
                Hand.Remove(card);
                DiscardPile.Add(card);
            }
        }

        public void ReshuffleDiscardIntoDraw()
        {
            List<CardInstance> discardCards = DiscardPile.GetCardsCopy();
            DiscardPile.Clear();

            foreach (CardInstance card in discardCards)
            {
                DrawPile.Add(card);
            }

            DrawPile.Shuffle();
        }
    }
}