using Towers;
using UnityEngine;

namespace Cards
{
    public class CardPlacementValidator
    {
        private readonly TowerManager towerManager;

        public CardPlacementValidator(TowerManager towerManager)
        {
            this.towerManager = towerManager;
        }

        public bool IsValid(CardInstance card, Vector3 position)
        {
            if (card == null || card.Definition is null)
                return false;

            switch (card.Type)
            {
                case CardType.Mod:
                    return true;

                case CardType.Spell:
                    return true;

                case CardType.Tower:
                    return towerManager != null && towerManager.CanPlaceTower(card, position);

                default:
                    return false;
            }
        }
    }
}
