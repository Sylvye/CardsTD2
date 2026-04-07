using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Enemies
{
    [ExecuteAlways] // makes it run during edit mode
    public class EnemyPath : MonoBehaviour
    {
        [SerializeField] private SplineContainer splineContainer;
        [SerializeField, Min(8)] private int sampleCount = 128;

        private readonly List<Vector3> sampledPoints = new();
        private readonly List<float> cumulativeLengths = new();

        public SplineContainer SplineContainer => splineContainer;
        public float TotalLength { get; private set; }

        private void Awake()
        {
            RebuildCache();
        }

        private void OnValidate()
        {
            sampleCount = Mathf.Max(8, sampleCount);
            RebuildCache();
        }

        public void RebuildCache()
        {
            sampledPoints.Clear();
            cumulativeLengths.Clear();
            TotalLength = 0f;

            if (splineContainer is null)
                return;

            Vector3 previous = GetPositionAtNormalizedInternal(0f);
            sampledPoints.Add(previous);
            cumulativeLengths.Add(0f);

            for (int i = 1; i <= sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                Vector3 current = GetPositionAtNormalizedInternal(t);

                TotalLength += Vector3.Distance(previous, current);

                sampledPoints.Add(current);
                cumulativeLengths.Add(TotalLength);

                previous = current;
            }
        }

        public Vector2 GetPositionAtNormalized(float t)
        {
            if (splineContainer is null)
                return transform.position;

            t = Mathf.Clamp01(t);
            return GetPositionAtNormalizedInternal(t);
        }

        public Vector2 GetPositionAtDistance(float distance)
        {
            if (splineContainer is null)
                return transform.position;

            if (sampledPoints.Count < 2 || cumulativeLengths.Count != sampledPoints.Count)
                RebuildCache();

            if (TotalLength <= 0f)
                return sampledPoints.Count > 0 ? sampledPoints[0] : (Vector2)transform.position;

            distance = Mathf.Clamp(distance, 0f, TotalLength);

            for (int i = 1; i < cumulativeLengths.Count; i++)
            {
                float segmentEnd = cumulativeLengths[i];

                if (distance <= segmentEnd)
                {
                    float segmentStart = cumulativeLengths[i - 1];
                    float segmentLength = segmentEnd - segmentStart;

                    if (segmentLength <= Mathf.Epsilon)
                        return sampledPoints[i];

                    float lerp = (distance - segmentStart) / segmentLength;
                    return Vector2.Lerp(sampledPoints[i - 1], sampledPoints[i], lerp);
                }
            }

            return sampledPoints[^1];
        }

        public Vector2 GetTangentAtDistance(float distance)
        {
            if (splineContainer is null)
                return Vector2.right;

            if (TotalLength <= 0f)
                RebuildCache();

            if (TotalLength <= 0f)
                return Vector2.right;

            float normalized = Mathf.Clamp01(distance / TotalLength);
            float3 tangent = splineContainer.EvaluateTangent(normalized);

            Vector2 result = new(tangent.x, tangent.y);
            return result.sqrMagnitude > 0.0001f ? result.normalized : Vector2.right;
        }

        private Vector3 GetPositionAtNormalizedInternal(float t)
        {
            float3 position = splineContainer.EvaluatePosition(t);
            return new Vector3(position.x, position.y, 0f);
        }
    }
}