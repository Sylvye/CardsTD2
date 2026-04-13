using System.Collections.Generic;
using Combat;
using Enemies;
using Towers;
using UnityEngine;

namespace Cards
{
    public sealed class SpellZoneRuntime : MonoBehaviour
    {
        private sealed class TickState
        {
            public TickState(SpellTriggeredEffect effect)
            {
                Effect = effect;
                Timer = Mathf.Max(0.01f, effect.tickInterval);
            }

            public SpellTriggeredEffect Effect { get; }
            public float Timer { get; set; }
        }

        private readonly HashSet<EnemyAgent> enemiesInZone = new();
        private readonly HashSet<EnemyAgent> previousEnemiesInZone = new();
        private readonly HashSet<TowerAgent> towersInZone = new();
        private readonly HashSet<TowerAgent> previousTowersInZone = new();
        private readonly List<Collider2D> overlapResults = new();
        private readonly List<TickState> tickStates = new();
        private readonly Dictionary<EnemyAgent, HashSet<SpellTriggeredEffect>> trackedEnemyAuraEffects = new();
        private readonly Dictionary<TowerAgent, HashSet<SpellTriggeredEffect>> trackedTowerAuraEffects = new();

        private SpellDef spellDef;
        private TowerManager towerManager;
        private EnemyManager enemyManager;
        private IPlayerEffects playerEffects;
        private Collider2D zoneCollider;
        private float remainingLifetime;
        private bool isInitialized;

        public bool Initialize(
            SpellDef def,
            Collider2D collider,
            TowerManager towers,
            EnemyManager enemies,
            IPlayerEffects player)
        {
            if (def == null || collider == null)
                return false;

            if (!collider.isTrigger)
            {
                Debug.LogWarning($"Spell '{def.name}' requires its Collider2D to be marked as a trigger.");
                return false;
            }

            CleanupAllAuraModifiers();
            UnsubscribeEnemyDeaths();
            enemiesInZone.Clear();
            previousEnemiesInZone.Clear();
            towersInZone.Clear();
            previousTowersInZone.Clear();
            tickStates.Clear();

            spellDef = def;
            zoneCollider = collider;
            towerManager = towers;
            enemyManager = enemies;
            playerEffects = player;
            remainingLifetime = Mathf.Max(0.01f, def.duration);
            isInitialized = true;

            if (spellDef.triggeredEffects != null)
            {
                for (int i = 0; i < spellDef.triggeredEffects.Count; i++)
                {
                    SpellTriggeredEffect effect = spellDef.triggeredEffects[i];
                    if (effect != null && effect.trigger == SpellTriggerType.OnTick)
                        tickStates.Add(new TickState(effect));
                }
            }

            RefreshOccupants();
            previousEnemiesInZone.Clear();
            previousEnemiesInZone.UnionWith(enemiesInZone);
            previousTowersInZone.Clear();
            previousTowersInZone.UnionWith(towersInZone);
            ResolveOnPlace();
            return true;
        }

