using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cards;
using Combat;
using Enemies;
using NUnit.Framework;
using Towers;
using UnityEngine;
using UnityEngine.UI;

public class TowerInspectorTests
{
    private sealed class TestPlayerEffects : IPlayerEffects
    {
        public int ManaGained { get; private set; }

        public void LoseHealth(int amount)
        {
        }

        public void GainMana(int amount)
        {
            ManaGained += amount;
        }
    }

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
    public void TowerInspectorPrefab_LoadsWithRequiredFieldsAssigned()
    {
        CombatTowerInspectorView prefab = Resources.Load<CombatTowerInspectorView>(CombatTowerInspectorView.ResourcePath);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.TryGetMissingFieldReport(out string report), Is.True, report);
    }

    [Test]
    public void TowerAgent_InspectorEntries_GroupActivePermanentAndInheritedModifiers()
    {
        TowerFlatStatModifierDef baseModifier = Track(ScriptableObject.CreateInstance<TowerFlatStatModifierDef>());
        baseModifier.name = "Base Modifier";

        TowerFlatStatModifierDef augmentModifier = Track(ScriptableObject.CreateInstance<TowerFlatStatModifierDef>());
        augmentModifier.name = "Augment Modifier";

        TowerFlatStatModifierDef activeSpellModifier = Track(ScriptableObject.CreateInstance<TowerFlatStatModifierDef>());
        activeSpellModifier.name = "Active Spell Modifier";

        TowerFlatStatModifierDef permanentSpellModifier = Track(ScriptableObject.CreateInstance<TowerFlatStatModifierDef>());
        permanentSpellModifier.name = "Permanent Spell Modifier";

        TowerFlatStatModifierDef inheritedModifier = Track(ScriptableObject.CreateInstance<TowerFlatStatModifierDef>());
        inheritedModifier.name = "Inherited Modifier";

        TowerDef towerDef = Track(ScriptableObject.CreateInstance<TowerDef>());
        towerDef.displayName = "Inspector Tower";
        towerDef.defaultModifiers = new List<TowerStatModifierDef> { baseModifier };

        TowerDef parentDef = Track(ScriptableObject.CreateInstance<TowerDef>());
        parentDef.displayName = "Parent Tower";
        parentDef.defaultModifiers = new List<TowerStatModifierDef> { inheritedModifier };

        GameObject towerObject = Track(new GameObject("Tower", typeof(TowerAgent)));
        GameObject parentTowerObject = Track(new GameObject("ParentTower", typeof(TowerAgent)));
        TowerAgent tower = towerObject.GetComponent<TowerAgent>();
        TowerAgent parentTower = parentTowerObject.GetComponent<TowerAgent>();

        TowerRuntimeContext context = new(null, null, null);
        tower.Initialize(towerDef, context);
        parentTower.Initialize(parentDef, context);

        tower.AddModifier(augmentModifier, TowerModifierSource.Augment, TowerModifierDuration.Permanent);
        tower.AddModifier(activeSpellModifier, TowerModifierSource.Spell, TowerModifierDuration.Active);
        tower.AddModifier(permanentSpellModifier, TowerModifierSource.Spell, TowerModifierDuration.Permanent);
        tower.InheritModifiersFrom(parentTower);

        List<TowerInspectorModifierEntry> activeEntries = tower.GetActiveEffectEntries();
        List<TowerInspectorModifierEntry> permanentEntries = tower.GetPermanentModifierEntries();

        Assert.That(activeEntries.Count, Is.EqualTo(1));
        Assert.That(activeEntries[0].DisplayName, Is.EqualTo("Active Spell Modifier"));
        Assert.That(activeEntries[0].Source, Is.EqualTo(TowerModifierSource.Spell));
        Assert.That(activeEntries[0].Duration, Is.EqualTo(TowerModifierDuration.Active));

        Assert.That(permanentEntries.Count, Is.EqualTo(4));
        CollectionAssert.AreEquivalent(
            new[]
            {
                TowerModifierSource.Base,
                TowerModifierSource.Augment,
                TowerModifierSource.Spell,
                TowerModifierSource.Inherited
            },
            permanentEntries.ConvertAll(entry => entry.Source));
    }

    [Test]
    public void BattleHUD_TargetingButton_CyclesTowerPriorityAndUpdatesLabel()
    {
        BattleHUD battleHUD = CreateBattleHud();
        TowerAgent tower = CreateTower("Target Tower");

        battleHUD.ShowTowerInspector(tower);

        Assert.That(battleHUD.TargetingButtonLabel.text, Is.EqualTo("Targeting: First"));

        battleHUD.TargetingButtonControl.onClick.Invoke();
        Assert.That(tower.CurrentPriority, Is.EqualTo(TargetPriority.Last));
        Assert.That(battleHUD.TargetingButtonLabel.text, Is.EqualTo("Targeting: Last"));

        battleHUD.TargetingButtonControl.onClick.Invoke();
        Assert.That(tower.CurrentPriority, Is.EqualTo(TargetPriority.Strong));
        Assert.That(battleHUD.TargetingButtonLabel.text, Is.EqualTo("Targeting: Strong"));

        battleHUD.TargetingButtonControl.onClick.Invoke();
        Assert.That(tower.CurrentPriority, Is.EqualTo(TargetPriority.First));
        Assert.That(battleHUD.TargetingButtonLabel.text, Is.EqualTo("Targeting: First"));
    }

    [Test]
    public void BattleHUD_ShowAndHideInspector_RendersEmptySections()
    {
        BattleHUD battleHUD = CreateBattleHud();
        TowerAgent tower = CreateTower("Empty Tower");

        battleHUD.ShowTowerInspector(tower);

        Assert.That(battleHUD.TowerInspectorRoot, Is.Not.Null);
        Assert.That(battleHUD.TowerInspectorRoot.gameObject.activeSelf, Is.True);
        Assert.That(battleHUD.TowerNameLabel.text, Is.EqualTo("Empty Tower"));
        Assert.That(battleHUD.AugmentEmptyLabel.gameObject.activeSelf, Is.True);
        Assert.That(battleHUD.ActiveEffectsEmptyLabel.gameObject.activeSelf, Is.True);
        Assert.That(battleHUD.PermanentModifiersEmptyLabel.gameObject.activeSelf, Is.True);

        battleHUD.HideTowerInspector();
        Assert.That(battleHUD.TowerInspectorRoot.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void TowerAgent_SetSupportState_UnifiesInspectorEntriesAndAttackModifiers()
    {
        ProjectileTowerAttackDef attackDef = Track(ScriptableObject.CreateInstance<ProjectileTowerAttackDef>());
        attackDef.projectileCount = 1;
        attackDef.pierceCount = 0;
        attackDef.AdjustSplashRadius(0.25f);

        TowerDef towerDef = Track(ScriptableObject.CreateInstance<TowerDef>());
        towerDef.displayName = "Support Tower";
        towerDef.baseStats = new TowerBaseStats
        {
            maxHealth = 10f,
            range = 5f,
            fireInterval = 1f,
            damage = 1f
        };
        towerDef.attacks = new List<TowerAttackDef> { attackDef };

        GameObject towerObject = Track(new GameObject("Support Tower", typeof(TowerAgent)));
        TowerAgent tower = towerObject.GetComponent<TowerAgent>();
        tower.Initialize(towerDef, new TowerRuntimeContext(null, null, null));

        tower.SetSupportState(new List<RuntimeSupportEffect>
        {
            new("+2 Range", rangeAdd: 2f),
            new("Projectile Count x2", attackModifier: new TowerAttackModifierData
            {
                projectileCountMultiplier = 2f,
                beamProjectileCountMultiplier = 2f
            }),
            new("+3 Pierce", attackModifier: new TowerAttackModifierData
            {
                pierceDelta = 3
            }),
            new("+1.25 Splash Radius", attackModifier: new TowerAttackModifierData
            {
                splashRadiusDelta = 1.25f
            }),
            new("On Kill: +2 Mana", triggeredEffect: new TowerTriggeredEffect
            {
                trigger = TowerTriggerType.OnKill,
                effectType = TowerEffectType.GainMana,
                amount = 2f
            })
        });

        List<TowerInspectorModifierEntry> activeEntries = tower.GetActiveEffectEntries();
        Assert.That(activeEntries.Count, Is.EqualTo(5));
        CollectionAssert.AreEquivalent(
            new[]
            {
                "+2 Range",
                "Projectile Count x2",
                "+3 Pierce",
                "+1.25 Splash Radius",
                "On Kill: +2 Mana"
            },
            activeEntries.ConvertAll(entry => entry.DisplayName));
        Assert.That(activeEntries.TrueForAll(entry => entry.Source == TowerModifierSource.Support));
        Assert.That(activeEntries.TrueForAll(entry => entry.Duration == TowerModifierDuration.Active));
        Assert.That(tower.GetResolvedStats().Range, Is.EqualTo(7f).Within(0.001f));

        ProjectileTowerAttackDef executionAttackDef = GetExecutionAttackDef(tower) as ProjectileTowerAttackDef;
        Assert.NotNull(executionAttackDef);
        Assert.That(executionAttackDef.projectileCount, Is.EqualTo(2));
        Assert.That(executionAttackDef.pierceCount, Is.EqualTo(3));
        Assert.That(executionAttackDef.SplashRadius, Is.EqualTo(1.5f).Within(0.001f));

        tower.SetSupportState(null);

        Assert.That(tower.GetActiveEffectEntries(), Is.Empty);

        executionAttackDef = GetExecutionAttackDef(tower) as ProjectileTowerAttackDef;
        Assert.NotNull(executionAttackDef);
        Assert.That(executionAttackDef.projectileCount, Is.EqualTo(1));
        Assert.That(executionAttackDef.pierceCount, Is.EqualTo(0));
        Assert.That(executionAttackDef.SplashRadius, Is.EqualTo(0.25f).Within(0.001f));
    }

    [Test]
    public void TowerAgent_SetSupportState_SupportTriggeredEffectsStillFire()
    {
        TestPlayerEffects playerEffects = new();
        ProjectileTowerAttackDef attackDef = Track(ScriptableObject.CreateInstance<ProjectileTowerAttackDef>());
        TowerDef towerDef = Track(ScriptableObject.CreateInstance<TowerDef>());
        towerDef.displayName = "Trigger Tower";
        towerDef.baseStats = new TowerBaseStats
        {
            maxHealth = 10f,
            range = 5f,
            fireInterval = 1f,
            damage = 1f
        };
        towerDef.attacks = new List<TowerAttackDef> { attackDef };

        GameObject towerObject = Track(new GameObject("Trigger Tower", typeof(TowerAgent)));
        TowerAgent tower = towerObject.GetComponent<TowerAgent>();
        tower.Initialize(towerDef, new TowerRuntimeContext(null, null, playerEffects));
        tower.SetSupportState(new List<RuntimeSupportEffect>
        {
            new("On Kill: +2 Mana", triggeredEffect: new TowerTriggeredEffect
            {
                trigger = TowerTriggerType.OnKill,
                effectType = TowerEffectType.GainMana,
                amount = 2f
            })
        });

        GameObject enemyObject = Track(new GameObject("Enemy", typeof(EnemyAgent)));
        EnemyAgent enemy = enemyObject.GetComponent<EnemyAgent>();
        tower.ReportKill(enemy, 3f, enemy.transform.position);

        Assert.That(playerEffects.ManaGained, Is.EqualTo(2));
    }

    private BattleHUD CreateBattleHud()
    {
        GameObject canvasObject = Track(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)));
        GameObject hudObject = Track(new GameObject("BattleHUD", typeof(RectTransform), typeof(BattleHUD)));
        hudObject.transform.SetParent(canvasObject.transform, false);

        BattleHUD battleHUD = hudObject.GetComponent<BattleHUD>();
        SetPrivateField(battleHUD, "manaText", CreateText("ManaText", hudObject.transform));
        battleHUD.EnsureInspectorUi();
        return battleHUD;
    }

    private TowerAgent CreateTower(string displayName)
    {
        TowerDef towerDef = Track(ScriptableObject.CreateInstance<TowerDef>());
        towerDef.displayName = displayName;

        GameObject towerObject = Track(new GameObject(displayName, typeof(TowerAgent)));
        TowerAgent tower = towerObject.GetComponent<TowerAgent>();
        tower.Initialize(towerDef, new TowerRuntimeContext(null, null, null));
        return tower;
    }

    private TMPro.TextMeshProUGUI CreateText(string name, Transform parent)
    {
        GameObject textObject = Track(new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI)));
        textObject.transform.SetParent(parent, false);
        return textObject.GetComponent<TMPro.TextMeshProUGUI>();
    }

    private void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Could not find field '{fieldName}'.");
        field.SetValue(target, value);
    }

    private static TowerAttackDef GetExecutionAttackDef(TowerAgent tower)
    {
        IList executions = GetInheritedPrivateField<IList>(tower, "attackExecutions");
        Assert.That(executions.Count, Is.GreaterThan(0));
        object execution = executions[0];
        FieldInfo field = execution.GetType().GetField("attackDef", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, "Missing execution attack definition field.");
        return field.GetValue(execution) as TowerAttackDef;
    }

    private static T GetInheritedPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = FindField(target.GetType(), fieldName);
        Assert.NotNull(field, $"Could not find field '{fieldName}'.");
        return (T)field.GetValue(target);
    }

    private static FieldInfo FindField(System.Type type, string fieldName)
    {
        while (type != null)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
                return field;

            type = type.BaseType;
        }

        return null;
    }

    private T Track<T>(T unityObject) where T : Object
    {
        cleanupObjects.Add(unityObject);
        return unityObject;
    }
}
