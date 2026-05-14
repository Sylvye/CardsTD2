using UnityEngine;

namespace Towers
{
    public class SplashImpactFx : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float lifetime = 0.35f;
        [SerializeField, Min(0f)] private float diameterScale = 1f;

        private Vector3 baseScale = Vector3.one;
        private bool hasInitializedScale;

        private void Awake()
        {
            CacheBaseScale();
        }

        public void Initialize(float radius)
        {
            CacheBaseScale();

            float diameter = Mathf.Max(0f, radius * 2f * Mathf.Max(0f, diameterScale));
            transform.localScale = new Vector3(
                baseScale.x * diameter,
                baseScale.y * diameter,
                baseScale.z);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(gameObject);
                return;
            }
#endif
            Destroy(gameObject, lifetime);
        }

        private void CacheBaseScale()
        {
            if (hasInitializedScale)
                return;

            baseScale = transform.localScale;
            hasInitializedScale = true;
        }
    }
}
