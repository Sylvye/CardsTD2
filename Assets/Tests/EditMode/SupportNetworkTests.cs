using System.Reflection;
using Cards;
using Combat;
using Enemies;
using NUnit.Framework;
using Towers;
using UnityEngine;

public class SupportNetworkTests
{
    private sealed class TestPlayerEffects : IPlayerEffects
    {
        public int ManaGained { get; private set; }
        public int HealthLost { get; private set; }

        public void LoseHealth(int amount)
        {
            HealthLost += amount;
        }

        public void GainMana(int amount)
        {
            ManaGained += amount;
        }
    }

    [Test]
    public void SupportNetwork_BeaconUsesAmplifiedTierAndCapacitorReduction()
    {
        GameObject managersObject = new("Support Network Managers");
        TowerManager towerManager = managersObject.AddComponent<TowerManager>();
        SupportManager supportManager = managersObject.AddComponent<SupportManager>();
        SetPrivateField(towerManager, "supportManager", supportManager);
        SetPrivateField(supportManager, "towerManager", towerManager);
        SetPrivateField(supportManager, "supportParent", managersObject.transform);

        BeaconSupportDef beaconDef = ScriptableObject.CreateInstance<BeaconSupportDef>();
        beaconDef.supportRadius = 5f;
        beaconDef.placementRadius = 0.1f;
        beaconDef.buffType = SupportBuffType.DamageAdd;
        beaconDef.baseValues.AddRange(new[] { 3f, 2f, 1f });
        beaconDef.amplifiedValues.AddRange(new[] { 5f, 3f, 2f });
        beaconDef.linkColor = Color.red;

        ResolvedCardData amplifierCard = CreateUpgradeCardData(
            SupportSubtype.Amplifier,
            SupportUpgradeTargetMask.Beacon | SupportUpgradeTargetMask.Conduit);
        ResolvedCardData capacitorCard = CreateUpgradeCardData(
            SupportSubtype.Capacitor,
            SupportUpgradeTargetMask.Beacon,
            effectiveConnectionReduction: 1);

        SupportAgent beacon = supportManager.PlaceSupport(beaconDef, Vector3.zero);
        Assert.NotNull(beacon);

        TowerAgent towerA = CreateTower(towerManager, new Vector3(1f, 0f, 0f));
        TowerAgent towerB = CreateTower(towerManager, new Vector3(-1f, 0f, 0f));

        Assert.That(towerA.GetResolvedStats().Damage, Is.EqualTo(3f).Within(0.001f));
        Assert.That(towerB.GetResolvedStats().Damage, Is.EqualTo(3f).Within(0.001f));

        bool appliedAmplifier = supportManager.ApplySupportUpgrade(amplifierCard, beacon.transform.position);
        Assert.That(appliedAmplifier, Is.True);

        Assert.That(towerA.GetResolvedStats().Damage, Is.EqualTo(4f).Within(0.001f));
        Assert.That(towerB.GetResolvedStats().Damage, Is.EqualTo(4f).Within(0.001f));

        bool appliedCapacitor = supportManager.ApplySupportUpgrade(capacitorCard, beacon.transform.position);
        Assert.That(appliedCapacitor, Is.True);
        Assert.That(towerA.GetResolvedStats().Damage, Is.EqualTo(6f).Within(0.001f));
        Assert.That(towerB.GetResolvedStats().Damage, Is.EqualTo(6f).Within(0.001f));

        Object.DestroyImmediate(beaconDef);
        Object.DestroyImmediate(amplifierCard.Definition);
        Object.DestroyImmediate(capacitorCard.Definition);
        Object.DestroyImmediate(beacon.gameObject);
        Object.DestroyImmediate(towerA.gameObject);
        Object.DestroyImmediate(towerB.gameObject);
        Object.DestroyImmediate(managersObject);
    }

