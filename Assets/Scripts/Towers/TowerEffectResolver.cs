using UnityEngine;

namespace Towers
{
    public class TowerEffectResolver
    {
        public void ResolveEffectsForTrigger(TowerDef towerDef, TowerTriggerType trigger, TowerEffectContext context)
        {
            if (towerDef == null || towerDef.triggeredEffects == null)
                return;

            foreach (TowerTriggeredEffect triggeredEffect in towerDef.triggeredEffects)
            {
                if (triggeredEffect == null || triggeredEffect.trigger != trigger)
                    continue;

                ResolveEffect(triggeredEffect, context);
            }
        }

        private static void ResolveEffect(TowerTriggeredEffect effect, TowerEffectContext context)
        {
            switch (effect.effectType)
            {
                case TowerEffectType.None:
                    break;

                case TowerEffectType.DamageTarget:
                    ResolveDamageTarget(effect, context);
                    break;

                case TowerEffectType.HealTower:
                    ResolveHealTower(effect, context);
                    break;

                default:
                    Debug.LogWarning($"Unhandled tower effect type: {effect.effectType}");
                    break;
            }
        }

        private static void ResolveDamageTarget(TowerTriggeredEffect effect, TowerEffectContext context)
        {
            if (context?.TargetEnemy == null || context.TargetEnemy.IsDeadOrEscaped)
                return;

            context.TargetEnemy.TakeDamage(effect.amount, effect.damageType);
        }

        private static void ResolveHealTower(TowerTriggeredEffect effect, TowerEffectContext context)
        {
            if (context?.Tower == null)
                return;

            context.Tower.Heal(effect.amount);
        }
    }
}
