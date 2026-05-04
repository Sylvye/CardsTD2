using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Combat
{
    public class TowerInspectorEntryView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text badgeText;

        public Image IconImage => iconImage;
        public TMP_Text LabelText => labelText;
        public TMP_Text BadgeText => badgeText;

        public bool HasRequiredFields => TryGetMissingFieldReport(out _);

        public bool TryGetMissingFieldReport(out string report)
        {
            StringBuilder missingFields = new();
            AppendMissingField(missingFields, iconImage, nameof(iconImage));
            AppendMissingField(missingFields, labelText, nameof(labelText));
            AppendMissingField(missingFields, badgeText, nameof(badgeText));

            if (missingFields.Length == 0)
            {
                report = string.Empty;
                return true;
            }

            report = $"Tower inspector entry view is missing required field assignment(s): {missingFields}";
            return false;
        }

        public void Bind(
            string label,
            Color labelColor,
            Sprite icon,
            Sprite fallbackIcon,
            Color fallbackIconColor,
            string badge,
            bool showBadge,
            Color badgeColor)
        {
            if (!TryGetMissingFieldReport(out string missingFieldReport))
            {
                Debug.LogError(missingFieldReport, this);
                return;
            }

            labelText.text = label ?? string.Empty;
            labelText.color = labelColor;

            iconImage.sprite = icon != null ? icon : fallbackIcon;
            iconImage.color = icon != null ? Color.white : fallbackIconColor;
            iconImage.preserveAspect = true;

            badgeText.text = badge ?? string.Empty;
            badgeText.color = badgeColor;
            badgeText.gameObject.SetActive(showBadge);
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
