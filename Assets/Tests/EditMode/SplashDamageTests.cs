using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cards;
using Combat;
using Enemies;
using NUnit.Framework;
using Towers;
using UnityEngine;

public class SplashDamageTests
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
    public void SplashDamage_RadiusZero_DoesNotDamageSecondaryTargets()
    {
        GameObject managersObject = new("Splash Radius Zero Managers");
        EnemyManager enemyManager = managersObject.AddComponent<EnemyManager>();
        TowerAgent tower = CreateTower("Radius Zero Tower", enemyManager, null);
        ProjectileTowerAttackDef attackDef = ScriptableObject.CreateInstance<ProjectileTowerAttackDef>();

        EnemyAgent primary = CreateEnemy(enemyManager, "Primary", Vector3.zero, 10f);
        EnemyAgent secondary = CreateEnemy(enemyManager, "Secondary", new Vector3(0.5f, 0f, 0f), 10f);

        TowerHitResolver.ApplyHit(tower, attackDef, primary, 2f, null, primary.transform.position);

        Assert.That(primary.CurrentHealth, Is.EqualTo(8f).Within(0.001f));
        Assert.That(secondary.CurrentHealth, Is.EqualTo(10f).Within(0.001f));

        Object.DestroyImmediate(attackDef);
        Object.DestroyImmediate(tower.Definition);
        DestroyAllImmediate(primary.gameObject, secondary.gameObject, tower.gameObject, managersObject);
    }

    [Test]
    public void SplashDamage_ProjectileHit_DamagesNearestTargetsUpToCapWithoutRehittingPrimary()
    {
        GameObject managersObject = new("Projectile Splash Managers");
        EnemyManager enemyManager = managersObject.AddComponent<EnemyManager>();
        TowerAgent tower = CreateTower("Projectile Splash Tower", enemyManager, null);
        ProjectileTowerAttackDef attackDef = ScriptableObject.CreateInstance<ProjectileTowerAttackDef>();
        attackDef.AdjustSplashRadius(1.5f);
        SetPrivateField(attackDef, "splashMaxTargets", 2);

        EnemyAgent primary = CreateEnemy(enemyManager, "Primary", Vector3.zero, 10f);
        EnemyAgent nearA = CreateEnemy(enemyManager, "Near A", new Vector3(0.25f, 0f, 0f), 10f);
        EnemyAgent nearB = CreateEnemy(enemyManager, "Near B", new Vector3(0.75f, 0f, 0f), 10f);
        EnemyAgent far = CreateEnemy(enemyManager, "Far", new Vector3(1.25f, 1f, 0f), 10f);

        TowerHitResolver.ApplyHit(tower, attackDef, primary, 2f, null, primary.transform.position);

        Assert.That(primary.CurrentHealth, Is.EqualTo(8f).Within(0.001f));
        Assert.That(nearA.CurrentHealth, Is.EqualTo(8f).Within(0.001f));
        Assert.That(nearB.CurrentHealth, Is.EqualTo(8f).Within(0.001f));
        Assert.That(far.CurrentHealth, Is.EqualTo(10f).Within(0.001f));

        Object.DestroyImmediate(attackDef);
        Object.DestroyImmediate(tower.Definition);
        DestroyAllImmediate(primary.gameObject, nearA.gameObject, nearB.gameObject, far.gameObject, tower.gameObject, managersObject);
    }

    [Test]
    public void SplashDamage_BeamHit_UsesHitPointSplash()
    {
        GameObject managersObject = new("Beam Splash Managers");
        EnemyManager enemyManager = managersObject.AddComponent<EnemyManager>();
        TowerAgent tower = CreateTower("Beam Splash Tower", enemyManager, null);
        BeamTowerAttackDef attackDef = ScriptableObject.CreateInstance<BeamTowerAttackDef>();
        attackDef.AdjustSplashRadius(1f);
        SetPrivateField(attackDef, "splashMaxTargets", 1);

        EnemyAgent primary = CreateEnemy(enemyManager, "Primary", Vector3.zero, 10f);
        EnemyAgent secondary = CreateEnemy(enemyManager, "Secondary", new Vector3(0.75f, 0f, 0f), 10f);

        TowerHitResolver.ApplyHit(tower, attackDef, primary, 3f, null, primary.transform.position);

        Assert.That(primary.CurrentHealth, Is.EqualTo(7f).Within(0.001f));
        Assert.That(secondary.CurrentHealth, Is.EqualTo(7f).Within(0.001f));

        Object.DestroyImmediate(attackDef);
        Object.DestroyImmediate(tower.Definition);
        DestroyAllImmediate(primary.gameObject, secondary.gameObject, tower.gameObject, managersObject);
    }

    [Test]
    public void SplashDamage_RedBeacon_AddsToTowerBaseSplashRadius()
    {
        GameObject managersObject = new("Beacon Splash Managers");
        EnemyManager enemyManager = managersObject.AddComponent<EnemyManager>();
        TowerManager towerManager = managersObject.AddComponent<TowerManager>();
        SupportManager supportManager = managersObject.AddComponent<SupportManager>();
        SetPrivateField(towerManager, "enemyManager", enemyManager);
        SetPrivateField(towerManager, "supportManager", supportManager);
        SetPrivateField(supportManager, "towerManager", towerManager);
        SetPrivateField(supportManager, "supportParent", managersObject.transform);

        ProjectileTowerAttackDef attackDef = ScriptableObject.CreateInstance<ProjectileTowerAttackDef>();
        attackDef.AdjustSplashRadius(0.5f);
        SetPrivateField(attackDef, "splashMaxTargets", 3);

        TowerDef towerDef = ScriptableObject.CreateInstance<TowerDef>();
        towerDef.baseStats = new TowerBaseStats
        {
            maxHealth = 10f,
            range = 5f,
            fireInterval = 1f,
            damage = 1f
        };
        towerDef.attacks = new List<TowerAttackDef> { attackDef };

        GameObject towerObject = new("Beacon Splash Tower", typeof(TowerAgent));
        towerObject.transform.position = new Vector3(1f, 0f, 0f);
        TowerAgent tower = towerObject.GetComponent<TowerAgent>();
        tower.Initialize(towerDef, new TowerRuntimeContext(towerManager, enemyManager, null));
        towerManager.RegisterTower(tower);

        BeaconSupportDef beaconDef = ScriptableObject.CreateInstance<BeaconSupportDef>();
        beaconDef.supportRadius = 5f;
        beaconDef.placementRadius = 0.1f;
        beaconDef.buffType = SupportBuffType.OnHitSplashRadius;
        beaconDef.baseValues.Add(1f);
        SupportAgent beacon = supportManager.PlaceSupport(beaconDef, Vector3.zero);
        Assert.NotNull(beacon);

        TowerAttackDef executionAttackDef = GetExecutionAttackDef(tower);
        Assert.That(executionAttackDef.SplashRadius, Is.EqualTo(1.5f).Within(0.001f));

        EnemyAgent primary = CreateEnemy(enemyManager, "Primary", tower.transform.position, 10f);
        EnemyAgent secondary = CreateEnemy(enemyManager, "Secondary", tower.transform.position + new Vector3(1.25f, 0f, 0f), 10f);

        TowerHitResolver.ApplyHit(tower, executionAttackDef, primary, 2f, null, primary.transform.position);

        Assert.That(secondary.CurrentHealth, Is.EqualTo(8f).Within(0.001f));

        Object.DestroyImmediate(beaconDef);
        Object.DestroyImmediate(towerDef);
        Object.DestroyImmediate(attackDef);
        DestroyAllImmediate(primary.gameObject, secondary.gameObject, beacon.gameObject, towerObject, managersObject);
    }

    [Test]
    public void SplashDamage_SecondaryTargets_GetFullHitEffectsWithoutRecursiveSplash()
    {
        GameObject managersObject = new("Splash Effects Managers");
        EnemyManager enemyManager = managersObject.AddComponent<EnemyManager>();
        TestPlayerEffects playerEffects = new();

        ProjectileTowerAttackDef attackDef = ScriptableObject.CreateInstance<ProjectileTowerAttackDef>();
        attackDef.AdjustSplashRadius(1.5f);
        SetPrivateField(attackDef, "splashMaxTargets", 3);
        EnemyStatusEffectDef statusEffect = CreateStatusEffect("slow");
        SetPrivateField(attackDef, "onHitStatusEffects", new List<EnemyStatusEffectApplication>
        {
            new()
            {
                effect = statusEffect
            }
        });

        TowerTriggeredEffect onHitEffect = new()
        {
            trigger = TowerTriggerType.OnHit,
            effectType = TowerEffectType.GainMana,
            amount = 1f
        };

        TowerDef towerDef = ScriptableObject.CreateInstance<TowerDef>();
        towerDef.baseStats = new TowerBaseStats
        {
            maxHealth = 10f,
            range = 5f,
            fireInterval = 1f,
            damage = 1f
        };
        towerDef.triggeredEffects = new List<TowerTriggeredEffect> { onHitEffect };

        GameObject towerObject = new("Splash Effects Tower", typeof(TowerAgent));
        TowerAgent tower = towerObject.GetComponent<TowerAgent>();
        tower.Initialize(towerDef, new TowerRuntimeContext(null, enemyManager, playerEffects));

        EnemyAgent primary = CreateEnemy(enemyManager, "Primary", Vector3.zero, 10f);
        EnemyAgent survivingSplashEnemy = CreateEnemy(enemyManager, "Survivor", new Vector3(1f, 0f, 0f), 10f);
        EnemyAgent triggeredSplashEnemy = CreateEnemy(enemyManager, "Triggered", new Vector3(-1f, 0f, 0f), 10f);
        EnemyAgent chainCandidate = CreateEnemy(enemyManager, "Chain Candidate", new Vector3(2.2f, 0f, 0f), 10f);

        TowerHitResolver.ApplyHit(tower, attackDef, primary, 5f, null, primary.transform.position);

        Assert.That(survivingSplashEnemy.CurrentHealth, Is.EqualTo(5f).Within(0.001f));
        Assert.That(survivingSplashEnemy.ActiveStatusEffectCount, Is.EqualTo(1));
        Assert.That(triggeredSplashEnemy.CurrentHealth, Is.EqualTo(5f).Within(0.001f));
        Assert.That(playerEffects.ManaGained, Is.EqualTo(3));
        Assert.That(chainCandidate.CurrentHealth, Is.EqualTo(10f).Within(0.001f));

        Object.DestroyImmediate(statusEffect);
        Object.DestroyImmediate(attackDef);
        Object.DestroyImmediate(towerDef);
        DestroyAllImmediate(primary.gameObject, survivingSplashEnemy.gameObject, triggeredSplashEnemy.gameObject, chainCandidate.gameObject, towerObject, managersObject);
    }

    [Test]
    public void SplashDamage_SummonAttackDefinition_DoesNotApplyHitPointSplash()
    {
        GameObject managersObject = new("Summon Splash Managers");
        EnemyManager enemyManager = managersObject.AddComponent<EnemyManager>();
        TowerAgent tower = CreateTower("Summon Splash Tower", enemyManager, null);
        SummonTowerAttackDef attackDef = ScriptableObject.CreateInstance<SummonTowerAttackDef>();
        attackDef.AdjustSplashRadius(3f);
        SetPrivateField(attackDef, "splashMaxTargets", 2);

        EnemyAgent primary = CreateEnemy(enemyManager, "Primary", Vector3.zero, 10f);
        EnemyAgent secondary = CreateEnemy(enemyManager, "Secondary", new Vector3(0.5f, 0f, 0f), 10f);

        TowerHitResolver.ApplyHit(tower, attackDef, primary, 2f, null, primary.transform.position);

        Assert.That(primary.CurrentHealth, Is.EqualTo(8f).Within(0.001f));
        Assert.That(secondary.CurrentHealth, Is.EqualTo(10f).Within(0.001f));

        Object.DestroyImmediate(attackDef);
        Object.DestroyImmediate(tower.Definition);
        DestroyAllImmediate(primary.gameObject, secondary.gameObject, tower.gameObject, managersObject);
    }

    private static TowerAgent CreateTower(string name, EnemyManager enemyManager, IPlayerEffects playerEffects)
    {
        TowerDef towerDef = ScriptableObject.CreateInstance<TowerDef>();
        towerDef.baseStats = new TowerBaseStats
        {
            maxHealth = 10f,
            range = 5f,
            fireInterval = 1f,
            damage = 1f
        };

        GameObject towerObject = new(name, typeof(TowerAgent));
        TowerAgent tower = towerObject.GetComponent<TowerAgent>();
        tower.Initialize(towerDef, new TowerRuntimeContext(null, enemyManager, playerEffects));
        return tower;
    }

    private static EnemyAgent CreateEnemy(EnemyManager enemyManager, string name, Vector3 position, float health)
    {
        GameObject enemyObject = new(name, typeof(EnemyAgent));
        enemyObject.transform.position = position;
        EnemyAgent enemy = enemyObject.GetComponent<EnemyAgent>();
        SetPrivateField(enemy, "maxHealth", health);
        SetPrivateField(enemy, "currentHealth", health);
        SetPrivateField(enemy, "isInitialized", true);
        enemyManager.RegisterEnemy(enemy);
        return enemy;
    }

    private static EnemyStatusEffectDef CreateStatusEffect(string id)
    {
        EnemyStatusEffectDef effectDef = ScriptableObject.CreateInstance<EnemyStatusEffectDef>();
        effectDef.id = id;
        effectDef.defaultDuration = 1f;
        effectDef.defaultStrength = 0.5f;
        effectDef.behaviorType = EnemyStatusEffectBehaviorType.Slow;
        return effectDef;
    }

    private static TowerAttackDef GetExecutionAttackDef(TowerAgent tower)
    {
        IList executions = GetPrivateField<IList>(tower, "attackExecutions");
        Assert.That(executions.Count, Is.GreaterThan(0));
        object execution = executions[0];
        FieldInfo field = execution.GetType().GetField("attackDef", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, "Missing execution attack definition field.");
        return field.GetValue(execution) as TowerAttackDef;
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = FindField(target.GetType(), fieldName);
        Assert.NotNull(field, $"Missing field '{fieldName}'.");
        return (T)field.GetValue(target);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = FindField(target.GetType(), fieldName);
        Assert.NotNull(field, $"Missing field '{fieldName}'.");
        field.SetValue(target, value);
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

    private static void DestroyAllImmediate(params Object[] objects)
    {
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                Object.DestroyImmediate(objects[i]);
        }
    }
}
