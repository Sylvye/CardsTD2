using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Enemies
{
    [ExecuteAlways]
    [RequireComponent(typeof(LineRenderer), typeof(EdgeCollider2D))]
    public sealed class SplinePathRenderer : MonoBehaviour
    {
        [SerializeField] private SplineContainer splineContainer;
        [SerializeField, Min(2)] private int lineRendererSampleCount = 128;
        [SerializeField, Min(2)] private int colliderSampleCount = 128;
        [SerializeField, Min(0.01f)] private float width = 0.6f;
        [SerializeField] private bool rebuildEveryFrameInEditMode = true;

        private LineRenderer lineRenderer;
        private EdgeCollider2D edgeCollider;

        private void Awake()
        {
            EnsureReferences();
            ApplyRendererDefaults();
            Rebuild();
        }

        private void Reset()
        {
            EnsureReferences();
            ApplyRendererDefaults();
            Rebuild();
        }

        private void OnEnable()
        {
            EnsureReferences();
            ApplyRendererDefaults();
            Rebuild();
        }

        private void OnValidate()
        {
            lineRendererSampleCount = Mathf.Max(2, lineRendererSampleCount);
            colliderSampleCount = Mathf.Max(2, colliderSampleCount);
            width = Mathf.Max(0.01f, width);

            EnsureReferences();
            ApplyRendererDefaults();
            Rebuild();
        }

        private void Update()
        {
            if (Application.isPlaying || !rebuildEveryFrameInEditMode)
                return;

            Rebuild();
        }

        public void Rebuild()
        {
            if (splineContainer == null || lineRenderer == null || edgeCollider == null)
                return;

            int linePointCount = lineRendererSampleCount + 1;
            lineRenderer.positionCount = linePointCount;

            for (int i = 0; i < linePointCount; i++)
            {
                float t = i / (float)lineRendererSampleCount;
                float3 point = splineContainer.EvaluatePosition(t);
                Vector3 worldPoint = new(point.x, point.y, point.z);
                Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
                lineRenderer.SetPosition(i, localPoint);
            }

            int colliderPointCount = colliderSampleCount + 1;
            Vector2[] colliderPoints = new Vector2[colliderPointCount];

            for (int i = 0; i < colliderPointCount; i++)
            {
                float t = i / (float)colliderSampleCount;
                float3 point = splineContainer.EvaluatePosition(t);
                Vector3 worldPoint = new(point.x, point.y, point.z);
                Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
                colliderPoints[i] = localPoint;
            }

            edgeCollider.points = colliderPoints;
        }

        private void EnsureReferences()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            if (edgeCollider == null)
                edgeCollider = GetComponent<EdgeCollider2D>();

            if (splineContainer == null)
                splineContainer = GetComponent<SplineContainer>();
        }

        private void ApplyRendererDefaults()
        {
            if (lineRenderer == null)
                return;

            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = false;
            lineRenderer.alignment = LineAlignment.TransformZ;
            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.widthMultiplier = width;

            if (edgeCollider != null)
            {
                edgeCollider.isTrigger = true;
                edgeCollider.edgeRadius = width * 0.5f;
            }
        }
    }
}
