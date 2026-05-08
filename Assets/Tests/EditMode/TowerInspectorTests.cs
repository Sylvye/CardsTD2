using System.Collections.Generic;
using System.Reflection;
using Combat;
using NUnit.Framework;
using Towers;
using UnityEngine;
using UnityEngine.UI;

public class TowerInspectorTests
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

    private T Track<T>(T unityObject) where T : Object
    {
        cleanupObjects.Add(unityObject);
        return unityObject;
    }
}
