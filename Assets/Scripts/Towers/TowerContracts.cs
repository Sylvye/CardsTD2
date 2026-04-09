using System.Collections.Generic;
using Enemies;
using UnityEngine;

namespace Towers
{
    public readonly struct TowerRuntimeContext
    {
        public TowerRuntimeContext(TowerManager towerManager, EnemyManager enemyManager)
        {
            TowerManager = towerManager;
            EnemyManager = enemyManager;
        }

        public TowerManager TowerManager { get; }
        public EnemyManager EnemyManager { get; }
    }

    public readonly struct TargetingContext
    {
        public TargetingContext(
            TowerAgent tower,
            TowerResolvedStats stats,
            TargetPriority priority,
            IReadOnlyList<EnemyAgent> candidates)
        {
            Tower = tower;
            Stats = stats;
            Priority = priority;
            Candidates = candidates;
        }

        public TowerAgent Tower { get; }
        public TowerResolvedStats Stats { get; }
        public TargetPriority Priority { get; }
        public IReadOnlyList<EnemyAgent> Candidates { get; }
    }

    public interface ITargetingStrategy
    {
        EnemyAgent SelectTarget(TargetingContext context);
    }

    public interface ITargetFilter
    {
        bool IsTargetValid(TowerAgent tower, EnemyAgent enemy);
    }

    public interface IStatModifier
    {
        void ModifyStats(TowerAgent tower, ref TowerResolvedStats stats);
    }

    public interface IAttackExecution
    {
        void Tick(float deltaTime);
        void Shutdown();
    }

    public abstract class TowerTargetFilterDef : ScriptableObject, ITargetFilter
    {
        public abstract bool IsTargetValid(TowerAgent tower, EnemyAgent enemy);
    }

    public abstract class TowerStatModifierDef : ScriptableObject, IStatModifier
    {
        public abstract void ModifyStats(TowerAgent tower, ref TowerResolvedStats stats);
    }

    [CreateAssetMenu(menuName = "Towers/Modifiers/Flat Stat Modifier", fileName = "TowerFlatStatModifier")]
    public class TowerFlatStatModifierDef : TowerStatModifierDef
    {
        [Header("Additive")]
        public float healthAdd;
        public float rangeAdd;
        public float fireIntervalAdd;
        public float damageAdd;

        [Header("Multipliers")]
        public float healthMultiplier = 1f;
        public float rangeMultiplier = 1f;
        public float fireIntervalMultiplier = 1f;
        public float damageMultiplier = 1f;

        public override void ModifyStats(TowerAgent tower, ref TowerResolvedStats stats)
        {
            stats.MaxHealth = (stats.MaxHealth + healthAdd) * Mathf.Max(0f, healthMultiplier);
            stats.Range = (stats.Range + rangeAdd) * Mathf.Max(0f, rangeMultiplier);
            stats.FireInterval = (stats.FireInterval + fireIntervalAdd) * Mathf.Max(0f, fireIntervalMultiplier);
            stats.Damage = (stats.Damage + damageAdd) * Mathf.Max(0f, damageMultiplier);
            stats.Clamp();
        }
    }
}
