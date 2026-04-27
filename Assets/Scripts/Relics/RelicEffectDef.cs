using Combat;
using RunFlow;
using UnityEngine;

namespace Relics
{
    public abstract class RelicEffectDef : ScriptableObject
    {
        public virtual void ModifyCombatSetup(CombatSessionSetup setup)
        {
        }

        public virtual void ModifyCard(RelicCardModificationContext context)
        {
        }

        public virtual int ModifyShopPrice(ShopOfferData offer, int currentPrice)
        {
            return currentPrice;
        }
    }
}
