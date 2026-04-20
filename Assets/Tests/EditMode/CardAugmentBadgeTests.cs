using System.Collections.Generic;
using System.Linq;
using Cards;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class CardAugmentBadgeTests
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
    public void CardInstance_AugmentIcons_SkipNullSpritesAndPreserveAppliedOrder()
    {
        CardDef cardDef = Track(ScriptableObject.CreateInstance<CardDef>());
        cardDef.displayName = "Badge Test Card";

        Sprite firstSprite = CreateSprite("first");
        Sprite secondSprite = CreateSprite("second");

        CardAugmentDef firstAugment = Track(ScriptableObject.CreateInstance<CardAugmentDef>());
        firstAugment.icon = firstSprite;

        CardAugmentDef hiddenAugment = Track(ScriptableObject.CreateInstance<CardAugmentDef>());

        CardAugmentDef secondAugment = Track(ScriptableObject.CreateInstance<CardAugmentDef>());
        secondAugment.icon = secondSprite;

        OwnedCard ownedCard = new(cardDef, "badge-card", new[] { firstAugment, hiddenAugment, secondAugment });
        CardInstance instance = new(ownedCard, 1);

        Assert.That(instance.ResolvedData, Is.Not.Null);
        Assert.That(instance.AugmentIcons.Count, Is.EqualTo(2));
        Assert.That(instance.ResolvedData.AugmentIcons.Count, Is.EqualTo(2));
        Assert.That(instance.AugmentIcons[0], Is.SameAs(firstSprite));
        Assert.That(instance.AugmentIcons[1], Is.SameAs(secondSprite));
    }

    [Test]
    public void CardViewPrefab_RefreshesAugmentBadges_AndHidesThemWhenUnbound()
    {
        CardView prefab = Resources.Load<CardView>("Combat/Cards/CardView");
        Assert.That(prefab, Is.Not.Null);

        CardView view = Track(Object.Instantiate(prefab));

        CardDef cardDef = Track(ScriptableObject.CreateInstance<CardDef>());
        cardDef.displayName = "Badge View Card";

        Sprite firstSprite = CreateSprite("badge-a");
        Sprite secondSprite = CreateSprite("badge-b");

        CardAugmentDef firstAugment = Track(ScriptableObject.CreateInstance<CardAugmentDef>());
        firstAugment.icon = firstSprite;

        CardAugmentDef skippedAugment = Track(ScriptableObject.CreateInstance<CardAugmentDef>());

        CardAugmentDef secondAugment = Track(ScriptableObject.CreateInstance<CardAugmentDef>());
        secondAugment.icon = secondSprite;

        OwnedCard ownedCard = new(cardDef, "view-card", new[] { firstAugment, skippedAugment, secondAugment });
        CardInstance instance = new(ownedCard, 2);

        view.Bind(instance, null);

        RectTransform badgeContainer = view.transform.Find("AugmentBadges") as RectTransform;
        Assert.That(badgeContainer, Is.Not.Null);

        List<Image> activeBadges = GetActiveBadgeImages(badgeContainer);
        Assert.That(activeBadges.Count, Is.EqualTo(2));
        Assert.That(activeBadges[0].sprite, Is.SameAs(firstSprite));
        Assert.That(activeBadges[1].sprite, Is.SameAs(secondSprite));

        view.Bind(null, null);

        Assert.That(GetActiveBadgeImages(badgeContainer), Is.Empty);
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
        texture.SetPixels(Enumerable.Repeat(Color.white, 64).ToArray());
        texture.Apply();
        return Track(Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f)));
    }

    private static List<Image> GetActiveBadgeImages(RectTransform badgeContainer)
    {
        List<Image> badges = new();
        Image[] badgeImages = badgeContainer.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < badgeImages.Length; i++)
        {
            Image badge = badgeImages[i];
            if (badge != null && badge.gameObject.activeSelf)
                badges.Add(badge);
        }

        badges.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        return badges;
    }
}
