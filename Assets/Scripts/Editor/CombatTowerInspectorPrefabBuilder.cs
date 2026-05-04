using Combat;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UI;

public static class CombatTowerInspectorPrefabBuilder
{
    private const string PrefabPath = "Assets/Resources/Combat/UI/TowerInspector.prefab";

    [MenuItem("Tools/Combat/Rebuild Tower Inspector Prefab")]
    public static void RebuildPrefab()
    {
        BuildPrefabAsset();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void RebuildAndValidatePrefab()
    {
        RebuildPrefab();
        ValidatePrefabAsset();
    }

    public static void BuildPrefabAsset()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Combat");
        EnsureFolder("Assets/Resources/Combat/UI");

        GameObject root = new("TowerInspector", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(CombatTowerInspectorView));
        try
        {
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.anchoredPosition = new Vector2(-20f, -20f);
            rootRect.sizeDelta = new Vector2(340f, 460f);

            Image rootImage = root.GetComponent<Image>();
            rootImage.color = new Color(0.06f, 0.08f, 0.11f, 0.94f);

            VerticalLayoutGroup rootLayout = root.GetComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(16, 16, 16, 16);
            rootLayout.spacing = 12f;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;
            rootLayout.childAlignment = TextAnchor.UpperLeft;

            CombatTowerInspectorView view = root.GetComponent<CombatTowerInspectorView>();

            TextMeshProUGUI towerNameText = CreateText("TowerName", root.transform, font, 28f, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            AddLayoutElement(towerNameText.gameObject, minHeight: 36f, preferredHeight: 36f);

            Button targetingButton = CreateButton("TargetingButton", root.transform, font, "Targeting: First", out TextMeshProUGUI targetingButtonText);
            AddLayoutElement(targetingButton.gameObject, minHeight: 42f, preferredHeight: 42f);

            ScrollRect scrollRect = CreateScrollRoot(root.transform, out RectTransform scrollContent);
            AddLayoutElement(scrollRect.gameObject, minHeight: 300f, preferredHeight: 340f, flexibleHeight: 1f);

            RectTransform augmentListRoot = CreateSection(scrollContent, "Augments", font, out TextMeshProUGUI augmentEmptyText);
            TowerInspectorEntryView augmentRowTemplate = CreateEntryTemplate(augmentListRoot, font, "AugmentRowTemplate");

            RectTransform activeEffectsListRoot = CreateSection(scrollContent, "Active Effects", font, out TextMeshProUGUI activeEffectsEmptyText);
            TowerInspectorEntryView modifierRowTemplate = CreateEntryTemplate(activeEffectsListRoot, font, "ModifierRowTemplate");

            RectTransform permanentModifiersListRoot = CreateSection(scrollContent, "Permanent Modifiers", font, out TextMeshProUGUI permanentModifiersEmptyText);

            RectTransform statsSection = CreateSectionContainer("Stats", scrollContent, font);
            RectTransform statsContent = CreateChild("StatsContent", statsSection);
            AddLayoutElement(statsContent.gameObject, minHeight: 56f, preferredHeight: 56f);

            SetSerializedField(view, "root", rootRect);
            SetSerializedField(view, "towerNameText", towerNameText);
            SetSerializedField(view, "targetingButton", targetingButton);
            SetSerializedField(view, "targetingButtonText", targetingButtonText);
            SetSerializedField(view, "augmentListRoot", augmentListRoot);
            SetSerializedField(view, "augmentEmptyText", augmentEmptyText);
            SetSerializedField(view, "activeEffectsListRoot", activeEffectsListRoot);
            SetSerializedField(view, "activeEffectsEmptyText", activeEffectsEmptyText);
            SetSerializedField(view, "permanentModifiersListRoot", permanentModifiersListRoot);
            SetSerializedField(view, "permanentModifiersEmptyText", permanentModifiersEmptyText);
            SetSerializedField(view, "reservedStatsRoot", statsContent);
            SetSerializedField(view, "augmentRowTemplate", augmentRowTemplate);
            SetSerializedField(view, "modifierRowTemplate", modifierRowTemplate);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    public static void ValidatePrefabAsset()
    {
        CombatTowerInspectorView prefab = AssetDatabase.LoadAssetAtPath<CombatTowerInspectorView>(PrefabPath);
        if (prefab == null)
            throw new BuildFailedException($"Failed to load tower inspector prefab at '{PrefabPath}'.");

        if (!prefab.TryGetMissingFieldReport(out string report))
            throw new BuildFailedException(report);
    }

    private static ScrollRect CreateScrollRoot(Transform parent, out RectTransform content)
    {
        GameObject scrollRoot = new("InspectorScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollRoot.transform.SetParent(parent, false);

        Image scrollImage = scrollRoot.GetComponent<Image>();
        scrollImage.color = new Color(0.11f, 0.14f, 0.18f, 0.8f);

        RectTransform viewport = CreateChild("Viewport", scrollRoot.transform, typeof(RectMask2D));
        Stretch(viewport);

        content = CreateChild("Content", viewport, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(12, 12, 12, 12);
        contentLayout.spacing = 10f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollRoot.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 16f;
        return scrollRect;
    }

    private static RectTransform CreateSection(Transform parent, string title, TMP_FontAsset font, out TextMeshProUGUI emptyText)
    {
        RectTransform section = CreateSectionContainer(title, parent, font);
        RectTransform listRoot = CreateChild($"{title.Replace(" ", string.Empty)}List", section, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));

        VerticalLayoutGroup listLayout = listRoot.GetComponent<VerticalLayoutGroup>();
        listLayout.spacing = 6f;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        listLayout.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter listFitter = listRoot.GetComponent<ContentSizeFitter>();
        listFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        listFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        emptyText = CreateText($"{title}Empty", listRoot, font, 16f, FontStyles.Normal, new Color(0.74f, 0.78f, 0.84f, 0.9f), TextAlignmentOptions.Left);
        emptyText.text = "None";
        AddLayoutElement(emptyText.gameObject, minHeight: 24f, preferredHeight: 24f);

        return listRoot;
    }

    private static RectTransform CreateSectionContainer(string title, Transform parent, TMP_FontAsset font)
    {
        RectTransform section = CreateChild(title.Replace(" ", string.Empty), parent, typeof(Image), typeof(VerticalLayoutGroup));
        section.GetComponent<Image>().color = new Color(0.13f, 0.16f, 0.2f, 0.92f);

        VerticalLayoutGroup layout = section.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperLeft;

        TextMeshProUGUI titleText = CreateText($"{title}Title", section, font, 18f, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
        titleText.text = title;
        AddLayoutElement(titleText.gameObject, minHeight: 24f, preferredHeight: 24f);

        return section;
    }

    private static TowerInspectorEntryView CreateEntryTemplate(Transform parent, TMP_FontAsset font, string name)
    {
        GameObject row = new(name, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement), typeof(TowerInspectorEntryView));
        row.transform.SetParent(parent, false);
        row.SetActive(false);

        row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.045f);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.spacing = 8f;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        AddLayoutElement(row, minHeight: 34f, preferredHeight: 34f);

        RectTransform iconRoot = CreateChild("Icon", row.transform, typeof(Image), typeof(LayoutElement));
        AddLayoutElement(iconRoot.gameObject, minWidth: 20f, preferredWidth: 20f, minHeight: 20f, preferredHeight: 20f);
        Image iconImage = iconRoot.GetComponent<Image>();
        iconImage.raycastTarget = false;

        TextMeshProUGUI labelText = CreateText("Label", row.transform, font, 16f, FontStyles.Normal, Color.white, TextAlignmentOptions.Left);
        AddLayoutElement(labelText.gameObject, minWidth: 120f, flexibleWidth: 1f, minHeight: 22f, preferredHeight: 22f);

        TextMeshProUGUI badgeText = CreateText("Badge", row.transform, font, 14f, FontStyles.Normal, Color.white, TextAlignmentOptions.MidlineRight);
        AddLayoutElement(badgeText.gameObject, minWidth: 60f, preferredWidth: 60f, minHeight: 20f, preferredHeight: 20f);

        TowerInspectorEntryView view = row.GetComponent<TowerInspectorEntryView>();
        SetSerializedField(view, "iconImage", iconImage);
        SetSerializedField(view, "labelText", labelText);
        SetSerializedField(view, "badgeText", badgeText);
        return view;
    }

    private static Button CreateButton(string name, Transform parent, TMP_FontAsset font, string text, out TextMeshProUGUI label)
    {
        GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.18f, 0.24f, 0.32f, 0.96f);

        Button button = buttonObject.GetComponent<Button>();
        label = CreateText($"{name}Label", buttonObject.transform, font, 17f, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
        label.text = text;
        Stretch(label.rectTransform, new Vector2(8f, 8f), new Vector2(-8f, -8f));
        return button;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        TMP_FontAsset font,
        float fontSize,
        FontStyles fontStyle,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateChild(string name, Transform parent, params System.Type[] extraComponents)
    {
        System.Type[] componentTypes = new System.Type[extraComponents.Length + 1];
        componentTypes[0] = typeof(RectTransform);
        for (int i = 0; i < extraComponents.Length; i++)
            componentTypes[i + 1] = extraComponents[i];

        GameObject child = new(name, componentTypes);
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rectTransform)
    {
        Stretch(rectTransform, Vector2.zero, Vector2.zero);
    }

    private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    private static void AddLayoutElement(
        Object target,
        float minWidth = -1f,
        float preferredWidth = -1f,
        float flexibleWidth = -1f,
        float minHeight = -1f,
        float preferredHeight = -1f,
        float flexibleHeight = -1f)
    {
        GameObject gameObject = target is Component component ? component.gameObject : (GameObject)target;
        LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = gameObject.AddComponent<LayoutElement>();

        if (minWidth >= 0f)
            layoutElement.minWidth = minWidth;
        if (preferredWidth >= 0f)
            layoutElement.preferredWidth = preferredWidth;
        if (flexibleWidth >= 0f)
            layoutElement.flexibleWidth = flexibleWidth;
        if (minHeight >= 0f)
            layoutElement.minHeight = minHeight;
        if (preferredHeight >= 0f)
            layoutElement.preferredHeight = preferredHeight;
        if (flexibleHeight >= 0f)
            layoutElement.flexibleHeight = flexibleHeight;
    }

    private static void SetSerializedField(Object target, string fieldName, Object value)
    {
        SerializedObject serializedObject = new(target);
        SerializedProperty property = serializedObject.FindProperty(fieldName);
        if (property == null)
            throw new System.InvalidOperationException($"Could not find serialized field '{fieldName}' on {target.GetType().Name}.");

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = System.IO.Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
