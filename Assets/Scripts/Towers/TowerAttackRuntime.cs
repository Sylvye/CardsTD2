using System.Collections.Generic;
using Enemies;
using UnityEngine;

namespace Towers
{
    internal sealed class PriorityTargetingStrategy : ITargetingStrategy
    {
        public EnemyAgent SelectTarget(TargetingContext context)
        {
            EnemyAgent bestTarget = null;

            foreach (EnemyAgent candidate in context.Candidates)
            {
                if (candidate == null)
                    continue;

                if (bestTarget == null || IsBetterCandidate(candidate, bestTarget, context.Priority))
                    bestTarget = candidate;
            }

            return bestTarget;
        }

        private static bool IsBetterCandidate(EnemyAgent candidate, EnemyAgent currentBest, TargetPriority priority)
        {
            switch (priority)
            {
                case TargetPriority.Last:
                    if (candidate.TrackDistance < currentBest.TrackDistance)
                        return true;

                    if (Mathf.Approximately(candidate.TrackDistance, currentBest.TrackDistance))
                        return candidate.CurrentHealth > currentBest.CurrentHealth;

                    return false;

                case TargetPriority.Strong:
                    if (candidate.CurrentHealth > currentBest.CurrentHealth)
                        return true;

                    if (Mathf.Approximately(candidate.CurrentHealth, currentBest.CurrentHealth))
                        return candidate.TrackDistance > currentBest.TrackDistance;

                    return false;

                case TargetPriority.First:
                default:
                    if (candidate.TrackDistance > currentBest.TrackDistance)
                        return true;

                    if (Mathf.Approximately(candidate.TrackDistance, currentBest.TrackDistance))
                        return candidate.CurrentHealth > currentBest.CurrentHealth;

                    return false;
            }
        }
    }

    internal sealed class ProjectileAttackExecution : IAttackExecution
    {
        private readonly TowerAgent tower;
        private readonly ProjectileTowerAttackDef attackDef;
        private float cooldownRemaining;

        public ProjectileAttackExecution(TowerAgent tower, ProjectileTowerAttackDef attackDef)
        {
            this.tower = tower;
            this.attackDef = attackDef;
        }

        public void Tick(float deltaTime)
        {
            cooldownRemaining -= deltaTime;
            if (cooldownRemaining > 0f)
                return;

            EnemyAgent target = tower.AcquireTarget(attackDef.TargetFilters);
            if (target == null)
                return;

            TowerResolvedStats stats = tower.GetResolvedStats();
            float damage = stats.Damage + attackDef.damageBonus;
            cooldownRemaining = stats.FireInterval;

            int projectileCount = Mathf.Max(1, attackDef.projectileCount);
            Vector3 firePosition = tower.transform.position + attackDef.fireOffset;
            Vector3 targetDirection = target.transform.position - firePosition;
            float targetDistance = targetDirection.magnitude;
            if (targetDistance < 0.0001f)
            {
                targetDirection = Vector3.right;
                targetDistance = 1f;
            }

            targetDirection.Normalize();
            Vector3 perpendicular = new Vector3(-targetDirection.y, targetDirection.x, 0f);
            float spreadDegrees = Mathf.Max(0f, attackDef.degreesSpread);
            float centerIndex = (projectileCount - 1) * 0.5f;

            for (int i = 0; i < projectileCount; i++)
            {
                TowerProjectile projectile = CreateProjectile();
                float normalizedIndex = projectileCount > 1 ? (i - centerIndex) / centerIndex : 0f;
                float projectileAngle = normalizedIndex * spreadDegrees * 0.5f;
                float laneOffset = Mathf.Tan(projectileAngle * Mathf.Deg2Rad) * targetDistance;
                projectile.transform.position = firePosition + (perpendicular * laneOffset);
                projectile.Initialize(
                    tower,
                    target,
                    damage,
                    attackDef.projectileSpeed,
                    attackDef.hitRadius,
                    attackDef.projectileLifetime,
                    attackDef.followTarget
                );
            }
        }

