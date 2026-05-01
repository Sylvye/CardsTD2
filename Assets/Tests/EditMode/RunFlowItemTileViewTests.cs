using System.Collections.Generic;
using NUnit.Framework;
using RunFlow;
using UnityEngine;
using UnityEngine.UI;

public class RunFlowItemTileViewTests
{
    private readonly List<Object> cleanupObjects = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = cleanupObjects.Count - 1; i >= 0; i--)
        {
            if (cleanupObjects[i] != null)
                Object.DestroyImmediate(cleanupObjects[i]);
        }

        cleanupObjects.Clear();
    }

    [Test]
    public void ItemTilePrefab_LoadsWithRequiredFieldsAssigned()
    {
        RunFlowItemTileView prefab = Resources.Load<RunFlowItemTileView>(RunFlowItemTileView.ResourcePath);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.TryGetMissingFieldReport(out string report), Is.True, report);
    }

    [Test]
    public void CreateItemTile_BindsTextAndMissingIconFallback()
    {
        RectTransform parent = CreateParent();

        Button button = SimpleUiFactory.CreateItemTile(parent, null, "Arc Tower", "Card Offer", "Deals damage.", null);
        RunFlowItemTileView tile = button.GetComponent<RunFlowItemTileView>();

        Assert.That(tile, Is.Not.Null);
        Assert.That(tile.TitleText.text, Is.EqualTo("Arc Tower"));
        Assert.That(tile.SubtitleText.text, Is.EqualTo("Card Offer"));
        Assert.That(tile.SubtitleText.gameObject.activeSelf, Is.True);
        Assert.That(tile.DetailText.text, Is.EqualTo("Deals damage."));
        Assert.That(tile.DetailText.gameObject.activeSelf, Is.True);
        Assert.That(tile.MainIconImage.gameObject.activeSelf, Is.False);
        Assert.That(tile.FallbackIconText.gameObject.activeSelf, Is.True);
        Assert.That(tile.FallbackIconText.text, Is.EqualTo("A"));
    }

    [Test]
    public void CreateItemTile_BindsProvidedIconAndHidesFallback()
    {
        RectTransform parent = CreateParent();
        Sprite icon = CreateSprite("main-icon");

        Button button = SimpleUiFactory.CreateItemTile(parent, icon, "Bolt", "Stored Augment", "Adds speed.", null);
        RunFlowItemTileView tile = button.GetComponent<RunFlowItemTileView>();

        Assert.That(tile, Is.Not.Null);
        Assert.That(tile.MainIconImage.gameObject.activeSelf, Is.True);
        Assert.That(tile.MainIconImage.sprite, Is.SameAs(icon));
        Assert.That(tile.FallbackIconText.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void CreateItemTile_DisabledTileAppliesInactiveState()
    {
        RectTransform parent = CreateParent();

        Button button = SimpleUiFactory.CreateItemTile(parent, null, "Locked", "Unavailable", "Need more gold.", null, interactable: false);
        RunFlowItemTileView tile = button.GetComponent<RunFlowItemTileView>();

        Assert.That(button.interactable, Is.False);
        Assert.That(tile.BackgroundImage.color, Is.EqualTo(RunFlowItemTileView.InactiveBackgroundColor));
        Assert.That(tile.TitleText.color, Is.EqualTo(RunFlowItemTileView.InactiveTitleColor));
    }

    [Test]
    public void CreateItemTile_BindsDetailIconsInOrderAndHidesRowWhenAbsent()
    {
        RectTransform parent = CreateParent();
        Sprite firstIcon = CreateSprite("first-detail-icon");
        Sprite secondIcon = CreateSprite("second-detail-icon");

        Button button = SimpleUiFactory.CreateItemTile(
            parent,
            null,
            "Target Card",
            "Compatible Card",
            "Has aspects.",
            null,
            detailIcons: new[] { firstIcon, null, secondIcon });
        RunFlowItemTileView tile = button.GetComponent<RunFlowItemTileView>();

        Assert.That(tile.DetailIconRow.gameObject.activeSelf, Is.True);
        List<Image> activeIcons = GetActiveDetailIcons(tile.DetailIconRow);
        Assert.That(activeIcons.Count, Is.EqualTo(2));
        Assert.That(activeIcons[0].sprite, Is.SameAs(firstIcon));
        Assert.That(activeIcons[1].sprite, Is.SameAs(secondIcon));

        tile.Bind(null, "Target Card", "Compatible Card", "No aspects.", null, true, null);

        Assert.That(tile.DetailIconRow.gameObject.activeSelf, Is.False);
        Assert.That(GetActiveDetailIcons(tile.DetailIconRow), Is.Empty);
    }

    private RectTransform CreateParent()
    {
        GameObject parentObject = Track(new GameObject("ItemTileTestParent", typeof(RectTransform)));
        return parentObject.GetComponent<RectTransform>();
    }

    private T Track<T>(T unityObject) where T : Object
    {
        cleanupObjects.Add(unityObject);
        return unityObject;
    }

    private Sprite CreateSprite(string textureName)
    {
        Texture2D texture = Track(new Texture2D(8, 8));
        texture.name = textureName;
        Color[] pixels = new Color[64];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        texture.SetPixels(pixels);
        texture.Apply();
        return Track(Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f)));
    }

    private static List<Image> GetActiveDetailIcons(RectTransform detailIconRow)
    {
        List<Image> activeIcons = new();
        Image[] images = detailIconRow.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image != null && image.gameObject.activeSelf)
                activeIcons.Add(image);
        }

        activeIcons.Sort((left, right) => left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex()));
        return activeIcons;
    }
}
