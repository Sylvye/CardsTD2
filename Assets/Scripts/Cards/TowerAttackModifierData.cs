using System;
using Towers;
using UnityEngine;

namespace Cards
{
    [Serializable]
    public class TowerAttackModifierData
    {
        [Tooltip("Leave empty to target every supported tower attack on the card.")]
        public string attackNameContains;

        [Header("Projectile")]
        public int projectileCountDelta;
        public float projectileCountMultiplier = 1f;
        public float damageBonusDelta;
        public int pierceDelta;
        public float degreesSpreadDelta;
        public float projectileSpeedDelta;
        public float projectileLifetimeDelta;

        [Header("Beam")]
        public int beamProjectileCountDelta;
        public float beamProjectileCountMultiplier = 1f;
        public float flatDamageBonusDelta;
        public float damageMultiplierDelta;

        [Header("Summon")]
        public int maxActiveSummonsDelta;
        public float spawnRadiusDelta;
        public float summonLifetimeDelta;

        [Header("Shared")]
        public float splashRadiusDelta;

        public bool Matches(TowerAttackDef attackDef)
        {
            if (attackDef == null)
                return false;

            if (string.IsNullOrWhiteSpace(attackNameContains))
                return true;

            return attackDef.name.IndexOf(attackNameContains, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void ApplyTo(TowerAttackDef attackDef)
        {
            attackDef.AdjustSplashRadius(splashRadiusDelta);

            if (attackDef is ProjectileTowerAttackDef projectileAttack)
            {
                float projectileCount = (projectileAttack.projectileCount + projectileCountDelta) * Mathf.Max(0f, projectileCountMultiplier);
                projectileAttack.projectileCount = Mathf.Max(1, Mathf.RoundToInt(projectileCount));
                projectileAttack.damageBonus = Mathf.Max(0f, projectileAttack.damageBonus + damageBonusDelta);
                projectileAttack.pierceCount = Mathf.Max(0, projectileAttack.pierceCount + pierceDelta);
                projectileAttack.AdjustDegreesSpread(degreesSpreadDelta);
                projectileAttack.projectileSpeed = Mathf.Max(0.01f, projectileAttack.projectileSpeed + projectileSpeedDelta);
                projectileAttack.projectileLifetime = Mathf.Max(0.01f, projectileAttack.projectileLifetime + projectileLifetimeDelta);
                return;
            }

            if (attackDef is BeamTowerAttackDef beamAttack)
            {
                float beamCount = (beamAttack.projectileCount + beamProjectileCountDelta) * Mathf.Max(0f, beamProjectileCountMultiplier);
                beamAttack.projectileCount = Mathf.Max(1, Mathf.RoundToInt(beamCount));
                beamAttack.flatDamageBonus = Mathf.Max(0f, beamAttack.flatDamageBonus + flatDamageBonusDelta);
                beamAttack.damageMultiplier = Mathf.Max(0f, beamAttack.damageMultiplier + damageMultiplierDelta);
                return;
            }

            if (attackDef is SummonTowerAttackDef summonAttack)
            {
                summonAttack.maxActiveSummons = Mathf.Max(1, summonAttack.maxActiveSummons + maxActiveSummonsDelta);
                summonAttack.spawnRadius = Mathf.Max(0f, summonAttack.spawnRadius + spawnRadiusDelta);
                summonAttack.summonLifetime = Mathf.Max(0.01f, summonAttack.summonLifetime + summonLifetimeDelta);
            }
        }
    }
}
