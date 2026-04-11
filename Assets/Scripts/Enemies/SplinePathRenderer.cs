using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Enemies
{
    [ExecuteAlways]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class SplinePathRenderer : MonoBehaviour
    {
        [SerializeField] private SplineContainer splineContainer;
        [SerializeField, Min(2)] private int sampleCount = 128;
        [SerializeField, Min(0.01f)] private float width = 0.6f;
        [SerializeField] private bool rebuildEveryFrameInEditMode = true;

        private LineRenderer lineRenderer;

        private void Awake()
        {
            EnsureReferences();
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
            Rebuild();
        }

        private void OnValidate()
        {
            sampleCount = Mathf.Max(2, sampleCount);
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
            if (splineContainer == null || lineRenderer == null)
                return;

            int pointCount = sampleCount + 1;
            lineRenderer.positionCount = pointCount;

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)sampleCount;
                float3 point = splineContainer.EvaluatePosition(t);
                Vector3 worldPoint = new(point.x, point.y, point.z);
                Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
                lineRenderer.SetPosition(i, localPoint);
            }
        }

        private void EnsureReferences()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

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
        }
    }
}
