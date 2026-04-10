using UnityEngine;

namespace Towers
{
    [CreateAssetMenu(menuName = "Towers/Attacks/Projectile Attack", fileName = "ProjectileTowerAttack")]
    public class ProjectileTowerAttackDef : TowerAttackDef
    {
        public TowerProjectile projectilePrefab;
        [Min(1)] public int projectileCount = 1;
        [Min(0f)] public float damageBonus;
        [Min(0.01f)] public float projectileSpeed = 8f;
        [Min(0.01f)] public float projectileLifetime = 4f;
        [Min(1)] public int pierceCount = 1;
        [Min(0f)] public float degreesSpread = 0f;
        public Vector3 fireOffset;
        public bool followTarget = false;

        public override IAttackExecution CreateExecution(TowerAgent tower)
        {
            return new ProjectileAttackExecution(tower, this);
        }
    }
}
