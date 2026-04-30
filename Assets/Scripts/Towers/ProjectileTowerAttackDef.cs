using UnityEngine;

namespace Towers
{
    [System.Serializable]
    public class ProjectileTrailSettings
    {
        public bool enabled = false;
        [Min(0.01f)] public float duration = 0.2f;
        [Min(0f)] public float startWidth = 0.12f;
        [Min(0f)] public float endWidth = 0f;
        public Color startColor = Color.white;
        public Color endColor = new(1f, 1f, 1f, 0f);
        public Material material;
        [Min(2)] public int maxPoints = 32;
        [Min(0f)] public float minPointDistance = 0.05f;
        public string sortingLayerName = "Default";
        public int sortingOrder = 0;

        internal void ClampValues()
        {
            duration = Mathf.Max(0.01f, duration);
            startWidth = Mathf.Max(0f, startWidth);
            endWidth = Mathf.Max(0f, endWidth);
            maxPoints = Mathf.Max(2, maxPoints);
            minPointDistance = Mathf.Max(0f, minPointDistance);
        }
    }

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
        public ProjectileTrailSettings trail = new();

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
            trail ??= new ProjectileTrailSettings();
            trail.ClampValues();
        }

        private void OnValidate()
        {
            ClampSpreadToMinimum();
        }
    }
}
