using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RunFlow
{
    public class RunFlowItemTileView : MonoBehaviour
    {
        public const string ResourcePath = "RunFlow/UI/ItemTile";

        public static readonly Color ActiveBackgroundColor = new(0.15f, 0.2f, 0.28f, 1f);
        public static readonly Color InactiveBackgroundColor = new(0.1f, 0.13f, 0.18f, 0.92f);
        public static readonly Color ActiveTitleColor = Color.white;
        public static readonly Color InactiveTitleColor = new(0.74f, 0.78f, 0.84f, 1f);
        public static readonly Color SubtitleColor = new(0.78f, 0.84f, 0.9f, 1f);
        public static readonly Color DetailColor = new(0.65f, 0.72f, 0.82f, 1f);
        public static readonly Color InactiveDetailIconColor = new(0.72f, 0.76f, 0.82f, 0.8f);

        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image mainIconImage;
        [SerializeField] private Text fallbackIconText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text detailText;
        [SerializeField] private RectTransform detailIconRow;
        [SerializeField] private Image detailIconTemplate;

        private readonly List<Image> detailIconPool = new();

        public Button Button => button;
        public Image BackgroundImage => backgroundImage;
        public Image MainIconImage => mainIconImage;
        public Text FallbackIconText => fallbackIconText;
        public Text TitleText => titleText;
        public Text SubtitleText => subtitleText;
        public Text DetailText => detailText;
        public RectTransform DetailIconRow => detailIconRow;
        public Image DetailIconTemplate => detailIconTemplate;

        public bool HasRequiredFields => TryGetMissingFieldReport(out _);

        private void Awake()
        {
            if (detailIconTemplate != null && !detailIconPool.Contains(detailIconTemplate))
                detailIconPool.Add(detailIconTemplate);
        }

        public bool TryGetMissingFieldReport(out string report)
        {
            StringBuilder missingFields = new();
            AppendMissingField(missingFields, button, nameof(button));
            AppendMissingField(missingFields, backgroundImage, nameof(backgroundImage));
            AppendMissingField(missingFields, mainIconImage, nameof(mainIconImage));
            AppendMissingField(missingFields, fallbackIconText, nameof(fallbackIconText));
            AppendMissingField(missingFields, titleText, nameof(titleText));
            AppendMissingField(missingFields, subtitleText, nameof(subtitleText));
            AppendMissingField(missingFields, detailText, nameof(detailText));
            AppendMissingField(missingFields, detailIconRow, nameof(detailIconRow));
            AppendMissingField(missingFields, detailIconTemplate, nameof(detailIconTemplate));

            if (missingFields.Length == 0)
            {
                report = string.Empty;
                return true;
            }

            report = $"RunFlow ItemTile prefab is missing required field assignment(s): {missingFields}";
            return false;
        }

        public void Bind(
            Sprite icon,
            string title,
            string subtitle,
            string detail,
            UnityAction onClick,
            bool interactable,
            IReadOnlyList<Sprite> detailIcons)
        {
            if (!TryGetMissingFieldReport(out string missingFieldReport))
            {
                Debug.LogError(missingFieldReport, this);
                return;
            }

            button.onClick.RemoveAllListeners();
            if (onClick != null)
                button.onClick.AddListener(onClick);

            button.interactable = interactable;
            backgroundImage.color = interactable ? ActiveBackgroundColor : InactiveBackgroundColor;
            EnsureTextFonts();

            string safeTitle = title ?? string.Empty;
            titleText.text = safeTitle;
            titleText.color = interactable ? ActiveTitleColor : InactiveTitleColor;

            SetOptionalText(subtitleText, subtitle, SubtitleColor);
            SetOptionalText(detailText, detail, DetailColor);
            BindMainIcon(icon, safeTitle);
            BindDetailIcons(detailIcons, interactable);
        }

        private void BindMainIcon(Sprite icon, string title)
        {
            bool hasIcon = icon != null;

            mainIconImage.gameObject.SetActive(hasIcon);
            mainIconImage.sprite = icon;
            mainIconImage.preserveAspect = true;

            fallbackIconText.gameObject.SetActive(!hasIcon);
            fallbackIconText.text = string.IsNullOrWhiteSpace(title) ? "?" : title.Substring(0, 1);
        }

        private void BindDetailIcons(IReadOnlyList<Sprite> icons, bool interactable)
        {
            if (detailIconTemplate != null && !detailIconPool.Contains(detailIconTemplate))
                detailIconPool.Add(detailIconTemplate);

            for (int i = 0; i < detailIconPool.Count; i++)
            {
                if (detailIconPool[i] == null)
                    continue;

                detailIconPool[i].gameObject.SetActive(false);
                detailIconPool[i].sprite = null;
            }

            int visibleIconIndex = 0;
            if (icons != null)
            {
                for (int i = 0; i < icons.Count; i++)
                {
                    Sprite icon = icons[i];
                    if (icon == null)
                        continue;

                    Image detailIcon = GetOrCreateDetailIcon(visibleIconIndex);
                    detailIcon.sprite = icon;
                    detailIcon.color = interactable ? Color.white : InactiveDetailIconColor;
                    detailIcon.preserveAspect = true;
                    detailIcon.raycastTarget = false;
                    detailIcon.gameObject.SetActive(true);
                    visibleIconIndex++;
                }
            }

            detailIconRow.gameObject.SetActive(visibleIconIndex > 0);
        }

        private Image GetOrCreateDetailIcon(int index)
        {
            while (detailIconPool.Count <= index)
            {
                Image created = Instantiate(detailIconTemplate, detailIconRow);
                created.name = $"DetailIcon{detailIconPool.Count}";
                detailIconPool.Add(created);
            }

            return detailIconPool[index];
        }

        private static void SetOptionalText(Text text, string content, Color color)
        {
            text.text = content ?? string.Empty;
            text.color = color;
            text.gameObject.SetActive(!string.IsNullOrWhiteSpace(content));
        }

        private void EnsureTextFonts()
        {
            Font runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            AssignFontIfMissing(fallbackIconText, runtimeFont);
            AssignFontIfMissing(titleText, runtimeFont);
            AssignFontIfMissing(subtitleText, runtimeFont);
            AssignFontIfMissing(detailText, runtimeFont);
        }

        private static void AssignFontIfMissing(Text text, Font font)
        {
            if (text != null && text.font == null)
                text.font = font;
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