        public void Shutdown()
        {
        }

        private TowerProjectile CreateProjectile()
        {
            if (attackDef.projectilePrefab != null)
                return Object.Instantiate(attackDef.projectilePrefab);

            GameObject fallback = new("TowerProjectile");
            return fallback.AddComponent<TowerProjectile>();
        }
    }

    internal sealed class BeamAttackExecution : IAttackExecution
    {
        private readonly TowerAgent tower;
        private readonly BeamTowerAttackDef attackDef;

        private EnemyAgent currentTarget;
        private float tickTimer;

        public BeamAttackExecution(TowerAgent tower, BeamTowerAttackDef attackDef)
        {
            this.tower = tower;
            this.attackDef = attackDef;
        }

        public void Tick(float deltaTime)
        {
            if (!tower.IsTargetValid(currentTarget, attackDef.TargetFilters))
                currentTarget = tower.AcquireTarget(attackDef.TargetFilters);

            if (currentTarget == null)
            {
                tickTimer = 0f;
                return;
            }

            tickTimer -= deltaTime;
            if (tickTimer > 0f)
                return;

            TowerResolvedStats stats = tower.GetResolvedStats();
            float damage = (stats.Damage * attackDef.damageMultiplier) + attackDef.flatDamageBonus;
            bool wasAliveBeforeHit = !currentTarget.IsDeadOrEscaped;
            currentTarget.TakeDamage(damage);
            tower.ReportHit(currentTarget, damage, currentTarget.transform.position);
            if (wasAliveBeforeHit && currentTarget.IsDeadOrEscaped)
                tower.ReportKill(currentTarget, damage, currentTarget.transform.position);
            tickTimer = Mathf.Max(0.01f, attackDef.tickInterval);
        }

        public void Shutdown()
        {
            currentTarget = null;
        }
    }

    internal sealed class SummonAttackExecution : IAttackExecution
    {
        private readonly TowerAgent tower;
        private readonly SummonTowerAttackDef attackDef;
        private readonly List<TowerSummonedUnit> activeSummons = new();

        private float cooldownRemaining;

        public SummonAttackExecution(TowerAgent tower, SummonTowerAttackDef attackDef)
        {
            this.tower = tower;
            this.attackDef = attackDef;
        }

        public void Tick(float deltaTime)
        {
            CleanupSummons();

            cooldownRemaining -= deltaTime;
            if (cooldownRemaining > 0f)
                return;

            if (activeSummons.Count >= Mathf.Max(1, attackDef.maxActiveSummons))
                return;

            EnemyAgent target = tower.AcquireTarget(attackDef.TargetFilters);
            if (target == null)
                return;

            TowerResolvedStats towerStats = tower.GetResolvedStats();
            cooldownRemaining = towerStats.FireInterval;

            TowerSummonedUnit summon = CreateSummon();
            Vector2 offset = Random.insideUnitCircle * attackDef.spawnRadius;
            summon.transform.position = tower.transform.position + new Vector3(offset.x, offset.y, 0f);
            summon.Initialize(
                tower.RuntimeContext.EnemyManager,
                attackDef.summonLifetime,
                attackDef.summonRange,
                attackDef.summonDamage + towerStats.Damage,
                attackDef.summonFireInterval
            );

            activeSummons.Add(summon);
        }

        public void Shutdown()
        {
            activeSummons.Clear();
        }

        private void CleanupSummons()
        {
            for (int i = activeSummons.Count - 1; i >= 0; i--)
            {
                if (activeSummons[i] == null || !activeSummons[i].IsAlive)
                    activeSummons.RemoveAt(i);
            }
        }

        private TowerSummonedUnit CreateSummon()
        {
            if (attackDef.summonPrefab != null)
                return Object.Instantiate(attackDef.summonPrefab);

            GameObject fallback = new("TowerSummon");
            return fallback.AddComponent<TowerSummonedUnit>();
        }
    }
}
