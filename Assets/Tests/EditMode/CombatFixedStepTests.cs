using Cards;
using Combat;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;

public class CombatFixedStepTests
{
    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
    }

    [Test]
    public void CombatSessionDriver_CycleSimulationSpeed_WrapsAndResetsOnDisable()
    {
        GameObject gameObject = new("CombatSessionDriver Test");
        CombatSessionDriver driver = gameObject.AddComponent<CombatSessionDriver>();

        Assert.That(driver.CurrentSpeedMultiplier, Is.EqualTo(1f));
        Assert.That(Time.timeScale, Is.EqualTo(1f));

        driver.CycleSimulationSpeed();
        Assert.That(driver.CurrentSpeedMultiplier, Is.EqualTo(2f));
        Assert.That(Time.timeScale, Is.EqualTo(2f));

        driver.CycleSimulationSpeed();
        Assert.That(driver.CurrentSpeedMultiplier, Is.EqualTo(4f));
        Assert.That(Time.timeScale, Is.EqualTo(4f));

        driver.CycleSimulationSpeed();
        Assert.That(driver.CurrentSpeedMultiplier, Is.EqualTo(1f));
        Assert.That(Time.timeScale, Is.EqualTo(1f));

        driver.CycleSimulationSpeed();
        Assert.That(Time.timeScale, Is.EqualTo(2f));

        MethodInfo onDisable = typeof(CombatSessionDriver).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(onDisable);
        onDisable.Invoke(driver, null);

        Assert.That(Time.timeScale, Is.EqualTo(1f));

        Object.DestroyImmediate(gameObject);
    }

    [Test]
    public void BattleFlowController_FixedTick_UsesFixedDeltaForDeterministicManaRegen()
    {
        CombatCardState cardState = new();
        HandController handController = new(cardState);
        PlayerState playerState = new(startingMana: 0, maxMana: 5, manaRegenPerSecond: 2f);
        BattleFlowController controller = new(playerState, handController);

        controller.FixedTick(0.25f);
        Assert.That(playerState.CurrentMana, Is.EqualTo(0));

        controller.FixedTick(0.25f);
        Assert.That(playerState.CurrentMana, Is.EqualTo(1));

        controller.FixedTick(0.5f);
        Assert.That(playerState.CurrentMana, Is.EqualTo(2));
    }
}
