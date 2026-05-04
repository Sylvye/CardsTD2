using System.Collections.Generic;
using Combat;
using Enemies;
using UnityEngine;

namespace Towers
{
    internal static class TowerHitResolver
    {
        public static EnemyDamageResult ApplyHit(
            TowerAgent tower,
            TowerAttackDef attackDef,
            EnemyAgent enemy,
            float damage,
            DamageTypeDef damageType,
            Vector3 hitPosition)
        {
            if (enemy == null || enemy.IsDeadOrEscaped)
                return new EnemyDamageResult(0f, EnemyDamageResponseType.Normal, false);

            EnemyDamageResult damageResult = enemy.TakeDamage(damage, damageType);
            tower?.ReportHit(enemy, damageResult.AppliedAmount, hitPosition);

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
    }
}
