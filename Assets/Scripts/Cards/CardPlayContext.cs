using UnityEngine;

namespace Cards
{
    public class CardPlayContext
    {
        public Vector3 WorldPosition { get; private set; }
        public bool HasWorldPosition { get; private set; }

        public CardPlayContext()
        {
            HasWorldPosition = false;
        }

        public CardPlayContext(Vector3 worldPosition)
        {
            WorldPosition = worldPosition;
            HasWorldPosition = true;
        }
    }
}