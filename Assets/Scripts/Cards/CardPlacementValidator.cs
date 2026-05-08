using Towers;
using UnityEngine;

namespace Cards
{
    public class CardPlacementValidator
    {
        private readonly TowerManager towerManager;
        private readonly SupportManager supportManager;

        public CardPlacementValidator(TowerManager towerManager, SupportManager supportManager)
        {
            this.towerManager = towerManager;
            this.supportManager = supportManager;
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

                case CardType.Support:
                    if (card.ResolvedData == null || supportManager == null)
                        return false;

                    return card.ResolvedData.SupportCardMode switch
                    {
                        SupportCardMode.Spawnable => card.ResolvedData.SupportDefinition != null && supportManager.CanPlaceSupport(card, position),
                        SupportCardMode.Upgrade => supportManager.CanApplySupportUpgrade(card, position),
                        _ => false
                    };

                default:
                    return false;
            }
        }
    }
}
