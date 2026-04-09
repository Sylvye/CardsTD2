using System.Collections.Generic;
using UnityEngine;

namespace Towers
{
    public abstract class TowerAttackDef : ScriptableObject
    {
        [SerializeField] private List<TowerTargetFilterDef> targetFilters = new();

        public IReadOnlyList<TowerTargetFilterDef> TargetFilters => targetFilters;

        public abstract IAttackExecution CreateExecution(TowerAgent tower);
    }

    [CreateAssetMenu(menuName = "Towers/Attacks/Projectile Attack", fileName = "ProjectileTowerAttack")]
    public class ProjectileTowerAttackDef : TowerAttackDef
    {
        public TowerProjectile projectilePrefab;
        [Min(0f)] public float damageBonus;
        [Min(0.01f)] public float projectileSpeed = 8f;
        [Min(0.01f)] public float hitRadius = 0.2f;
        [Min(0.01f)] public float projectileLifetime = 4f;
        public Vector3 fireOffset;

        public override IAttackExecution CreateExecution(TowerAgent tower)
        {
            return new ProjectileAttackExecution(tower, this);
        }
    }

    [CreateAssetMenu(menuName = "Towers/Attacks/Beam Attack", fileName = "BeamTowerAttack")]
    public class BeamTowerAttackDef : TowerAttackDef
    {
        [Min(0.01f)] public float tickInterval = 0.2f;
        [Min(0f)] public float damageMultiplier = 1f;
        [Min(0f)] public float flatDamageBonus;

        public override IAttackExecution CreateExecution(TowerAgent tower)
        {
            return new BeamAttackExecution(tower, this);
        }
    }

    [CreateAssetMenu(menuName = "Towers/Attacks/Summon Attack", fileName = "SummonTowerAttack")]
    public class SummonTowerAttackDef : TowerAttackDef
    {
        public TowerSummonedUnit summonPrefab;
        [Min(1)] public int maxActiveSummons = 1;
        [Min(0f)] public float spawnRadius = 0.5f;
        [Min(0.01f)] public float summonLifetime = 8f;
        [Min(0f)] public float summonRange = 2.5f;
        [Min(0f)] public float summonDamage = 1f;
        [Min(0.01f)] public float summonFireInterval = 0.75f;

        public override IAttackExecution CreateExecution(TowerAgent tower)
        {
            return new SummonAttackExecution(tower, this);
        }
    }
}
