using UnityEngine;

namespace Towers
{
    internal static class ProjectileSpreadUtility
    {
        public static float GetAngleOffsetDegrees(int projectileIndex, int projectileCount, float degreesSpread)
        {
            int clampedProjectileCount = Mathf.Max(1, projectileCount);
            if (clampedProjectileCount == 1)
                return 0f;

            float spacingDegrees = Mathf.Max(0f, degreesSpread);
            float centerIndex = (clampedProjectileCount - 1) * 0.5f;
            return (projectileIndex - centerIndex) * spacingDegrees;
        }
    }
}
