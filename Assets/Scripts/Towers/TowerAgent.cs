using System.Collections.Generic;
using Cards;
using Enemies;
using UnityEngine;

namespace Towers
{
    public class TowerAgent : MonoBehaviour
    {
        private sealed class TrackedModifier
        {
            public TrackedModifier(IStatModifier modifier, TowerModifierSource source, TowerModifierDuration duration)
            {
                Modifier = modifier;
                Source = source;
                Duration = duration;
            }

            public IStatModifier Modifier { get; }
            public TowerModifierSource Source { get; }
            public TowerModifierDuration Duration { get; }
        }

        private readonly List<IStatModifier> runtimeModifiers = new();
        private readonly List<TrackedModifier> trackedModifiers = new();
        private readonly List<TowerAgent> inheritedModifierSources = new();
        private readonly List<IAttackExecution> attackExecutions = new();
        private readonly List<EnemyAgent> targetBuffer = new();
        private readonly HashSet<EnemyAgent> inRangeEnemies = new();
        private readonly HashSet<EnemyAgent> previousInRangeEnemies = new();
        private readonly List<TowerAttackDef> runtimeAttackDefinitions = new();
        private readonly List<CardAugmentDef> appliedAugments = new();

        private static readonly ITargetingStrategy DefaultTargetingStrategy = new PriorityTargetingStrategy();

        private readonly TowerEffectResolver effectResolver = new();
        private TowerDef towerDef;
        private TowerRuntimeContext runtimeContext;
        private TargetPriority currentPriority;
        private float currentHealth;
        private float legacyPlacementRadius;
        private TowerResolvedStats legacyStats;
        private bool isLegacyTower;
        private bool isInitialized;
        private bool isDead;
        private bool useRuntimeAttackDefinitions;

        public TowerDef Definition => towerDef;
        public TowerRuntimeContext RuntimeContext => runtimeContext;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => GetResolvedStats().MaxHealth;
        public bool IsDead => isDead;
        public float PlacementRadius => towerDef != null ? towerDef.placementRadius : legacyPlacementRadius;
        public float Range => GetResolvedStats().Range;
        public TargetPriority CurrentPriority => currentPriority;
        public string DisplayName => towerDef != null && !string.IsNullOrWhiteSpace(towerDef.displayName) ? towerDef.displayName : gameObject.name;
        public IReadOnlyList<CardAugmentDef> AppliedAugments => appliedAugments;
        internal int TrackedModifierCount => trackedModifiers.Count;

        public virtual void Initialize(TowerDef def, TowerRuntimeContext context)
        {
            ShutdownExecutions();

            towerDef = def;
            runtimeContext = context;
            currentPriority = def != null ? def.defaultTargetPriority : TargetPriority.First;
            isLegacyTower = false;
            isDead = false;
            isInitialized = true;

            runtimeModifiers.Clear();
            trackedModifiers.Clear();
            inheritedModifierSources.Clear();
            appliedAugments.Clear();
            runtimeAttackDefinitions.Clear();
            useRuntimeAttackDefinitions = false;
            inRangeEnemies.Clear();
            previousInRangeEnemies.Clear();
            if (def != null && def.defaultModifiers != null)
            {
                foreach (TowerStatModifierDef modifier in def.defaultModifiers)
                {
                    if (modifier != null)
                        AddModifier(modifier, TowerModifierSource.Base, TowerModifierDuration.Permanent);
                }
            }

            currentHealth = GetResolvedStats().MaxHealth;
            BuildAttackExecutions();
        }

        public virtual void Initialize(float placementRadius, float range, TowerRuntimeContext context)
        {
            Initialize(
                placementRadius,
                new TowerResolvedStats(10f, range, 1f, 0f),
                context
            );
        }

        public virtual void Initialize(float placementRadius, TowerResolvedStats baseStats, TowerRuntimeContext context)
        {
            ShutdownExecutions();

            towerDef = null;
            runtimeContext = context;
            currentPriority = TargetPriority.First;
            legacyPlacementRadius = Mathf.Max(0f, placementRadius);
            legacyStats = baseStats;
            currentHealth = legacyStats.MaxHealth;
            isLegacyTower = true;
            isDead = false;
            isInitialized = true;
            runtimeModifiers.Clear();
            trackedModifiers.Clear();
            inheritedModifierSources.Clear();
            appliedAugments.Clear();
            runtimeAttackDefinitions.Clear();
            useRuntimeAttackDefinitions = false;
            inRangeEnemies.Clear();
            previousInRangeEnemies.Clear();
        }

