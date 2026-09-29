using System.Collections.Generic;
using Combat;
using RunFlow;
using UnityEngine;

namespace Relics
{
    public static class RelicResolver
    {
        public static void ModifyCombatSetup(IReadOnlyList<OwnedRelic> relics, CombatSessionSetup setup)
        {
            if (setup == null)
                return;

            ForEachEffect(relics, effect => effect.ModifyCombatSetup(setup));
        }

        public static void ModifyCard(IReadOnlyList<OwnedRelic> relics, RelicCardModificationContext context)
        {
            if (context == null)
                return;

            ForEachEffect(relics, effect => effect.ModifyCard(context));
        }

        public static int ModifyShopPrice(IReadOnlyList<OwnedRelic> relics, ShopOfferData offer, int basePrice)
        {
            int price = Mathf.Max(0, basePrice);
            ForEachEffect(relics, effect => price = Mathf.Max(0, effect.ModifyShopPrice(offer, price)));
            return price;
        }

        public static int ModifyEnemyBuffChoiceCount(IReadOnlyList<OwnedRelic> relics, EnemyBuffPoolDef pool, int baseChoiceCount)
        {
            int choiceCount = Mathf.Max(0, baseChoiceCount);
            ForEachEffect(relics, effect => choiceCount = Mathf.Max(0, effect.ModifyEnemyBuffChoiceCount(pool, choiceCount)));
            return choiceCount;
        }

        private static void ForEachEffect(IReadOnlyList<OwnedRelic> relics, System.Action<RelicEffectDef> apply)
        {
            if (relics == null || apply == null)
                return;

            for (int relicIndex = 0; relicIndex < relics.Count; relicIndex++)
            {
                RelicDef relic = relics[relicIndex]?.Definition;
                if (relic?.effects == null)
                    continue;

                for (int effectIndex = 0; effectIndex < relic.effects.Count; effectIndex++)
                {
                    RelicEffectDef effect = relic.effects[effectIndex];
                    if (effect != null)
                        apply(effect);
                }
            }
        }
    }
}