        private void Update()
        {
            if (!isInitialized)
                return;

            float deltaTime = Time.deltaTime;
            remainingLifetime -= deltaTime;
            if (remainingLifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            RefreshOccupants();
            ProcessEnemyMembershipChanges();
            ProcessTowerMembershipChanges();
            UpdateTicks(deltaTime);
            SaveOccupancyState();
        }

        private void OnDestroy()
        {
            UnsubscribeEnemyDeaths();
            CleanupAllAuraModifiers();
        }

        private void HandleEnemyDied(EnemyAgent enemy)
        {
            if (!isInitialized || enemy == null || !enemiesInZone.Contains(enemy))
                return;

            ResolveTrigger(SpellTriggerType.OnEnemyDeath, enemy, null);
            CleanupEnemyAuraModifiers(enemy);
            enemy.Died -= HandleEnemyDied;
            enemiesInZone.Remove(enemy);
            previousEnemiesInZone.Remove(enemy);
        }

        private void UpdateTicks(float deltaTime)
        {
            if (tickStates.Count == 0)
                return;

            for (int i = 0; i < tickStates.Count; i++)
            {
                TickState tickState = tickStates[i];
                tickState.Timer -= deltaTime;
                if (tickState.Timer > 0f)
                    continue;

                tickState.Timer = Mathf.Max(0.01f, tickState.Effect.tickInterval);
                ResolveEffect(
                    tickState.Effect,
                    new SpellEffectContext(
                        gameObject,
                        null,
                        null,
                        CaptureEnemySnapshot(),
                        CaptureTowerSnapshot(),
                        enemyManager,
                        towerManager,
                        playerEffects));
            }
        }

        private void RefreshOccupants()
        {
            enemiesInZone.Clear();
            towersInZone.Clear();
            overlapResults.Clear();

            zoneCollider.Overlap(ContactFilter2D.noFilter, overlapResults);
            for (int i = 0; i < overlapResults.Count; i++)
            {
                Collider2D hit = overlapResults[i];
                if (hit == null)
                    continue;

                EnemyAgent enemy = hit.GetComponentInParent<EnemyAgent>();
                if (enemy != null && enemy.gameObject != gameObject && !enemy.IsDeadOrEscaped)
                {
                    enemiesInZone.Add(enemy);
                    continue;
                }

                TowerAgent tower = hit.GetComponentInParent<TowerAgent>();
                if (tower != null && tower.gameObject != gameObject && !tower.IsDead)
                    towersInZone.Add(tower);
            }
        }

        private void ProcessEnemyMembershipChanges()
        {
            List<EnemyAgent> currentEnemies = CaptureEnemySnapshot();
            for (int i = 0; i < currentEnemies.Count; i++)
            {
                EnemyAgent enemy = currentEnemies[i];
                if (enemy == null || previousEnemiesInZone.Contains(enemy))
                    continue;

                enemy.Died -= HandleEnemyDied;
                enemy.Died += HandleEnemyDied;
                ResolveTrigger(SpellTriggerType.OnEnemyEnter, enemy, null);
            }

            List<EnemyAgent> previousEnemies = new(previousEnemiesInZone.Count);
            foreach (EnemyAgent enemy in previousEnemiesInZone)
                previousEnemies.Add(enemy);

            for (int i = 0; i < previousEnemies.Count; i++)
            {
                EnemyAgent enemy = previousEnemies[i];
                if (enemy == null || enemiesInZone.Contains(enemy))
                    continue;

                enemy.Died -= HandleEnemyDied;
                ResolveTrigger(SpellTriggerType.OnEnemyLeave, enemy, null);
                CleanupEnemyAuraModifiers(enemy);
            }
        }

        private void ProcessTowerMembershipChanges()
        {
            List<TowerAgent> previousTowers = new(previousTowersInZone.Count);
            foreach (TowerAgent tower in previousTowersInZone)
                previousTowers.Add(tower);

            for (int i = 0; i < previousTowers.Count; i++)
            {
                TowerAgent tower = previousTowers[i];
                if (tower == null || towersInZone.Contains(tower))
                    continue;

                CleanupTowerAuraModifiers(tower);
            }
        }

        private void SaveOccupancyState()
        {
            previousEnemiesInZone.Clear();
            previousEnemiesInZone.UnionWith(enemiesInZone);
            previousTowersInZone.Clear();
            previousTowersInZone.UnionWith(towersInZone);
        }

        private void ResolveTrigger(SpellTriggerType trigger, EnemyAgent sourceEnemy, TowerAgent sourceTower)
        {
            if (spellDef == null || spellDef.triggeredEffects == null)
                return;

            SpellEffectContext context = new(
                gameObject,
                sourceEnemy,
                sourceTower,
                CaptureEnemySnapshot(),
                CaptureTowerSnapshot(),
                enemyManager,
                towerManager,
                playerEffects);

            for (int i = 0; i < spellDef.triggeredEffects.Count; i++)
            {
                SpellTriggeredEffect effect = spellDef.triggeredEffects[i];
                if (effect == null || effect.trigger != trigger || effect.effectType == SpellEffectType.None)
                    continue;

                ResolveEffect(effect, context);
            }
        }

        private void ResolveOnPlace()
        {
            if (spellDef == null || spellDef.triggeredEffects == null)
                return;

            SpellEffectContext context = new(
                gameObject,
                null,
                null,
                CaptureEnemySnapshot(),
                CaptureTowerSnapshot(),
                enemyManager,
                towerManager,
                playerEffects);

            for (int i = 0; i < spellDef.triggeredEffects.Count; i++)
            {
                SpellTriggeredEffect effect = spellDef.triggeredEffects[i];
                if (effect == null || effect.trigger != SpellTriggerType.OnPlace || effect.effectType == SpellEffectType.None)
                    continue;

                ResolveEffect(effect, context);
            }
        }

        private void ResolveEffect(SpellTriggeredEffect effect, SpellEffectContext context)
        {
            switch (effect.effectType)
            {
                case SpellEffectType.GainMana:
                    ResolveGainMana(effect, context);
                    break;
                case SpellEffectType.DamageEnemy:
                    ResolveEnemyTargets(effect, context, enemy => enemy.TakeDamage(effect.amount));
                    break;
                case SpellEffectType.ApplyEnemyModifier:
                    ResolveEnemyModifier(effect, context);
                    break;
                case SpellEffectType.ApplyTowerModifier:
                    ResolveTowerModifier(effect, context);
                    break;
            }
        }

        private void ResolveGainMana(SpellTriggeredEffect effect, SpellEffectContext context)
        {
            if (effect.targetType != SpellTargetType.Player)
                return;

            int amount = Mathf.RoundToInt(effect.amount);
            if (amount <= 0)
                return;

            context.PlayerEffects?.GainMana(amount);
        }

        private void ResolveEnemyModifier(SpellTriggeredEffect effect, SpellEffectContext context)
        {
            if (effect.enemyModifier == null)
                return;

            ResolveEnemyTargets(effect, context, enemy =>
            {
                if (effect.modifierApplicationMode == SpellModifierApplicationMode.WhileInside)
                {
                    if (TryTrackEnemyAuraEffect(enemy, effect))
                        enemy.AddModifier(effect.enemyModifier);
                    return;
                }

                enemy.AddModifier(effect.enemyModifier);
            });
        }

        private void ResolveTowerModifier(SpellTriggeredEffect effect, SpellEffectContext context)
        {
            if (effect.towerModifier == null)
                return;

            ResolveTowerTargets(effect, context, tower =>
            {
                if (effect.modifierApplicationMode == SpellModifierApplicationMode.WhileInside)
                {
                    if (TryTrackTowerAuraEffect(tower, effect))
                        tower.AddModifier(effect.towerModifier);
                    return;
                }

                tower.AddModifier(effect.towerModifier);
            });
        }

        private void ResolveEnemyTargets(SpellTriggeredEffect effect, SpellEffectContext context, System.Action<EnemyAgent> apply)
        {
            if (!TargetsEnemies(effect.targetType))
                return;

            if (effect.trigger == SpellTriggerType.OnTick || effect.trigger == SpellTriggerType.OnPlace)
            {
                IReadOnlyCollection<EnemyAgent> currentEnemies = context.CurrentEnemies;
                foreach (EnemyAgent enemy in currentEnemies)
                {
                    if (enemy == null || enemy.IsDeadOrEscaped || !enemiesInZone.Contains(enemy))
                        continue;

                    apply(enemy);
                }

                return;
            }

            EnemyAgent sourceEnemy = context.SourceEnemy;
            if (sourceEnemy == null || sourceEnemy.IsDeadOrEscaped || !enemiesInZone.Contains(sourceEnemy))
                return;

            apply(sourceEnemy);
        }

        private void ResolveTowerTargets(SpellTriggeredEffect effect, SpellEffectContext context, System.Action<TowerAgent> apply)
        {
            if (!TargetsTowers(effect.targetType))
                return;

            if (effect.trigger == SpellTriggerType.OnTick || effect.trigger == SpellTriggerType.OnPlace)
            {
                IReadOnlyCollection<TowerAgent> currentTowers = context.CurrentTowers;
                foreach (TowerAgent tower in currentTowers)
                {
                    if (tower == null || tower.IsDead || !towersInZone.Contains(tower))
                        continue;

                    apply(tower);
                }

                return;
            }

            TowerAgent sourceTower = context.SourceTower;
            if (sourceTower == null || sourceTower.IsDead || !towersInZone.Contains(sourceTower))
                return;

            apply(sourceTower);
        }

        private bool TryTrackEnemyAuraEffect(EnemyAgent enemy, SpellTriggeredEffect effect)
        {
            if (enemy == null || effect == null)
                return false;

            if (!trackedEnemyAuraEffects.TryGetValue(enemy, out HashSet<SpellTriggeredEffect> effects))
            {
                effects = new HashSet<SpellTriggeredEffect>();
                trackedEnemyAuraEffects.Add(enemy, effects);
            }

            return effects.Add(effect);
        }

        private bool TryTrackTowerAuraEffect(TowerAgent tower, SpellTriggeredEffect effect)
        {
            if (tower == null || effect == null)
                return false;

            if (!trackedTowerAuraEffects.TryGetValue(tower, out HashSet<SpellTriggeredEffect> effects))
            {
                effects = new HashSet<SpellTriggeredEffect>();
                trackedTowerAuraEffects.Add(tower, effects);
            }

            return effects.Add(effect);
        }

        private void CleanupEnemyAuraModifiers(EnemyAgent enemy)
        {
            if (enemy == null || !trackedEnemyAuraEffects.TryGetValue(enemy, out HashSet<SpellTriggeredEffect> effects))
                return;

            foreach (SpellTriggeredEffect effect in effects)
            {
                if (effect?.enemyModifier != null)
                    enemy.RemoveModifier(effect.enemyModifier);
            }

            trackedEnemyAuraEffects.Remove(enemy);
        }

        private void CleanupTowerAuraModifiers(TowerAgent tower)
        {
            if (tower == null || !trackedTowerAuraEffects.TryGetValue(tower, out HashSet<SpellTriggeredEffect> effects))
                return;

            foreach (SpellTriggeredEffect effect in effects)
            {
                if (effect?.towerModifier != null)
                    tower.RemoveModifier(effect.towerModifier);
            }

            trackedTowerAuraEffects.Remove(tower);
        }

        private void CleanupAllAuraModifiers()
        {
            List<EnemyAgent> enemyKeys = new(trackedEnemyAuraEffects.Keys);
            for (int i = 0; i < enemyKeys.Count; i++)
                CleanupEnemyAuraModifiers(enemyKeys[i]);

            List<TowerAgent> towerKeys = new(trackedTowerAuraEffects.Keys);
            for (int i = 0; i < towerKeys.Count; i++)
                CleanupTowerAuraModifiers(towerKeys[i]);
        }

        private void UnsubscribeEnemyDeaths()
        {
            foreach (EnemyAgent enemy in enemiesInZone)
            {
                if (enemy != null)
                    enemy.Died -= HandleEnemyDied;
            }

            foreach (EnemyAgent enemy in previousEnemiesInZone)
            {
                if (enemy != null)
                    enemy.Died -= HandleEnemyDied;
            }
        }

        private static bool TargetsEnemies(SpellTargetType targetType)
        {
            return targetType == SpellTargetType.Enemies || targetType == SpellTargetType.EnemiesAndTowers;
        }

        private static bool TargetsTowers(SpellTargetType targetType)
        {
            return targetType == SpellTargetType.Towers || targetType == SpellTargetType.EnemiesAndTowers;
        }

        private List<EnemyAgent> CaptureEnemySnapshot()
        {
            List<EnemyAgent> enemySnapshot = new(enemiesInZone.Count);
            foreach (EnemyAgent enemy in enemiesInZone)
                enemySnapshot.Add(enemy);

            return enemySnapshot;
        }

        private List<TowerAgent> CaptureTowerSnapshot()
        {
            List<TowerAgent> towerSnapshot = new(towersInZone.Count);
            foreach (TowerAgent tower in towersInZone)
                towerSnapshot.Add(tower);

            return towerSnapshot;
        }
    }
}
