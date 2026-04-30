using Cards;
using Combat;
using Enemies;
using NUnit.Framework;
using RunFlow;
using System.Reflection;
using Towers;
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
    public void CombatSessionDriver_ManualDrawCost_UsesConfiguredSessionValue()
    {
        GameObject gameObject = new("CombatSessionDriver Manual Draw Cost Test");
        CombatSessionDriver driver = gameObject.AddComponent<CombatSessionDriver>();

        Assert.That(driver.ManualDrawCost, Is.EqualTo(2));

        driver.ConfigureSession(new CombatSessionSetup
        {
            ManualDrawCost = 7
        });

        Assert.That(driver.ManualDrawCost, Is.EqualTo(7));

        Object.DestroyImmediate(gameObject);
    }

    [Test]
    public void CombatSessionDriver_SetPaused_ZeroesTimeScaleAndRestoresSelectedSpeed()
    {
        GameObject gameObject = new("CombatSessionDriver Pause Test");
        CombatSessionDriver driver = gameObject.AddComponent<CombatSessionDriver>();

        driver.CycleSimulationSpeed();
        driver.CycleSimulationSpeed();
        Assert.That(driver.CurrentSpeedMultiplier, Is.EqualTo(4f));
        Assert.That(Time.timeScale, Is.EqualTo(4f));

        driver.SetPaused(true);
        Assert.That(driver.IsPaused, Is.True);
        Assert.That(Time.timeScale, Is.EqualTo(0f));

        driver.SetPaused(false);
        Assert.That(driver.IsPaused, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(4f));

        Object.DestroyImmediate(gameObject);
    }

    [Test]
    public void CombatSceneBootstrapper_BuildSessionSetup_UsesDriverDefaultsWhenRequestHasNoOverrides()
    {
        GameObject driverObject = new("CombatSessionDriver Defaults Test");
        CombatSessionDriver driver = driverObject.AddComponent<CombatSessionDriver>();
        SetPrivateField(driver, "defaultSetup", new CombatSessionSetup
        {
            StartingMana = 10,
            MaxMana = 20,
            ManaRegenPerSecond = 1f,
            CurrentHealth = 20,
            MaxHealth = 20,
            OpeningHandSize = 5,
            ManualDrawCost = 5
        });

        GameObject bootstrapperObject = new("CombatSceneBootstrapper Test");
        CombatSceneBootstrapper bootstrapper = bootstrapperObject.AddComponent<CombatSceneBootstrapper>();
        SetPrivateField(bootstrapper, "combatSessionDriver", driver);

        CombatSceneRequest request = new("node", null, null, new RunSaveData
        {
            currentHealth = 13,
            maxHealth = 21
        });

        MethodInfo buildSessionSetup = typeof(CombatSceneBootstrapper).GetMethod("BuildSessionSetup", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(buildSessionSetup);

        CombatSessionSetup setup = (CombatSessionSetup)buildSessionSetup.Invoke(bootstrapper, new object[] { request });

        Assert.That(setup.StartingMana, Is.EqualTo(10));
        Assert.That(setup.ManualDrawCost, Is.EqualTo(5));
        Assert.That(setup.CurrentHealth, Is.EqualTo(13));
        Assert.That(setup.MaxHealth, Is.EqualTo(21));

        Object.DestroyImmediate(bootstrapperObject);
        Object.DestroyImmediate(driverObject);
    }

    [Test]
    public void FieldCardUseController_ShouldProcessInput_FalseWhenInputIsBlocked()
    {
        GameObject controllerObject = new("FieldCardUseController Test");
        FieldCardUseController controller = controllerObject.AddComponent<FieldCardUseController>();
        SelectedCardController selectedCardController = new();
        CardDef cardDef = ScriptableObject.CreateInstance<CardDef>();
        selectedCardController.Select(new CardInstance(cardDef, 1));

        controller.Initialize(selectedCardController, null, null, null, () => true);

        MethodInfo shouldProcessInput = typeof(FieldCardUseController).GetMethod("ShouldProcessInput", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(shouldProcessInput);
        Assert.That((bool)shouldProcessInput.Invoke(controller, null), Is.False);

        Object.DestroyImmediate(cardDef);
        Object.DestroyImmediate(controllerObject);
    }

    [Test]
    public void CardPreviewController_Update_HidesPreviewWhenInputIsBlocked()
    {
        GameObject controllerObject = new("CardPreviewController Test");
        CardPreviewController controller = controllerObject.AddComponent<CardPreviewController>();
        GameObject primaryVisual = new("Primary Visual");
        GameObject secondaryVisual = new("Secondary Visual");
        primaryVisual.SetActive(true);
        secondaryVisual.SetActive(true);

        SetPrivateField(controller, "primaryRadiusVisual", primaryVisual);
        SetPrivateField(controller, "secondaryRadiusVisual", secondaryVisual);

        SelectedCardController selectedCardController = new();
        CardDef cardDef = ScriptableObject.CreateInstance<CardDef>();
        selectedCardController.Select(new CardInstance(cardDef, 2));

        controller.Initialize(selectedCardController, null, null, () => true);

        MethodInfo update = typeof(CardPreviewController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(update);
        update.Invoke(controller, null);

        Assert.That(primaryVisual.activeSelf, Is.False);
        Assert.That(secondaryVisual.activeSelf, Is.False);

        Object.DestroyImmediate(cardDef);
        Object.DestroyImmediate(primaryVisual);
        Object.DestroyImmediate(secondaryVisual);
        Object.DestroyImmediate(controllerObject);
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

    [Test]
    public void TowerProjectile_FixedUpdate_SweepsBetweenPositions()
    {
        GameObject enemyObject = new("Projectile Sweep Enemy");
        enemyObject.transform.position = new Vector3(1f, 0f, 0f);
        CircleCollider2D enemyCollider = enemyObject.AddComponent<CircleCollider2D>();
        enemyCollider.radius = 0.2f;
        EnemyAgent enemy = enemyObject.AddComponent<EnemyAgent>();
        SetPrivateField(enemy, "currentHealth", 10f);

        GameObject projectileObject = new("Projectile Sweep Projectile");
        projectileObject.transform.position = Vector3.zero;
        projectileObject.AddComponent<Rigidbody2D>();
        CircleCollider2D projectileCollider = projectileObject.AddComponent<CircleCollider2D>();
        projectileCollider.radius = 0.05f;
        projectileCollider.isTrigger = true;
        TowerProjectile projectile = projectileObject.AddComponent<TowerProjectile>();

        Physics2D.SyncTransforms();

        projectile.Initialize(
            null,
            enemy,
            Vector3.right,
            1f,
            null,
            100f,
            1f,
            false,
            1,
            null
        );

        MethodInfo fixedUpdate = typeof(TowerProjectile).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(fixedUpdate);
        fixedUpdate.Invoke(projectile, null);

        Assert.That(enemy.CurrentHealth, Is.EqualTo(9f));

        Object.DestroyImmediate(projectileObject);
        Object.DestroyImmediate(enemyObject);
    }

    private static void SetPrivateField<TTarget>(TTarget target, string fieldName, object value)
    {
        FieldInfo field = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(target, value);
    }
}