    [Test]
    public void SupportNetwork_ConduitRelaysBeaconEffectBeyondDirectRadius()
    {
        GameObject managersObject = new("Conduit Relay Managers");
        TowerManager towerManager = managersObject.AddComponent<TowerManager>();
        SupportManager supportManager = managersObject.AddComponent<SupportManager>();
        SetPrivateField(towerManager, "supportManager", supportManager);
        SetPrivateField(supportManager, "towerManager", towerManager);
        SetPrivateField(supportManager, "supportParent", managersObject.transform);

        BeaconSupportDef beaconDef = ScriptableObject.CreateInstance<BeaconSupportDef>();
        beaconDef.supportRadius = 2.5f;
        beaconDef.placementRadius = 0.1f;
        beaconDef.buffType = SupportBuffType.RangeAdd;
        beaconDef.baseValues.Add(2f);
        beaconDef.linkColor = Color.green;

        ConduitSupportDef conduitDef = ScriptableObject.CreateInstance<ConduitSupportDef>();
        conduitDef.supportRadius = 2.5f;
        conduitDef.placementRadius = 0.1f;

        SupportAgent beacon = supportManager.PlaceSupport(beaconDef, Vector3.zero);
        SupportAgent conduit = supportManager.PlaceSupport(conduitDef, new Vector3(2f, 0f, 0f));
        Assert.NotNull(beacon);
        Assert.NotNull(conduit);

        TowerAgent relayedTower = CreateTower(towerManager, new Vector3(4f, 0f, 0f));
        Assert.That(relayedTower.GetResolvedStats().Range, Is.EqualTo(3f).Within(0.001f));

        Object.DestroyImmediate(beaconDef);
        Object.DestroyImmediate(conduitDef);
        Object.DestroyImmediate(beacon.gameObject);
        Object.DestroyImmediate(conduit.gameObject);
        Object.DestroyImmediate(relayedTower.gameObject);
        Object.DestroyImmediate(managersObject);
    }

    [Test]
    public void SupportNetwork_AmplifiedConduitRelaysUsingBoostedRadius()
    {
        GameObject managersObject = new("Amplified Conduit Managers");
        TowerManager towerManager = managersObject.AddComponent<TowerManager>();
        SupportManager supportManager = managersObject.AddComponent<SupportManager>();
        SetPrivateField(towerManager, "supportManager", supportManager);
        SetPrivateField(supportManager, "towerManager", towerManager);
        SetPrivateField(supportManager, "supportParent", managersObject.transform);

        BeaconSupportDef beaconDef = ScriptableObject.CreateInstance<BeaconSupportDef>();
        beaconDef.supportRadius = 2.5f;
        beaconDef.placementRadius = 0.1f;
        beaconDef.buffType = SupportBuffType.RangeAdd;
        beaconDef.baseValues.Add(2f);
        beaconDef.linkColor = Color.green;

        ConduitSupportDef conduitDef = ScriptableObject.CreateInstance<ConduitSupportDef>();
        conduitDef.supportRadius = 2.5f;
        conduitDef.placementRadius = 0.1f;

        ResolvedCardData amplifierCard = CreateUpgradeCardData(
            SupportSubtype.Amplifier,
            SupportUpgradeTargetMask.Beacon | SupportUpgradeTargetMask.Conduit,
            conduitRangeIncrease: 1.5f);

        SupportAgent beacon = supportManager.PlaceSupport(beaconDef, Vector3.zero);
        SupportAgent conduit = supportManager.PlaceSupport(conduitDef, new Vector3(2f, 0f, 0f));
        Assert.NotNull(beacon);
        Assert.NotNull(conduit);

        TowerAgent outOfBaseRangeTower = CreateTower(towerManager, new Vector3(5f, 0f, 0f));
        Assert.That(outOfBaseRangeTower.GetResolvedStats().Range, Is.EqualTo(1f).Within(0.001f));

        bool appliedAmplifier = supportManager.ApplySupportUpgrade(amplifierCard, conduit.transform.position);
        Assert.That(appliedAmplifier, Is.True);
        Assert.That(outOfBaseRangeTower.GetResolvedStats().Range, Is.EqualTo(3f).Within(0.001f));

        Object.DestroyImmediate(beaconDef);
        Object.DestroyImmediate(conduitDef);
        Object.DestroyImmediate(amplifierCard.Definition);
        Object.DestroyImmediate(beacon.gameObject);
        Object.DestroyImmediate(conduit.gameObject);
        Object.DestroyImmediate(outOfBaseRangeTower.gameObject);
        Object.DestroyImmediate(managersObject);
    }

