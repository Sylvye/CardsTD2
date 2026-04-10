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
            if (targetDirection.sqrMagnitude < 0.0001f)
            {
                targetDirection = Vector3.right;
            }

            targetDirection.Normalize();
            float spreadDegrees = Mathf.Max(0f, attackDef.degreesSpread);
            float centerIndex = (projectileCount - 1) * 0.5f;

            for (int i = 0; i < projectileCount; i++)
            {
                TowerProjectile projectile = CreateProjectile();
                float angleOffset = projectileCount > 1
                    ? ((i - centerIndex) / centerIndex) * spreadDegrees * 0.5f
                    : 0f;
                Vector3 projectileDirection = Quaternion.Euler(0f, 0f, angleOffset) * targetDirection;
                projectile.transform.position = firePosition;
                projectile.Initialize(
                    tower,
                    target,
                    projectileDirection,
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
        private readonly ITargetingStrategy targetingStrategy = new PriorityTargetingStrategy();

        private readonly List<EnemyAgent> currentTargets = new();
        private readonly List<EnemyAgent> candidateTargets = new();
        private readonly List<LineRenderer> beamRendererInstances = new();
        private float tickTimer;

        public BeamAttackExecution(TowerAgent tower, BeamTowerAttackDef attackDef)
        {
            this.tower = tower;
            this.attackDef = attackDef;
        }

        public void Tick(float deltaTime)
        {
            AcquireTargets();
            if (currentTargets.Count == 0)
            {
                SetAllBeamsEnabled(false);
                tickTimer = 0f;
                return;
            }

            UpdateBeamRenderers();

            tickTimer -= deltaTime;
            if (tickTimer > 0f)
                return;

            TowerResolvedStats stats = tower.GetResolvedStats();
            float damage = (stats.Damage * attackDef.damageMultiplier) + attackDef.flatDamageBonus;
            for (int i = 0; i < currentTargets.Count; i++)
            {
                EnemyAgent target = currentTargets[i];
                if (!tower.IsTargetValid(target, attackDef.TargetFilters))
                    continue;

                bool wasAliveBeforeHit = !target.IsDeadOrEscaped;
                target.TakeDamage(damage);
                tower.ReportHit(target, damage, target.transform.position);
                if (wasAliveBeforeHit && target.IsDeadOrEscaped)
                    tower.ReportKill(target, damage, target.transform.position);
            }

            tickTimer = Mathf.Max(0.01f, attackDef.tickInterval);
        }

        public void Shutdown()
        {
            SetAllBeamsEnabled(false);
            for (int i = 0; i < beamRendererInstances.Count; i++)
            {
                LineRenderer renderer = beamRendererInstances[i];
                if (renderer != null)
                    Object.Destroy(renderer.gameObject);
            }

            beamRendererInstances.Clear();
            currentTargets.Clear();
            candidateTargets.Clear();
        }

        private void AcquireTargets()
        {
            currentTargets.Clear();

            EnemyManager enemyManager = tower.RuntimeContext.EnemyManager;
            if (enemyManager == null)
                return;

            IReadOnlyList<EnemyAgent> activeEnemies = enemyManager.ActiveEnemies;
            candidateTargets.Clear();
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                EnemyAgent enemy = activeEnemies[i];
                if (tower.IsTargetValid(enemy, attackDef.TargetFilters))
                    candidateTargets.Add(enemy);
            }

            if (candidateTargets.Count == 0)
                return;

            TowerResolvedStats stats = tower.GetResolvedStats();
            int beamsToFire = Mathf.Max(1, attackDef.projectileCount);
            while (currentTargets.Count < beamsToFire && candidateTargets.Count > 0)
            {
                EnemyAgent target = targetingStrategy.SelectTarget(
                    new TargetingContext(tower, stats, tower.CurrentPriority, candidateTargets)
                );
                if (target == null)
                    break;

                currentTargets.Add(target);
                candidateTargets.Remove(target);
            }
        }

        private void UpdateBeamRenderers()
        {
            Vector3 origin = tower.transform.position;
            for (int i = 0; i < currentTargets.Count; i++)
            {
                LineRenderer renderer = EnsureBeamRenderer(i);
                EnemyAgent target = currentTargets[i];
                if (renderer == null || target == null)
                    continue;

                renderer.SetPosition(0, origin);
                renderer.SetPosition(1, target.transform.position);
                renderer.enabled = true;
            }

            for (int i = currentTargets.Count; i < beamRendererInstances.Count; i++)
            {
                if (beamRendererInstances[i] != null)
                    beamRendererInstances[i].enabled = false;
            }
        }

        private LineRenderer EnsureBeamRenderer(int index)
        {
            while (beamRendererInstances.Count <= index)
                beamRendererInstances.Add(null);

            if (beamRendererInstances[index] != null)
                return beamRendererInstances[index];

            LineRenderer instance;
            if (attackDef.beamRendererPrefab != null)
            {
                instance = Object.Instantiate(attackDef.beamRendererPrefab, tower.transform);
                instance.positionCount = Mathf.Max(2, instance.positionCount);
            }
            else
            {
                GameObject fallback = new($"BeamRenderer_{index}");
                fallback.transform.SetParent(tower.transform, false);
                instance = fallback.AddComponent<LineRenderer>();
                instance.positionCount = 2;
                instance.useWorldSpace = true;
                instance.widthMultiplier = 0.1f;
                instance.material = new Material(Shader.Find("Sprites/Default"));
                instance.startColor = Color.cyan;
                instance.endColor = Color.cyan;
            }

            instance.enabled = false;
            beamRendererInstances[index] = instance;
            return instance;
        }

        private void SetAllBeamsEnabled(bool enabled)
        {
            for (int i = 0; i < beamRendererInstances.Count; i++)
            {
                if (beamRendererInstances[i] != null)
                    beamRendererInstances[i].enabled = enabled;
            }
        }
    }

    internal sealed class SummonAttackExecution : IAttackExecution
    {
        private readonly TowerAgent tower;
        private readonly SummonTowerAttackDef attackDef;
        private readonly List<ITowerSummon> activeSummons = new();

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

            ITowerSummon summon = CreateSummon();
            MonoBehaviour summonBehaviour = summon as MonoBehaviour;
            if (summonBehaviour == null)
                return;

            Vector2 offset = Random.insideUnitCircle * attackDef.spawnRadius;
            summonBehaviour.transform.position = tower.transform.position + new Vector3(offset.x, offset.y, 0f);
            summon.Initialize(new TowerSummonContext(
                tower,
                tower.RuntimeContext,
                attackDef.summonLifetime
            ));

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

        private ITowerSummon CreateSummon()
        {
            if (attackDef.summonPrefab != null)
            {
                MonoBehaviour summonPrefabInstance = Object.Instantiate(attackDef.summonPrefab);
                SummonedTowerAgent summonHost = summonPrefabInstance.GetComponent<SummonedTowerAgent>();
                if (summonHost != null)
                {
                    summonHost.ConfigureSummon(attackDef.summonTowerDef, attackDef.summonAttacks);
                    return summonHost;
                }

                ITowerSummon typedSummon = summonPrefabInstance as ITowerSummon;
                if (typedSummon != null)
                    return typedSummon;

                Object.Destroy(summonPrefabInstance.gameObject);
            }

            GameObject fallback = new("TowerSummon");
            SummonedTowerAgent fallbackHost = fallback.AddComponent<SummonedTowerAgent>();
            fallbackHost.ConfigureSummon(attackDef.summonTowerDef, attackDef.summonAttacks);
            return fallbackHost;
        }
    }
}
