using System.Collections.Generic;
using RunFlow;
using UnityEngine;

namespace Relics
{
    [CreateAssetMenu(menuName = "Relics/Effects/Shop Price Modifier", fileName = "RelicShopPriceModifier")]
    public class RelicShopPriceModifierDef : RelicEffectDef
    {
        public List<ShopOfferType> affectedOfferTypes = new();
        public int priceDelta;
        public float priceMultiplier = 1f;

        public override int ModifyShopPrice(ShopOfferData offer, int currentPrice)
        {
            if (offer == null || !AffectsOffer(offer))
                return currentPrice;

            float modifiedPrice = (currentPrice + priceDelta) * Mathf.Max(0f, priceMultiplier);
            return Mathf.Max(0, Mathf.RoundToInt(modifiedPrice));
        }

        private bool AffectsOffer(ShopOfferData offer)
        {
            return affectedOfferTypes == null ||
                   affectedOfferTypes.Count == 0 ||
                   affectedOfferTypes.Contains(offer.offerType);
        }
    }
}
