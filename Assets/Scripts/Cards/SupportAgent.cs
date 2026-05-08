using Towers;
using UnityEngine;

namespace Cards
{
    public sealed class SupportAgent : MonoBehaviour
    {
        private const string AreaOfEffectReticleResourcePath = "Combat/FX/AreaOfEffectReticle";
        private static Sprite fallbackSprite;
        private static GameObject radiusPreviewPrefab;

        private SupportManager supportManager;
        private SupportDef supportDef;
        private SpriteRenderer spriteRenderer;
        private GameObject radiusPreviewObject;
        private SpriteRenderer radiusPreviewSpriteRenderer;
        private Color baseColor;
        private int capacitorCharge;
        private bool isAmplified;
        private float conduitRangeBonus;

        public SupportDef Definition => supportDef;
        public SupportSubtype SupportSubtype => supportDef != null ? supportDef.supportSubtype : SupportSubtype.None;
        public BeaconSupportDef BeaconDefinition => supportDef as BeaconSupportDef;
        public float PlacementRadius => supportDef != null ? supportDef.placementRadius : 0f;
        public float SupportRadius => supportDef != null ? supportDef.supportRadius : 0f;
        public float EffectiveSupportRadius => Mathf.Max(0f, SupportRadius + (IsConduit ? conduitRangeBonus : 0f));
        public int CapacitorCharge => capacitorCharge;
        public bool IsAmplified => isAmplified;
        public float ConduitRangeBonus => conduitRangeBonus;
        public bool IsBeacon => SupportSubtype == SupportSubtype.Beacon;
        public bool IsConduit => SupportSubtype == SupportSubtype.Conduit;

        public void Initialize(SupportDef definition, SupportManager manager)
        {
            supportDef = definition;
            supportManager = manager;
            capacitorCharge = 0;
            isAmplified = false;
            conduitRangeBonus = 0f;
            EnsureRuntimeVisuals();
            HideAllRadiusPreviews();
            SetHighlighted(false, default);
        }

        public void AddCapacitorCharge(int amount)
        {
            capacitorCharge = Mathf.Max(0, capacitorCharge + amount);
        }

        public void SetAmplified(bool value = true)
        {
            isAmplified = value;
        }

        public void AddConduitRangeBonus(float amount)
        {
            conduitRangeBonus = Mathf.Max(0f, conduitRangeBonus + Mathf.Max(0f, amount));
        }

        public bool ContainsPoint(Vector3 point, float minRadius = 0.15f)
        {
            float radius = Mathf.Max(PlacementRadius, minRadius);
            return (transform.position - point).sqrMagnitude <= radius * radius;
        }

        public void ShowRadiusPreview(Color color)
        {
            if (EffectiveSupportRadius <= 0f)
                return;

            EnsureRadiusPreviewVisual();
            if (radiusPreviewObject == null)
                return;

            radiusPreviewObject.transform.localPosition = Vector3.zero;
            radiusPreviewObject.transform.localScale = ResolvePreviewLocalScale(EffectiveSupportRadius * 2f);
            radiusPreviewObject.SetActive(true);

            if (radiusPreviewSpriteRenderer != null)
                radiusPreviewSpriteRenderer.color = ResolvePreviewColor(color);
        }

        public void HideAllRadiusPreviews()
        {
            if (radiusPreviewObject != null)
                radiusPreviewObject.SetActive(false);
        }

        public void SetHighlighted(bool highlighted, Color tint)
        {
            EnsureRuntimeVisuals();
            if (spriteRenderer == null)
                return;

            spriteRenderer.color = highlighted
                ? Color.Lerp(baseColor, tint, 0.65f)
                : baseColor;
        }

        private void OnDestroy()
        {
            supportManager?.UnregisterSupport(this);
        }

        private void EnsureRuntimeVisuals()
        {
            CircleCollider2D collider = GetComponent<CircleCollider2D>();
            if (collider == null)
                collider = gameObject.AddComponent<CircleCollider2D>();

            collider.isTrigger = true;
            collider.radius = Mathf.Max(PlacementRadius, 0.2f);

            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            spriteRenderer.sprite = GetFallbackSprite();
            baseColor = ResolveDisplayColor();
            spriteRenderer.color = baseColor;

            float diameter = Mathf.Max(PlacementRadius * 2f, 0.35f);
            transform.localScale = new Vector3(diameter, diameter, 1f);
        }

        private void EnsureRadiusPreviewVisual()
        {
            if (radiusPreviewObject != null)
                return;

            if (radiusPreviewPrefab == null)
                radiusPreviewPrefab = Resources.Load<GameObject>(AreaOfEffectReticleResourcePath);

            if (radiusPreviewPrefab != null)
            {
                radiusPreviewObject = Instantiate(radiusPreviewPrefab, transform);
                radiusPreviewObject.name = "SupportRadiusPreview";
            }
            else
            {
                radiusPreviewObject = new GameObject("SupportRadiusPreview");
                radiusPreviewObject.transform.SetParent(transform, false);
                radiusPreviewSpriteRenderer = radiusPreviewObject.AddComponent<SpriteRenderer>();
                radiusPreviewSpriteRenderer.sprite = GetFallbackSprite();
                radiusPreviewSpriteRenderer.color = new Color(1f, 1f, 1f, 0.12f);
            }

            if (radiusPreviewObject != null)
            {
                radiusPreviewObject.transform.localPosition = Vector3.zero;
                radiusPreviewObject.transform.localRotation = Quaternion.identity;
                radiusPreviewObject.SetActive(false);
                radiusPreviewSpriteRenderer = radiusPreviewObject.GetComponent<SpriteRenderer>();
            }
        }

        private Color ResolveDisplayColor()
        {
            if (BeaconDefinition != null)
                return BeaconDefinition.linkColor;

            return SupportSubtype switch
            {
                SupportSubtype.Conduit => new Color(0.6f, 0.6f, 0.6f, 0.95f),
                _ => new Color(1f, 1f, 1f, 0.95f)
            };
        }

        private static Sprite GetFallbackSprite()
        {
            if (fallbackSprite != null)
                return fallbackSprite;

            fallbackSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                Texture2D.whiteTexture.width);
            return fallbackSprite;
        }

        private static Color ResolvePreviewColor(Color color)
        {
            Color previewColor = color;
            previewColor.a = Mathf.Clamp01(Mathf.Max(0.14f, color.a * 0.28f));
            return previewColor;
        }

        private Vector3 ResolvePreviewLocalScale(float desiredWorldDiameter)
        {
            Vector3 parentScale = transform.lossyScale;
            float xScale = Mathf.Abs(parentScale.x) > 0.0001f ? desiredWorldDiameter / Mathf.Abs(parentScale.x) : desiredWorldDiameter;
            float yScale = Mathf.Abs(parentScale.y) > 0.0001f ? desiredWorldDiameter / Mathf.Abs(parentScale.y) : desiredWorldDiameter;
            return new Vector3(xScale, yScale, 1f);
        }
    }
}
