using Cards;
using System.Collections.Generic;
using Enemies;
using UnityEngine;

namespace Towers
{
    public class TowerEffectResolver
    {
        public void ResolveEffectsForTrigger(TowerAgent tower, TowerTriggerType trigger, TowerEffectContext context)
        {
            if (tower == null)
                return;

            ResolveEffectList(tower.Definition != null ? tower.Definition.triggeredEffects : null, trigger, context);
            ResolveSupportEffectList(tower.SupportEffects, trigger, context);
        }

        private static void ResolveEffectList(IReadOnlyList<TowerTriggeredEffect> effects, TowerTriggerType trigger, TowerEffectContext context)
        {
            if (effects == null)
                return;

            for (int i = 0; i < effects.Count; i++)
            {
                TowerTriggeredEffect triggeredEffect = effects[i];
                if (triggeredEffect == null || triggeredEffect.trigger != trigger)
                    continue;

                ResolveEffect(triggeredEffect, context);
            }
        }

        private static void ResolveSupportEffectList(IReadOnlyList<RuntimeSupportEffect> supportEffects, TowerTriggerType trigger, TowerEffectContext context)
        {
            if (supportEffects == null)
                return;

            for (int i = 0; i < supportEffects.Count; i++)
            {
                TowerTriggeredEffect triggeredEffect = supportEffects[i]?.TriggeredEffect;
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

                case TowerEffectType.GainMana:
                    ResolveGainMana(effect, context);
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

        private static void ResolveGainMana(TowerTriggeredEffect effect, TowerEffectContext context)
        {
            if (context?.RuntimeContext.PlayerEffects == null || effect.amount <= 0f)
                return;

            context.RuntimeContext.PlayerEffects.GainMana(Mathf.RoundToInt(effect.amount));
        }
    }
}
