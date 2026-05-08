using Cards;
using Combat;
using NUnit.Framework;
using UnityEngine;

public class CardExhaustTests
{
    [Test]
    public void PlayCard_NonExhaustCard_GoesToDiscardPile()
    {
        CardDef cardDef = CreateCardDef(exhaust: false);
        CombatCardState cardState = BuildState(cardDef, out CardInstance cardInstance);
        HandController handController = new(cardState);
        PlayerState playerState = new(startingMana: 5);

        bool played = handController.PlayCard(cardInstance, playerState, playContext: null);

        Assert.That(played, Is.True);
        Assert.That(cardState.Hand.Contains(cardInstance), Is.True);
        Assert.That(cardState.DiscardPile.Contains(cardInstance), Is.False);
        Assert.That(cardState.ExhaustPile.Contains(cardInstance), Is.False);

        Object.DestroyImmediate(cardDef);
    }

    [Test]
    public void PlayCard_ExhaustCard_GoesToExhaustPileAndSkipsDiscardReshuffle()
    {
        CardDef cardDef = CreateCardDef(exhaust: true);
        CombatCardState cardState = BuildState(cardDef, out CardInstance cardInstance);
        HandController handController = new(cardState);
        PlayerState playerState = new(startingMana: 5);

        bool played = handController.PlayCard(cardInstance, playerState, playContext: null);

        Assert.That(played, Is.True);
        Assert.That(cardState.Hand.Contains(cardInstance), Is.False);
        Assert.That(cardState.DiscardPile.Contains(cardInstance), Is.False);
        Assert.That(cardState.ExhaustPile.Contains(cardInstance), Is.True);

        cardState.ReshuffleDiscardIntoDraw();

        Assert.That(cardState.DrawPile.Contains(cardInstance), Is.False);
        Assert.That(cardState.ExhaustPile.Contains(cardInstance), Is.True);

        Object.DestroyImmediate(cardDef);
    }

    [Test]
    public void PlayCard_DrawsReplacementCardIntoFreedHandSlot()
    {
        CardDef firstCardDef = CreateCardDef(exhaust: false);
        firstCardDef.id = "first-card";
        CardDef secondCardDef = CreateCardDef(exhaust: false);
        secondCardDef.id = "second-card";

        CombatCardState cardState = new();
        cardState.BuildDrawPileFromOwnedCards(new[]
        {
            new OwnedCard(firstCardDef),
            new OwnedCard(secondCardDef)
        });

        CardInstance openingCard = cardState.DrawOne();
        HandController handController = new(cardState);
        PlayerState playerState = new(startingMana: 5);

        bool played = handController.PlayCard(openingCard, playerState, playContext: null);

        Assert.That(played, Is.True);
        Assert.That(cardState.Hand.Count, Is.EqualTo(1));
        Assert.That(cardState.Hand.Contains(openingCard), Is.False);

        CardInstance replacement = cardState.Hand.Cards[0];
        Assert.That(replacement.Definition.id, Is.Not.EqualTo(openingCard.Definition.id));

        Object.DestroyImmediate(firstCardDef);
        Object.DestroyImmediate(secondCardDef);
    }

    private static CardDef CreateCardDef(bool exhaust)
    {
        CardDef cardDef = ScriptableObject.CreateInstance<CardDef>();
        cardDef.id = exhaust ? "exhaust-card" : "normal-card";
        cardDef.displayName = exhaust ? "Exhaust Card" : "Normal Card";
        cardDef.baseManaCost = 1;
        cardDef.exhaust = exhaust;
        return cardDef;
    }

    private static CombatCardState BuildState(CardDef cardDef, out CardInstance drawnCard)
    {
        CombatCardState cardState = new();
        OwnedCard ownedCard = new(cardDef);
        cardState.BuildDrawPileFromOwnedCards(new[] { ownedCard });
        drawnCard = cardState.DrawOne();
        return cardState;
    }
}