        protected virtual void FixedUpdate()
        {
            if (!isInitialized || isDead)
                return;

            UpdateRangeTriggers();

            float deltaTime = Time.fixedDeltaTime;
            for (int i = 0; i < attackExecutions.Count; i++)
                attackExecutions[i].Tick(deltaTime);
        }

        public void SetTargetPriority(TargetPriority priority)
        {
            currentPriority = priority;
        }

        public TargetPriority CycleTargetPriority()
        {
            currentPriority = currentPriority switch
            {
                TargetPriority.First => TargetPriority.Last,
                TargetPriority.Last => TargetPriority.Strong,
                _ => TargetPriority.First
            };

            return currentPriority;
        }

        public void SetAppliedAugments(IReadOnlyList<CardAugmentDef> augments)
        {
            appliedAugments.Clear();

            if (augments == null)
                return;

            for (int i = 0; i < augments.Count; i++)
            {
                CardAugmentDef augment = augments[i];
                if (augment != null)
                    appliedAugments.Add(augment);
            }
        }

        public void AddModifier(IStatModifier modifier)
        {
            AddModifier(modifier, TowerModifierSource.Unknown, TowerModifierDuration.Permanent);
        }

        public void AddModifier(IStatModifier modifier, TowerModifierSource source, TowerModifierDuration duration)
        {
            if (modifier == null)
                return;

            runtimeModifiers.Add(modifier);
            trackedModifiers.Add(new TrackedModifier(modifier, source, duration));
        }

        public void RemoveModifier(IStatModifier modifier)
        {
            if (modifier == null)
                return;

            runtimeModifiers.Remove(modifier);
            RemoveTrackedModifier(modifier);
        }

        public void InheritModifiersFrom(TowerAgent sourceTower, bool append = true)
        {
            if (sourceTower == null)
                return;

            if (!append)
            {
                runtimeModifiers.Clear();
                trackedModifiers.Clear();
                inheritedModifierSources.Clear();
            }

            if (sourceTower != this && !inheritedModifierSources.Contains(sourceTower))
                inheritedModifierSources.Add(sourceTower);
        }

        public void SetRuntimeAttackDefinitions(IReadOnlyList<TowerAttackDef> attacks)
        {
            runtimeAttackDefinitions.Clear();
            useRuntimeAttackDefinitions = false;

            if (attacks != null)
            {
                for (int i = 0; i < attacks.Count; i++)
                {
                    TowerAttackDef attackDef = attacks[i];
                    if (attackDef != null)
                        runtimeAttackDefinitions.Add(attackDef);
                }
            }

            useRuntimeAttackDefinitions = runtimeAttackDefinitions.Count > 0;

            if (isInitialized)
                BuildAttackExecutions();
        }

        public List<TowerInspectorModifierEntry> GetActiveEffectEntries()
        {
            List<TowerInspectorModifierEntry> entries = new();
            AppendOwnInspectorEntries(entries, TowerModifierDuration.Active, null, null);
            return entries;
        }

        public List<TowerInspectorModifierEntry> GetPermanentModifierEntries()
        {
            List<TowerInspectorModifierEntry> entries = new();
            AppendOwnInspectorEntries(entries, TowerModifierDuration.Permanent, null, null);
            AppendInheritedInspectorEntries(entries);
            return entries;
        }

        public TowerResolvedStats GetResolvedStats()
        {
            TowerResolvedStats stats = isLegacyTower || towerDef == null
                ? legacyStats
                : towerDef.GetBaseResolvedStats();

            ApplyRuntimeModifiers(ref stats);

            stats.Clamp();
            return stats;
        }

