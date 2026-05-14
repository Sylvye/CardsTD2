using System.Collections.Generic;
using Combat;
using Enemies;
using UnityEngine;

namespace Towers
{
    internal static class TowerHitResolver
    {
        private readonly struct SplashTargetCandidate
        {
            public SplashTargetCandidate(EnemyAgent enemy, float distanceSqr)
            {
                Enemy = enemy;
                DistanceSqr = distanceSqr;
            }

            public EnemyAgent Enemy { get; }
            public float DistanceSqr { get; }
        }

        public static EnemyDamageResult ApplyHit(
            TowerAgent tower,
            TowerAttackDef attackDef,
            EnemyAgent enemy,
            float damage,
            DamageTypeDef damageType,
            Vector3 hitPosition)
        {
            return ApplyHit(
                tower,
                attackDef,
                enemy,
                damage,
                damageType,
                hitPosition,
                allowSplash: true);
        }

        private static EnemyDamageResult ApplyHit(
            TowerAgent tower,
            TowerAttackDef attackDef,
            EnemyAgent enemy,
            float damage,
            DamageTypeDef damageType,
            Vector3 hitPosition,
            bool allowSplash)
        {
            if (enemy == null || enemy.IsDeadOrEscaped)
                return new EnemyDamageResult(0f, EnemyDamageResponseType.Normal, false);

            EnemyDamageResult damageResult = enemy.TakeDamage(damage, damageType);
            tower?.ReportHit(enemy, damageResult.AppliedAmount, hitPosition);
            if (allowSplash)
                ApplySplashDamage(tower, attackDef, enemy, damage, damageType, hitPosition);

            if (damageResult.WasKill)
            {
                tower?.ReportKill(enemy, damageResult.AppliedAmount, hitPosition);
                return damageResult;
            }

            if (damageResult.AppliedAmount > 0f && damageResult.ResponseType != EnemyDamageResponseType.Resistance)
                ApplyOnHitStatusEffects(attackDef, enemy);

            return damageResult;
        }

        private static void ApplyOnHitStatusEffects(TowerAttackDef attackDef, EnemyAgent enemy)
        {
            if (attackDef?.OnHitStatusEffects == null || enemy == null || enemy.IsDeadOrEscaped)
                return;

            IReadOnlyList<EnemyStatusEffectApplication> statusEffects = attackDef.OnHitStatusEffects;
            for (int i = 0; i < statusEffects.Count; i++)
                enemy.ApplyStatusEffect(statusEffects[i]);
        }

        private static void ApplySplashDamage(
            TowerAgent tower,
            TowerAttackDef attackDef,
            EnemyAgent primaryTarget,
            float damage,
            DamageTypeDef damageType,
            Vector3 hitPosition)
        {
            if (attackDef == null || !attackDef.SupportsHitPointSplash)
                return;

            float radius = attackDef.SplashRadius;
            if (radius <= 0f)
                return;

            SpawnSplashImpactEffect(attackDef, hitPosition, radius);

            EnemyManager enemyManager = tower?.RuntimeContext.EnemyManager;
            if (enemyManager == null || damage <= 0f)
                return;

            int maxTargets = attackDef.SplashMaxTargets;
            if (maxTargets <= 0)
                return;

            float radiusSqr = radius * radius;
            IReadOnlyList<EnemyAgent> activeEnemies = enemyManager.ActiveEnemies;
            List<SplashTargetCandidate> candidates = new();
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                EnemyAgent candidate = activeEnemies[i];
                if (candidate == null || candidate.IsDeadOrEscaped || candidate == primaryTarget)
                    continue;

                float distanceSqr = (candidate.transform.position - hitPosition).sqrMagnitude;
                if (distanceSqr > radiusSqr)
                    continue;

                candidates.Add(new SplashTargetCandidate(candidate, distanceSqr));
            }

            candidates.Sort((a, b) => a.DistanceSqr.CompareTo(b.DistanceSqr));
            int splashTargetCount = Mathf.Min(maxTargets, candidates.Count);
            for (int i = 0; i < splashTargetCount; i++)
            {
                ApplyHit(
                    tower,
                    attackDef,
                    candidates[i].Enemy,
                    damage,
                    damageType,
                    candidates[i].Enemy.transform.position,
                    allowSplash: false);
            }
        }

        private static void SpawnSplashImpactEffect(TowerAttackDef attackDef, Vector3 hitPosition, float radius)
        {
            GameObject effectPrefab = attackDef.SplashImpactEffectPrefab;
            if (effectPrefab == null)
                return;

            GameObject effectInstance = Object.Instantiate(effectPrefab, hitPosition, Quaternion.identity);
            SplashImpactFx effectRuntime = effectInstance.GetComponent<SplashImpactFx>();
            if (effectRuntime != null)
            {
                effectRuntime.Initialize(radius);
                return;
            }

            effectInstance.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            DestroyEffectInstance(effectInstance, 0.5f);
        }

        private static void DestroyEffectInstance(GameObject effectInstance, float delay)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(effectInstance);
                return;
            }
#endif
            Object.Destroy(effectInstance, delay);
        }
    }
}
