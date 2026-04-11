using UnityEngine;

namespace Enemies
{
    public class EnemyEffectResolver
    {
        public void ResolveEffectsForTrigger(
            EnemyDef enemyDef,
            EnemyTriggerType trigger,
            EnemyEffectContext context)
        {
            if (enemyDef == null || enemyDef.triggeredEffects == null)
                return;

            foreach (EnemyTriggeredEffect triggeredEffect in enemyDef.triggeredEffects)
            {
                if (triggeredEffect == null || triggeredEffect.trigger != trigger)
                    continue;

                ResolveEffect(triggeredEffect, context);
            }
        }

        private void ResolveEffect(EnemyTriggeredEffect effect, EnemyEffectContext context)
        {
            switch (effect.effectType)
            {
                case EnemyEffectType.None:
                    break;

                case EnemyEffectType.GainMana:
                    ResolveGainMana(effect, context);
                    break;

                case EnemyEffectType.SpawnEnemy:
                    ResolveSpawnEnemy(effect, context);
                    break;

                default:
                    Debug.LogWarning($"Unhandled enemy effect type: {effect.effectType}");
                    break;
            }
        }

        private void ResolveGainMana(EnemyTriggeredEffect effect, EnemyEffectContext context)
        {
            if (context?.PlayerEffects == null)
                return;

            context.PlayerEffects.GainMana(effect.amount);
        }

        private void ResolveSpawnEnemy(EnemyTriggeredEffect effect, EnemyEffectContext context)
        {
            if (context?.EnemySpawner == null || effect.enemyToSpawn == null)
                return;

            int spawnCount = Mathf.Max(1, effect.amount);

            for (int i = 0; i < spawnCount; i++)
            {
                context.EnemySpawner.SpawnEnemyNow(effect.enemyToSpawn, context.TrackDistance);
            }
        }
    }
}