        private void ApplyRuntimeModifiers(ref TowerResolvedStats stats)
        {
            for (int i = 0; i < runtimeModifiers.Count; i++)
                runtimeModifiers[i].ModifyStats(this, ref stats);

            for (int i = 0; i < inheritedModifierSources.Count; i++)
            {
                TowerAgent sourceTower = inheritedModifierSources[i];
                if (sourceTower == null || sourceTower.IsDead)
                    continue;

                for (int modifierIndex = 0; modifierIndex < sourceTower.runtimeModifiers.Count; modifierIndex++)
                {
                    IStatModifier modifier = sourceTower.runtimeModifiers[modifierIndex];
                    if (modifier != null)
                        modifier.ModifyStats(this, ref stats);
                }
            }
        }

        private void RemoveTrackedModifier(IStatModifier modifier)
        {
            for (int i = trackedModifiers.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(trackedModifiers[i].Modifier, modifier))
                    continue;

                trackedModifiers.RemoveAt(i);
                return;
            }
        }

        private void AppendOwnInspectorEntries(
            List<TowerInspectorModifierEntry> entries,
            TowerModifierDuration durationFilter,
            TowerModifierSource? sourceOverride,
            TowerModifierDuration? durationOverride)
        {
            for (int i = 0; i < trackedModifiers.Count; i++)
            {
                TrackedModifier trackedModifier = trackedModifiers[i];
                if (trackedModifier == null || trackedModifier.Duration != durationFilter)
                    continue;

                entries.Add(
                    CreateInspectorEntry(
                        trackedModifier.Modifier,
                        sourceOverride ?? trackedModifier.Source,
                        durationOverride ?? trackedModifier.Duration));
            }
        }

        private void AppendInheritedInspectorEntries(List<TowerInspectorModifierEntry> entries)
        {
            for (int i = 0; i < inheritedModifierSources.Count; i++)
            {
                TowerAgent sourceTower = inheritedModifierSources[i];
                if (sourceTower == null || sourceTower.IsDead)
                    continue;

                sourceTower.AppendOwnInspectorEntries(entries, TowerModifierDuration.Permanent, TowerModifierSource.Inherited, TowerModifierDuration.Permanent);
                sourceTower.AppendOwnInspectorEntries(entries, TowerModifierDuration.Active, TowerModifierSource.Inherited, TowerModifierDuration.Permanent);
            }
        }

        private static TowerInspectorModifierEntry CreateInspectorEntry(
            IStatModifier modifier,
            TowerModifierSource source,
            TowerModifierDuration duration)
        {
            if (modifier is TowerStatModifierDef towerModifier)
            {
                return new TowerInspectorModifierEntry(
                    towerModifier.DisplayName,
                    towerModifier.Icon,
                    towerModifier.Tone,
                    source,
                    duration);
            }

            string fallbackName = modifier != null ? NicifyTypeName(modifier.GetType().Name) : "Unknown Effect";
            return new TowerInspectorModifierEntry(
                fallbackName,
                null,
                TowerModifierTone.Neutral,
                source,
                duration);
        }

        private static string NicifyTypeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown Effect";

            System.Text.StringBuilder builder = new(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (i > 0 && char.IsUpper(current) && !char.IsWhiteSpace(value[i - 1]))
                    builder.Append(' ');

                builder.Append(current);
            }

