using UnityEngine;

namespace Cards
{
    public static class SpawnableColliderUtility
    {
        public static float GetPreviewRadius(SpawnableObjectDef spawnableObject)
        {
            if (spawnableObject == null || spawnableObject.prefab == null)
                return 0f;

            Collider2D collider = spawnableObject.prefab.GetComponent<Collider2D>();
            if (collider == null)
                return 0f;

            Vector2 scale = GetAbsoluteScale(collider.transform);

            return collider switch
            {
                CircleCollider2D circleCollider => GetCircleRadius(circleCollider, scale),
                BoxCollider2D boxCollider => GetBoxRadius(boxCollider, scale),
                CapsuleCollider2D capsuleCollider => GetCapsuleRadius(capsuleCollider, scale),
                PolygonCollider2D polygonCollider => GetPolygonRadius(polygonCollider, scale),
                _ => 0f
            };
        }

        private static Vector2 GetAbsoluteScale(Transform transform)
        {
            Vector3 lossyScale = transform.lossyScale;
            return new Vector2(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y));
        }

        private static float GetCircleRadius(CircleCollider2D circleCollider, Vector2 scale)
        {
            Vector2 scaledOffset = Vector2.Scale(circleCollider.offset, scale);
            float scaledRadius = circleCollider.radius * Mathf.Max(scale.x, scale.y);
            return scaledOffset.magnitude + scaledRadius;
        }

        private static float GetBoxRadius(BoxCollider2D boxCollider, Vector2 scale)
        {
            Vector2 scaledOffset = Vector2.Scale(boxCollider.offset, scale);
            Vector2 scaledHalfSize = Vector2.Scale(boxCollider.size * 0.5f, scale);
            return scaledOffset.magnitude + scaledHalfSize.magnitude;
        }

        private static float GetCapsuleRadius(CapsuleCollider2D capsuleCollider, Vector2 scale)
        {
            Vector2 scaledOffset = Vector2.Scale(capsuleCollider.offset, scale);
            Vector2 scaledHalfSize = Vector2.Scale(capsuleCollider.size * 0.5f, scale);
            return scaledOffset.magnitude + scaledHalfSize.magnitude;
        }

        private static float GetPolygonRadius(PolygonCollider2D polygonCollider, Vector2 scale)
        {
            float maxDistance = 0f;

            foreach (Vector2 point in polygonCollider.points)
            {
                Vector2 scaledPoint = Vector2.Scale(point + polygonCollider.offset, scale);
                maxDistance = Mathf.Max(maxDistance, scaledPoint.magnitude);
            }

            return maxDistance;
        }
    }
}
