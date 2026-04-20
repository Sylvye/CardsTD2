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
        [Min(0)] public int pierceCount = 0;
        [SerializeField, Min(0f), Tooltip("Angular spacing in degrees between adjacent projectiles.")]
        private float degreesSpread = 0f;
        [SerializeField, Min(0f), Tooltip("Minimum allowed projectile spacing. Designers can set this in the editor, but gameplay code cannot change it.")]
        private float minimumDegreesSpread = 0f;
        public Vector3 fireOffset;
        public bool followTarget = false;

        public float DegreesSpread => Mathf.Max(MinimumDegreesSpread, degreesSpread);
        public float MinimumDegreesSpread => Mathf.Max(0f, minimumDegreesSpread);

        public override IAttackExecution CreateExecution(TowerAgent tower)
        {
            ClampSpreadToMinimum();
            return new ProjectileAttackExecution(tower, this);
        }

        internal void AdjustDegreesSpread(float delta)
        {
            degreesSpread = Mathf.Max(MinimumDegreesSpread, degreesSpread + delta);
        }

        internal void ClampSpreadToMinimum()
        {
            minimumDegreesSpread = Mathf.Max(0f, minimumDegreesSpread);
            degreesSpread = Mathf.Max(minimumDegreesSpread, degreesSpread);
        }

        private void OnValidate()
        {
            ClampSpreadToMinimum();
        }
    }
}