            return builder.ToString();
        }

        public EnemyAgent AcquireTarget(IReadOnlyList<TowerTargetFilterDef> filters)
        {
            if (runtimeContext.EnemyManager == null)
                return null;

            targetBuffer.Clear();

            TowerResolvedStats stats = GetResolvedStats();
            IReadOnlyList<EnemyAgent> activeEnemies = runtimeContext.EnemyManager.ActiveEnemies;
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                EnemyAgent enemy = activeEnemies[i];
                if (IsTargetValid(enemy, filters, stats))
                    targetBuffer.Add(enemy);
            }

            TargetingContext targetingContext = new(this, stats, currentPriority, targetBuffer);
            return DefaultTargetingStrategy.SelectTarget(targetingContext);
        }

        public bool IsTargetValid(EnemyAgent enemy, IReadOnlyList<TowerTargetFilterDef> filters)
        {
            return IsTargetValid(enemy, filters, GetResolvedStats());
        }

        public void TakeDamage(float amount)
        {
            if (isDead || amount <= 0f)
                return;

            currentHealth -= amount;
            if (currentHealth <= 0f)
                Die();
        }

        public void Heal(float amount)
        {
            if (isDead || amount <= 0f)
                return;

            currentHealth = Mathf.Min(currentHealth + amount, GetResolvedStats().MaxHealth);
        }

        public void Die()
        {
            if (isDead)
                return;

            isDead = true;
            FireTrigger(TowerTriggerType.OnDeath, null, 0f, transform.position, false);
            ShutdownExecutions();
            runtimeContext.TowerManager?.UnregisterTower(this);
            Destroy(gameObject);
        }

        public void ReportHit(EnemyAgent enemy, float damageAmount, Vector3 effectPosition)
        {
            if (enemy == null)
                return;

            bool wasKill = enemy.IsDeadOrEscaped;
            FireTrigger(TowerTriggerType.OnHit, enemy, damageAmount, effectPosition, wasKill);
        }

        public void ReportKill(EnemyAgent enemy, float damageAmount, Vector3 effectPosition)
        {
            if (enemy == null)
                return;

            FireTrigger(TowerTriggerType.OnKill, enemy, damageAmount, effectPosition, true);
        }

        private bool IsTargetValid(EnemyAgent enemy, IReadOnlyList<TowerTargetFilterDef> filters, TowerResolvedStats stats)
        {
            if (enemy == null || enemy.IsDeadOrEscaped)
                return false;

            if (Vector2.Distance(transform.position, enemy.transform.position) > stats.Range)
                return false;

            if (filters == null)
                return true;

            for (int i = 0; i < filters.Count; i++)
            {
                TowerTargetFilterDef filter = filters[i];
                if (filter != null && !filter.IsTargetValid(this, enemy))
                    return false;
            }

            return true;
        }

        private void BuildAttackExecutions()
        {
            ShutdownExecutions();
            attackExecutions.Clear();

            IReadOnlyList<TowerAttackDef> attackDefs = useRuntimeAttackDefinitions
                ? runtimeAttackDefinitions
                : towerDef != null
                    ? towerDef.attacks
                    : null;

            if (attackDefs == null)
                return;

            for (int i = 0; i < attackDefs.Count; i++)
            {
                TowerAttackDef attackDef = attackDefs[i];
                if (attackDef == null)
                    continue;

                IAttackExecution execution = attackDef.CreateExecution(this);
                if (execution != null)
                    attackExecutions.Add(execution);
            }
        }

        private void ShutdownExecutions()
        {
            for (int i = 0; i < attackExecutions.Count; i++)
                attackExecutions[i].Shutdown();

            attackExecutions.Clear();
            inRangeEnemies.Clear();
            previousInRangeEnemies.Clear();
        }

        private void UpdateRangeTriggers()
        {
            EnemyManager enemyManager = runtimeContext.EnemyManager;
            if (enemyManager == null)
                return;

            previousInRangeEnemies.Clear();
            previousInRangeEnemies.UnionWith(inRangeEnemies);
            inRangeEnemies.Clear();

            TowerResolvedStats stats = GetResolvedStats();
            IReadOnlyList<EnemyAgent> activeEnemies = enemyManager.ActiveEnemies;
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                EnemyAgent enemy = activeEnemies[i];
                if (!IsTargetValid(enemy, null, stats))
                    continue;

                inRangeEnemies.Add(enemy);
                if (!previousInRangeEnemies.Contains(enemy))
                    FireTrigger(TowerTriggerType.OnEnemyEnterRange, enemy, 0f, enemy.transform.position, false);
            }
        }

        private void FireTrigger(TowerTriggerType trigger, EnemyAgent enemy, float damageAmount, Vector3 effectPosition, bool wasKill)
        {
            if (towerDef == null || effectResolver == null || towerDef.triggeredEffects == null || towerDef.triggeredEffects.Count == 0)
                return;

            Vector3 towerPosition = transform.position;
            Vector3 targetPosition = enemy != null ? enemy.transform.position : towerPosition;
            TowerEffectContext effectContext = new(
                this,
                enemy,
                towerPosition,
                targetPosition,
                effectPosition,
                damageAmount,
                wasKill,
                runtimeContext
            );

            effectResolver.ResolveEffectsForTrigger(towerDef, trigger, effectContext);
        }
    }
}