    [Test]
    public void SupportNetwork_UpgradeTargetingRulesMatchSupportSubtype()
    {
        GameObject managersObject = new("Upgrade Targeting Managers");
        TowerManager towerManager = managersObject.AddComponent<TowerManager>();
        SupportManager supportManager = managersObject.AddComponent<SupportManager>();
        SetPrivateField(towerManager, "supportManager", supportManager);
        SetPrivateField(supportManager, "towerManager", towerManager);
        SetPrivateField(supportManager, "supportParent", managersObject.transform);

        BeaconSupportDef beaconDef = ScriptableObject.CreateInstance<BeaconSupportDef>();
        beaconDef.supportRadius = 3f;
        beaconDef.placementRadius = 0.1f;

        ConduitSupportDef conduitDef = ScriptableObject.CreateInstance<ConduitSupportDef>();
        conduitDef.supportRadius = 3f;
        conduitDef.placementRadius = 0.1f;

        ResolvedCardData amplifierCard = CreateUpgradeCardData(
            SupportSubtype.Amplifier,
            SupportUpgradeTargetMask.Beacon | SupportUpgradeTargetMask.Conduit,
            conduitRangeIncrease: 1f);
        ResolvedCardData capacitorCard = CreateUpgradeCardData(
            SupportSubtype.Capacitor,
            SupportUpgradeTargetMask.Beacon,
            effectiveConnectionReduction: 1);

        SupportAgent beacon = supportManager.PlaceSupport(beaconDef, Vector3.zero);
        SupportAgent conduit = supportManager.PlaceSupport(conduitDef, new Vector3(2f, 0f, 0f));
        Assert.NotNull(beacon);
        Assert.NotNull(conduit);

        Assert.That(supportManager.CanApplySupportUpgrade(amplifierCard, beacon.transform.position), Is.True);
        Assert.That(supportManager.CanApplySupportUpgrade(amplifierCard, conduit.transform.position), Is.True);
        Assert.That(supportManager.CanApplySupportUpgrade(capacitorCard, beacon.transform.position), Is.True);
        Assert.That(supportManager.CanApplySupportUpgrade(capacitorCard, conduit.transform.position), Is.False);

        Object.DestroyImmediate(beaconDef);
        Object.DestroyImmediate(conduitDef);
        Object.DestroyImmediate(amplifierCard.Definition);
        Object.DestroyImmediate(capacitorCard.Definition);
        Object.DestroyImmediate(beacon.gameObject);
        Object.DestroyImmediate(conduit.gameObject);
        Object.DestroyImmediate(managersObject);
    }

    [Test]
    public void SupportNetwork_BeaconCanGrantOnKillMana()
    {
        GameObject managersObject = new("Kill Mana Managers");
        TowerManager towerManager = managersObject.AddComponent<TowerManager>();
        SupportManager supportManager = managersObject.AddComponent<SupportManager>();
        TestPlayerEffects playerEffects = new();
        SetPrivateField(towerManager, "supportManager", supportManager);
        SetPrivateField(supportManager, "towerManager", towerManager);
        SetPrivateField(supportManager, "supportParent", managersObject.transform);

        BeaconSupportDef beaconDef = ScriptableObject.CreateInstance<BeaconSupportDef>();
        beaconDef.supportRadius = 3f;
        beaconDef.placementRadius = 0.1f;
        beaconDef.buffType = SupportBuffType.OnKillGainMana;
        beaconDef.baseValues.Add(2f);

        SupportAgent beacon = supportManager.PlaceSupport(beaconDef, Vector3.zero);
        Assert.NotNull(beacon);

        TowerAgent tower = CreateTower(towerManager, new Vector3(1f, 0f, 0f), playerEffects);
        GameObject enemyObject = new("Support Kill Target", typeof(EnemyAgent));
        EnemyAgent enemy = enemyObject.GetComponent<EnemyAgent>();

        tower.ReportKill(enemy, 5f, enemy.transform.position);

        Assert.That(playerEffects.ManaGained, Is.EqualTo(2));

        Object.DestroyImmediate(beaconDef);
        Object.DestroyImmediate(beacon.gameObject);
        Object.DestroyImmediate(enemyObject);
        Object.DestroyImmediate(tower.gameObject);
        Object.DestroyImmediate(managersObject);
    }

    private static TowerAgent CreateTower(TowerManager towerManager, Vector3 position, IPlayerEffects playerEffects = null)
    {
        TowerDef towerDef = ScriptableObject.CreateInstance<TowerDef>();
        towerDef.baseStats = new TowerBaseStats
        {
            maxHealth = 10f,
            range = 1f,
            fireInterval = 1f,
            damage = 1f
        };

        GameObject towerObject = new("Support Test Tower", typeof(TowerAgent));
        towerObject.transform.position = position;
        TowerAgent tower = towerObject.GetComponent<TowerAgent>();
        tower.Initialize(towerDef, new TowerRuntimeContext(towerManager, null, playerEffects));
        towerManager.RegisterTower(tower);
        return tower;
    }

    private static ResolvedCardData CreateUpgradeCardData(
        SupportSubtype subtype,
        SupportUpgradeTargetMask targets,
        float conduitRangeIncrease = 0f,
        int effectiveConnectionReduction = 1)
    {
        CardDef cardDef = ScriptableObject.CreateInstance<CardDef>();
        cardDef.type = CardType.Support;
        cardDef.supportCardMode = SupportCardMode.Upgrade;
        cardDef.supportSubtype = subtype;
        cardDef.supportUpgradeTargets = targets;
        cardDef.supportConduitRangeIncrease = conduitRangeIncrease;
        cardDef.supportEffectiveConnectionReduction = effectiveConnectionReduction;
        return CardRuntimeResolver.Build(new OwnedCard(cardDef));
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Missing field '{fieldName}'.");
        field.SetValue(target, value);
    }
}
