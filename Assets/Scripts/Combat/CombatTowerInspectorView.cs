using System.Collections.Generic;
using System.Text;
using Cards;
using TMPro;
using Towers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Combat
{
    public class CombatTowerInspectorView : MonoBehaviour
    {
        public const string ResourcePath = "Combat/UI/TowerInspector";

        private static readonly Color AugmentLabelColor = Color.white;
        private static readonly Color BadgeColor = new(0.73f, 0.82f, 0.96f, 0.95f);
        private static readonly Color EmptyColor = new(0.74f, 0.78f, 0.84f, 0.9f);

        [SerializeField] private RectTransform root;
        [SerializeField] private TMP_Text towerNameText;
        [SerializeField] private Button targetingButton;
        [SerializeField] private TMP_Text targetingButtonText;
        [SerializeField] private RectTransform augmentListRoot;
        [SerializeField] private TMP_Text augmentEmptyText;
        [SerializeField] private RectTransform activeEffectsListRoot;
        [SerializeField] private TMP_Text activeEffectsEmptyText;
        [SerializeField] private RectTransform permanentModifiersListRoot;
        [SerializeField] private TMP_Text permanentModifiersEmptyText;
        [SerializeField] private RectTransform reservedStatsRoot;
        [SerializeField] private TowerInspectorEntryView augmentRowTemplate;
        [SerializeField] private TowerInspectorEntryView modifierRowTemplate;

        private readonly List<TowerInspectorEntryView> augmentRowPool = new();
        private readonly List<TowerInspectorEntryView> activeEffectRowPool = new();
        private readonly List<TowerInspectorEntryView> permanentModifierRowPool = new();

        private Sprite fallbackInspectorSprite;
        private Texture2D fallbackInspectorTexture;

        public RectTransform Root => root;
        public TMP_Text TowerNameText => towerNameText;
        public Button TargetingButton => targetingButton;
        public TMP_Text TargetingButtonText => targetingButtonText;
        public TMP_Text AugmentEmptyText => augmentEmptyText;
        public TMP_Text ActiveEffectsEmptyText => activeEffectsEmptyText;
        public TMP_Text PermanentModifiersEmptyText => permanentModifiersEmptyText;

        private void Awake()
        {
            PrimeTemplatePools();
        }

        private void OnDestroy()
        {
            if (fallbackInspectorSprite != null)
                Destroy(fallbackInspectorSprite);
        }

        public bool TryGetMissingFieldReport(out string report)
        {
            StringBuilder missingFields = new();
            AppendMissingField(missingFields, root, nameof(root));
            AppendMissingField(missingFields, towerNameText, nameof(towerNameText));
            AppendMissingField(missingFields, targetingButton, nameof(targetingButton));
            AppendMissingField(missingFields, targetingButtonText, nameof(targetingButtonText));
            AppendMissingField(missingFields, augmentListRoot, nameof(augmentListRoot));
            AppendMissingField(missingFields, augmentEmptyText, nameof(augmentEmptyText));
            AppendMissingField(missingFields, activeEffectsListRoot, nameof(activeEffectsListRoot));
            AppendMissingField(missingFields, activeEffectsEmptyText, nameof(activeEffectsEmptyText));
            AppendMissingField(missingFields, permanentModifiersListRoot, nameof(permanentModifiersListRoot));
            AppendMissingField(missingFields, permanentModifiersEmptyText, nameof(permanentModifiersEmptyText));
            AppendMissingField(missingFields, reservedStatsRoot, nameof(reservedStatsRoot));
            AppendMissingField(missingFields, augmentRowTemplate, nameof(augmentRowTemplate));
            AppendMissingField(missingFields, modifierRowTemplate, nameof(modifierRowTemplate));

            if (augmentRowTemplate != null && !augmentRowTemplate.HasRequiredFields)
                AppendMissingField(missingFields, null, nameof(augmentRowTemplate));

            if (modifierRowTemplate != null && !modifierRowTemplate.HasRequiredFields)
                AppendMissingField(missingFields, null, nameof(modifierRowTemplate));

            if (missingFields.Length == 0)
            {
                report = string.Empty;
                return true;
            }

            report = $"TowerInspector prefab is missing required field assignment(s): {missingFields}";
            return false;
        }

        public void Initialize(UnityAction onTargetingClicked)
        {
            if (!TryGetMissingFieldReport(out string missingFieldReport))
            {
                Debug.LogError(missingFieldReport, this);
                return;
            }

            PrimeTemplatePools();
            SetEmptyTextStyling(augmentEmptyText);
            SetEmptyTextStyling(activeEffectsEmptyText);
            SetEmptyTextStyling(permanentModifiersEmptyText);

            targetingButton.onClick.RemoveAllListeners();
            if (onTargetingClicked != null)
                targetingButton.onClick.AddListener(onTargetingClicked);
        }

        public void Show(TowerAgent tower)
        {
            if (tower == null)
            {
                Hide();
                return;
            }

            gameObject.SetActive(true);

            towerNameText.text = tower.DisplayName;
            targetingButtonText.text = $"Targeting: {GetPriorityLabel(tower.CurrentPriority)}";

            BindAugments(tower.AppliedAugments);
            BindModifiers(activeEffectsListRoot, activeEffectsEmptyText, activeEffectRowPool, tower.GetActiveEffectEntries());
            BindModifiers(permanentModifiersListRoot, permanentModifiersEmptyText, permanentModifierRowPool, tower.GetPermanentModifierEntries());
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void BindAugments(IReadOnlyList<CardAugmentDef> augments)
        {
            int visibleCount = 0;
            if (augments != null)
            {
                for (int i = 0; i < augments.Count; i++)
                {
                    CardAugmentDef augment = augments[i];
                    if (augment == null)
                        continue;

                    TowerInspectorEntryView row = GetOrCreateRow(augmentRowPool, visibleCount, augmentRowTemplate, augmentListRoot);
                    row.gameObject.SetActive(true);
                    row.Bind(
                        string.IsNullOrWhiteSpace(augment.displayName) ? augment.name : augment.displayName,
                        AugmentLabelColor,
                        augment.icon,
                        GetFallbackInspectorSprite(),
                        GetToneColor(TowerModifierTone.Buff),
                        string.Empty,
                        false,
                        BadgeColor);
                    visibleCount++;
                }
            }

            SetSectionVisibility(augmentEmptyText, augmentRowPool, visibleCount);
        }

        private void BindModifiers(
            RectTransform listRoot,
            TMP_Text emptyText,
            List<TowerInspectorEntryView> pool,
            IReadOnlyList<TowerInspectorModifierEntry> entries)
        {
            int visibleCount = 0;
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    TowerInspectorModifierEntry entry = entries[i];
                    TowerInspectorEntryView row = GetOrCreateRow(pool, visibleCount, modifierRowTemplate, listRoot);
                    row.gameObject.SetActive(true);
                    row.Bind(
                        entry.DisplayName,
                        GetToneColor(entry.Tone),
                        entry.Icon,
                        GetFallbackInspectorSprite(),
                        GetToneColor(entry.Tone),
                        entry.SourceLabel,
                        true,
                        BadgeColor);
                    visibleCount++;
                }
            }

            SetSectionVisibility(emptyText, pool, visibleCount);
        }

        private void SetSectionVisibility(TMP_Text emptyText, List<TowerInspectorEntryView> pool, int visibleCount)
        {
            if (emptyText != null)
                emptyText.gameObject.SetActive(visibleCount == 0);

            for (int i = visibleCount; i < pool.Count; i++)
            {
                TowerInspectorEntryView row = pool[i];
                if (row != null)
                    row.gameObject.SetActive(false);
            }
        }

        private TowerInspectorEntryView GetOrCreateRow(
            List<TowerInspectorEntryView> pool,
            int index,
            TowerInspectorEntryView template,
            RectTransform parent)
        {
            while (pool.Count <= index)
            {
                TowerInspectorEntryView created = Instantiate(template, parent, false);
                created.name = $"{template.name}{pool.Count}";
                created.gameObject.SetActive(false);
                pool.Add(created);
            }

            return pool[index];
        }

        private void PrimeTemplatePools()
        {
            RegisterTemplate(augmentRowPool, augmentRowTemplate);
            RegisterTemplate(activeEffectRowPool, modifierRowTemplate);
        }

        private static void RegisterTemplate(List<TowerInspectorEntryView> pool, TowerInspectorEntryView template)
        {
            if (template == null || pool.Contains(template))
                return;

            template.gameObject.SetActive(false);
            pool.Add(template);
        }

        private Sprite GetFallbackInspectorSprite()
        {
            if (fallbackInspectorSprite != null)
                return fallbackInspectorSprite;

            if (fallbackInspectorTexture == null)
            {
                fallbackInspectorTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    name = "TowerInspectorFallbackIcon",
                    hideFlags = HideFlags.HideAndDontSave
                };
                fallbackInspectorTexture.SetPixel(0, 0, Color.white);
                fallbackInspectorTexture.Apply(false, true);
            }

            fallbackInspectorSprite = Sprite.Create(
                fallbackInspectorTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            fallbackInspectorSprite.name = "TowerInspectorFallbackIcon";
            fallbackInspectorSprite.hideFlags = HideFlags.HideAndDontSave;
            return fallbackInspectorSprite;
        }

        private static void SetEmptyTextStyling(TMP_Text emptyText)
        {
            if (emptyText == null)
                return;

            emptyText.color = EmptyColor;
        }

        private static string GetPriorityLabel(TargetPriority priority)
        {
            return priority switch
            {
                TargetPriority.First => "First",
                TargetPriority.Last => "Last",
                TargetPriority.Strong => "Strong",
                _ => priority.ToString()
            };
        }

        private static Color GetToneColor(TowerModifierTone tone)
        {
            return tone switch
            {
                TowerModifierTone.Buff => new Color(0.75f, 0.96f, 0.75f, 1f),
                TowerModifierTone.Debuff => new Color(1f, 0.74f, 0.74f, 1f),
                _ => new Color(0.9f, 0.92f, 0.96f, 1f)
            };
        }

        private static void AppendMissingField(StringBuilder builder, Object value, string fieldName)
        {
            if (value != null)
                return;

            if (builder.Length > 0)
                builder.Append(", ");

            builder.Append(fieldName);
        }
    }
}
