namespace Cards
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class CardZone
    {
        [SerializeField] private string zoneName;
        [SerializeField] private List<CardInstance> cards = new();

        public string ZoneName => zoneName;
        public IReadOnlyList<CardInstance> Cards => cards;
        public int Count => cards.Count;

        public CardZone(string zoneName)
        {
            this.zoneName = zoneName;
        }

        public void Add(CardInstance card)
        {
            if (card == null)
            {
                Debug.LogWarning($"Tried to add null card to zone '{zoneName}'.");
                return;
            }

            cards.Add(card);
        }

        public bool Remove(CardInstance card)
        {
            if (card == null) return false;
            return cards.Remove(card);
        }

        public CardInstance DrawTop()
        {
            if (cards.Count == 0)
                return null;

            int lastIndex = cards.Count - 1;
            CardInstance topCard = cards[lastIndex];
            cards.RemoveAt(lastIndex);
            return topCard;
        }

        public CardInstance PeekTop()
        {
            if (cards.Count == 0)
                return null;

            return cards[cards.Count - 1];
        }

        public void Clear()
        {
            cards.Clear();
        }

        public bool Contains(CardInstance card)
        {
            return cards.Contains(card);
        }

        public void Shuffle()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, cards.Count);
                (cards[i], cards[randomIndex]) = (cards[randomIndex], cards[i]);
            }
        }

        public List<CardInstance> GetCardsCopy()
        {
            return new List<CardInstance>(cards);
        }
    }
}