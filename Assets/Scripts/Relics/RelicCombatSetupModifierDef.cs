using Combat;
using UnityEngine;

namespace Relics
{
    [CreateAssetMenu(menuName = "Relics/Effects/Combat Setup Modifier", fileName = "RelicCombatSetupModifier")]
    public class RelicCombatSetupModifierDef : RelicEffectDef
    {
        [Header("Mana")]
        public bool overrideStartingMana;
        public int startingManaOverride;
        public int startingManaDelta;
        public int maxManaDelta;
        public float manaRegenPerSecondDelta;

        [Header("Cards")]
        public int openingHandSizeDelta;
        public int maxHandSizeDelta;
        public int manualDrawCostDelta;

        public override void ModifyCombatSetup(CombatSessionSetup setup)
        {
            if (setup == null)
                return;

            if (overrideStartingMana)
                setup.StartingMana = Mathf.Max(0, startingManaOverride);

            setup.StartingMana = Mathf.Max(0, setup.StartingMana + startingManaDelta);
            setup.MaxMana = Mathf.Max(0, setup.MaxMana + maxManaDelta);
            setup.ManaRegenPerSecond = Mathf.Max(0f, setup.ManaRegenPerSecond + manaRegenPerSecondDelta);
            setup.OpeningHandSize = Mathf.Max(0, setup.OpeningHandSize + openingHandSizeDelta);
            setup.MaxHandSize = Mathf.Max(0, setup.MaxHandSize + maxHandSizeDelta);
            setup.ManualDrawCost = Mathf.Max(0, setup.ManualDrawCost + manualDrawCostDelta);
        }
    }
}
