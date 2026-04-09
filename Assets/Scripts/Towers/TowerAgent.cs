using System.Collections.Generic;
using Enemies;
using UnityEngine;

namespace Towers
{
    public class TowerAgent : MonoBehaviour
    {
        private readonly List<IStatModifier> runtimeModifiers = new();
        private readonly List<IAttackExecution> attackExecutions = new();
        private readonly List<EnemyAgent> targetBuffer = new();

        private static readonly ITargetingStrategy DefaultTargetingStrategy = new PriorityTargetingStrategy();

        private TowerDef towerDef;
        private TowerRuntimeContext runtimeContext;
        private TargetPriority currentPriority;
        private float currentHealth;
        private float legacyPlacementRadius;
        private TowerResolvedStats legacyStats;
        private bool isLegacyTower;
        private bool isInitialized;
        private bool isDead;

        public TowerDef Definition => towerDef;
        public TowerRuntimeContext RuntimeContext => runtimeContext;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => GetResolvedStats().MaxHealth;
        public bool IsDead => isDead;
        public float PlacementRadius => towerDef != null ? towerDef.placementRadius : legacyPlacementRadius;
        public float Range => GetResolvedStats().Range;
        public TargetPriority CurrentPriority => currentPriority;

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
            if (def != null && def.defaultModifiers != null)
            {
                foreach (TowerStatModifierDef modifier in def.defaultModifiers)
                {
                    if (modifier != null)
                        runtimeModifiers.Add(modifier);
                }
            }

            currentHealth = GetResolvedStats().MaxHealth;
            BuildAttackExecutions();
        }

        public virtual void Initialize(float placementRadius, float range, TowerRuntimeContext context)
        {
            ShutdownExecutions();

            towerDef = null;
            runtimeContext = context;
            currentPriority = TargetPriority.First;
            legacyPlacementRadius = Mathf.Max(0f, placementRadius);
            legacyStats = new TowerResolvedStats(10f, range, 1f, 0f);
            currentHealth = legacyStats.MaxHealth;
            isLegacyTower = true;
            isDead = false;
            isInitialized = true;
            runtimeModifiers.Clear();
        }

        protected virtual void Update()
        {
            if (!isInitialized || isDead)
                return;

            float deltaTime = Time.deltaTime;
            for (int i = 0; i < attackExecutions.Count; i++)
                attackExecutions[i].Tick(deltaTime);
        }

        public void SetTargetPriority(TargetPriority priority)
        {
            currentPriority = priority;
        }

        public void AddModifier(IStatModifier modifier)
        {
            if (modifier == null)
                return;

            runtimeModifiers.Add(modifier);
        }

        public void RemoveModifier(IStatModifier modifier)
        {
            if (modifier == null)
                return;

            runtimeModifiers.Remove(modifier);
        }

        public TowerResolvedStats GetResolvedStats()
        {
            TowerResolvedStats stats = isLegacyTower || towerDef == null
                ? legacyStats
                : towerDef.GetBaseResolvedStats();

            for (int i = 0; i < runtimeModifiers.Count; i++)
                runtimeModifiers[i].ModifyStats(this, ref stats);

            stats.Clamp();
            return stats;
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
            ShutdownExecutions();
            runtimeContext.TowerManager?.UnregisterTower(this);
            Destroy(gameObject);
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
            attackExecutions.Clear();

            if (towerDef == null || towerDef.attacks == null)
                return;

            foreach (TowerAttackDef attackDef in towerDef.attacks)
            {
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
        }
    }
}
