using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RunFlow
{
    public static class SimpleUiFactory
    {
        private static Font cachedRuntimeFont;
        private static RunFlowItemTileView cachedItemTilePrefab;

        public static Canvas EnsureCanvas(string name = "Canvas")
        {
            Canvas existingCanvas = Object.FindAnyObjectByType<Canvas>();
            if (existingCanvas != null)
                return existingCanvas;

            GameObject canvasObject = new(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            _ = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        public static RectTransform CreatePanel(Transform parent, string name)
        {
            GameObject panel = new(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.08f, 0.1f, 0.12f, 0.88f);

            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(24f, 24f);
            rectTransform.offsetMax = new Vector2(-24f, -24f);
            return rectTransform;
        }

        public static RectTransform CreateFullscreenBlocker(Transform parent, string name, Color color)
        {
            GameObject overlay = new(name, typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(parent, false);

            RectTransform rectTransform = overlay.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            Image image = overlay.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return rectTransform;
        }

        public static RectTransform CreateDialogPanel(Transform parent, string name, Vector2 size)
        {
            GameObject panel = new(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = Vector2.zero;

            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.08f, 0.1f, 0.12f, 0.96f);
            return rectTransform;
        }

        public static VerticalLayoutGroup AddVerticalLayout(Transform target, int spacing = 12, int padding = 20)
        {
            VerticalLayoutGroup layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = target.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            return layout;
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(Transform target, int spacing = 12, int padding = 0)
        {
            HorizontalLayoutGroup layout = target.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static Text CreateText(Transform parent, string content, int fontSize = 28, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            GameObject textObject = new("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.font = GetRuntimeFont();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = content;

            ContentSizeFitter fitter = textObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return text;
        }

        public static Button CreateButton(Transform parent, string label, UnityAction onClick)
        {
            GameObject buttonObject = new("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.24f, 0.32f, 1f);

            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = 54f;

            Button button = buttonObject.GetComponent<Button>();
            if (onClick != null)
                button.onClick.AddListener(onClick);

            Text buttonText = CreateText(buttonObject.transform, label, 24, TextAnchor.MiddleCenter);
            RectTransform textTransform = buttonText.rectTransform;
            textTransform.anchorMin = Vector2.zero;
            textTransform.anchorMax = Vector2.one;
            textTransform.offsetMin = Vector2.zero;
            textTransform.offsetMax = Vector2.zero;

            return button;
        }

        public static InputField CreateInputField(Transform parent, string placeholder)
        {
            GameObject inputObject = new("InputField", typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
            inputObject.transform.SetParent(parent, false);

            Image background = inputObject.GetComponent<Image>();
            background.color = new Color(0.12f, 0.16f, 0.22f, 1f);

            LayoutElement layoutElement = inputObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = 54f;
            layoutElement.flexibleWidth = 1f;

            RectTransform inputRect = inputObject.GetComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(0f, 54f);

            GameObject textAreaObject = new("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textAreaObject.transform.SetParent(inputObject.transform, false);

            RectTransform textAreaRect = textAreaObject.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(16f, 10f);
            textAreaRect.offsetMax = new Vector2(-16f, -10f);

            Text placeholderText = CreateText(textAreaObject.transform, placeholder, 22, TextAnchor.MiddleLeft);
            placeholderText.name = "Placeholder";
            placeholderText.color = new Color(0.58f, 0.64f, 0.74f, 0.9f);

            Text inputText = CreateText(textAreaObject.transform, string.Empty, 22, TextAnchor.MiddleLeft);
            inputText.name = "Text";
            inputText.supportRichText = false;

            RectTransform placeholderRect = placeholderText.rectTransform;
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;

            RectTransform inputTextRect = inputText.rectTransform;
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;

            InputField inputField = inputObject.GetComponent<InputField>();
            inputField.targetGraphic = background;
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;
            inputField.lineType = InputField.LineType.SingleLine;
            inputField.text = string.Empty;
            return inputField;
        }

        public static RectTransform CreateSection(Transform parent, string name, int spacing = 8)
        {
            GameObject sectionObject = new(name, typeof(RectTransform));
            sectionObject.transform.SetParent(parent, false);
            RectTransform section = sectionObject.GetComponent<RectTransform>();
            AddVerticalLayout(section, spacing: spacing, padding: 0);
            return section;
        }

        public static RectTransform CreateScrollContent(Transform parent, string name, int spacing = 12, int padding = 20)
        {
            GameObject scrollObject = new(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(parent, false);

            Image scrollBackground = scrollObject.GetComponent<Image>();
            scrollBackground.color = new Color(1f, 1f, 1f, 0.02f);

            RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            GameObject viewportObject = new("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportObject.transform.SetParent(scrollObject.transform, false);

            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);

            RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            GameObject contentObject = new("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewportObject.transform, false);

            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            AddVerticalLayout(contentRect, spacing: spacing, padding: padding);

            ScrollRect scrollView = scrollObject.GetComponent<ScrollRect>();
            scrollView.viewport = viewportRect;
            scrollView.content = contentRect;
            scrollView.horizontal = false;
            scrollView.vertical = true;
            scrollView.movementType = ScrollRect.MovementType.Clamped;
            scrollView.scrollSensitivity = 28f;

            return contentRect;
        }

        public static Button CreateItemTile(
            Transform parent,
            Sprite icon,
            string title,
            string subtitle,
            string detail,
            UnityAction onClick,
            bool interactable = true,
            IReadOnlyList<Sprite> detailIcons = null)
        {
            RunFlowItemTileView prefab = GetItemTilePrefab();
            if (prefab != null)
            {
                if (prefab.TryGetMissingFieldReport(out string missingFieldReport))
                {
                    RunFlowItemTileView tileView = Object.Instantiate(prefab, parent, false);
                    tileView.name = "ItemTile";
                    tileView.Bind(icon, title, subtitle, detail, onClick, interactable, detailIcons);
                    return tileView.Button;
                }

                Debug.LogError($"{missingFieldReport}. Falling back to generated ItemTile UI.", prefab);
            }
            else
            {
                Debug.LogError($"Missing ItemTile prefab at Resources/{RunFlowItemTileView.ResourcePath}. Falling back to generated ItemTile UI.");
            }

            return CreateGeneratedItemTile(parent, icon, title, subtitle, detail, onClick, interactable, detailIcons);
        }

        private static RunFlowItemTileView GetItemTilePrefab()
        {
            cachedItemTilePrefab ??= Resources.Load<RunFlowItemTileView>(RunFlowItemTileView.ResourcePath);
            return cachedItemTilePrefab;
        }

        private static Button CreateGeneratedItemTile(
            Transform parent,
            Sprite icon,
            string title,
            string subtitle,
            string detail,
            UnityAction onClick,
            bool interactable,
            IReadOnlyList<Sprite> detailIcons)
        {
            GameObject tileObject = new("ItemTile", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            tileObject.transform.SetParent(parent, false);

            Image background = tileObject.GetComponent<Image>();
            background.color = interactable
                ? new Color(0.15f, 0.2f, 0.28f, 1f)
                : new Color(0.1f, 0.13f, 0.18f, 0.92f);

            LayoutElement layoutElement = tileObject.GetComponent<LayoutElement>();
            layoutElement.minHeight = 92f;

            HorizontalLayoutGroup layout = tileObject.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Button button = tileObject.GetComponent<Button>();
            button.interactable = interactable;
            if (onClick != null)
                button.onClick.AddListener(onClick);

            CreateIconFrame(tileObject.transform, icon, string.IsNullOrWhiteSpace(title) ? "?" : title.Substring(0, 1));
            CreateTileTextColumn(tileObject.transform, title, subtitle, detail, interactable, detailIcons);
            return button;
        }

        public static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.Destroy(parent.GetChild(i).gameObject);
        }

        private static void CreateIconFrame(Transform parent, Sprite icon, string fallbackText)
        {
            GameObject iconRoot = new("IconRoot", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            iconRoot.transform.SetParent(parent, false);

            LayoutElement layoutElement = iconRoot.GetComponent<LayoutElement>();
            layoutElement.minWidth = 56f;
            layoutElement.preferredWidth = 56f;
            layoutElement.minHeight = 56f;
            layoutElement.preferredHeight = 56f;

            Image rootImage = iconRoot.GetComponent<Image>();
            rootImage.color = new Color(0.08f, 0.1f, 0.14f, 1f);

            if (icon != null)
            {
                GameObject iconObject = new("Icon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(iconRoot.transform, false);

                Image iconImage = iconObject.GetComponent<Image>();
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;

                RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.1f, 0.1f);
                iconRect.anchorMax = new Vector2(0.9f, 0.9f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                return;
            }

            Text fallback = CreateText(iconRoot.transform, fallbackText, 20, TextAnchor.MiddleCenter);
            RectTransform fallbackRect = fallback.rectTransform;
            fallbackRect.anchorMin = Vector2.zero;
            fallbackRect.anchorMax = Vector2.one;
            fallbackRect.offsetMin = Vector2.zero;
            fallbackRect.offsetMax = Vector2.zero;
        }

        private static void CreateTileTextColumn(
            Transform parent,
            string title,
            string subtitle,
            string detail,
            bool interactable,
            IReadOnlyList<Sprite> detailIcons)
        {
            GameObject textColumnObject = new("TextColumn", typeof(RectTransform), typeof(LayoutElement));
            textColumnObject.transform.SetParent(parent, false);

            LayoutElement layoutElement = textColumnObject.GetComponent<LayoutElement>();
            layoutElement.minWidth = 0f;
            layoutElement.preferredWidth = 0f;
            layoutElement.flexibleWidth = 1f;

            RectTransform textColumn = textColumnObject.GetComponent<RectTransform>();
            AddVerticalLayout(textColumn, spacing: 4, padding: 0);

            Text titleText = CreateText(textColumn, title, 24);
            titleText.color = interactable ? Color.white : new Color(0.74f, 0.78f, 0.84f, 1f);

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                Text subtitleText = CreateText(textColumn, subtitle, 18);
                subtitleText.color = new Color(0.78f, 0.84f, 0.9f, 1f);
            }

            CreateDetailIconRow(textColumn, detailIcons, interactable);

            if (!string.IsNullOrWhiteSpace(detail))
            {
                Text detailText = CreateText(textColumn, detail, 18);
                detailText.color = new Color(0.65f, 0.72f, 0.82f, 1f);
            }
        }

        private static void CreateDetailIconRow(Transform parent, IReadOnlyList<Sprite> icons, bool interactable)
        {
            if (icons == null || icons.Count == 0)
                return;

            GameObject rowObject = new("DetailIconRow", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            rowObject.transform.SetParent(parent, false);

            LayoutElement rowLayoutElement = rowObject.GetComponent<LayoutElement>();
            rowLayoutElement.minHeight = 28f;
            rowLayoutElement.preferredHeight = 28f;

            HorizontalLayoutGroup rowLayout = rowObject.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 6f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            for (int i = 0; i < icons.Count; i++)
            {
                Sprite icon = icons[i];
                if (icon == null)
                    continue;

                GameObject badgeObject = new("DetailIcon", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
                badgeObject.transform.SetParent(rowObject.transform, false);

                LayoutElement badgeLayout = badgeObject.GetComponent<LayoutElement>();
                badgeLayout.minWidth = 28f;
                badgeLayout.preferredWidth = 28f;
                badgeLayout.minHeight = 28f;
                badgeLayout.preferredHeight = 28f;

                Image badgeImage = badgeObject.GetComponent<Image>();
                badgeImage.sprite = icon;
                badgeImage.preserveAspect = true;
                badgeImage.raycastTarget = false;
                badgeImage.color = interactable ? Color.white : new Color(0.72f, 0.76f, 0.82f, 0.8f);
            }
        }

        private static Font GetRuntimeFont()
        {
            cachedRuntimeFont ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return cachedRuntimeFont;
        }
    }
}
