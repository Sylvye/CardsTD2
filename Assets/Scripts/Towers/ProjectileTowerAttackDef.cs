using UnityEngine;

namespace Towers
{
    [CreateAssetMenu(menuName = "Towers/Attacks/Projectile Attack", fileName = "ProjectileTowerAttack")]
    public class ProjectileTowerAttackDef : TowerAttackDef
    {
        public TowerProjectile projectilePrefab;
        [Min(0f)] public float damageBonus;
        [Min(0.01f)] public float projectileSpeed = 8f;
        [Min(0.01f)] public float hitRadius = 0.2f;
        [Min(0.01f)] public float projectileLifetime = 4f;
        public Vector3 fireOffset;
        public bool followTarget = false;

        public override IAttackExecution CreateExecution(TowerAgent tower)
        {
            return new ProjectileAttackExecution(tower, this);
        }
    }
}
