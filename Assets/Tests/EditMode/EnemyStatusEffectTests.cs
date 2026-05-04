using System.Collections.Generic;
using System.Reflection;
using Combat;
using Enemies;
using NUnit.Framework;
using Towers;
using UnityEngine;

public class EnemyStatusEffectTests
{
    [Test]
    public void Slow_ReapplicationsDoNotStack_AndKeepLongestDuration()
    {
        EnemyAgent enemy = CreateEnemy(out GameObject enemyObject, 10f, 100f);
        EnemyStatusEffectDef slow = CreateStatus("slow", EnemyStatusEffectBehaviorType.Slow, EnemyStatusStackingMode.NonStackingStrongest);

        enemy.ApplyStatusEffect(CreateApplication(slow, 2f, 0.5f));
        enemy.ApplyStatusEffect(CreateApplication(slow, 1f, 0.5f));

        Assert.That(enemy.ActiveStatusEffectCount, Is.EqualTo(1));
        Assert.That(enemyObject.GetComponent<PathFollower>().Speed, Is.EqualTo(5f).Within(0.001f));

        enemy.TickStatusEffectsForTest(1.5f);
        Assert.That(enemyObject.GetComponent<PathFollower>().Speed, Is.EqualTo(5f).Within(0.001f));

        enemy.TickStatusEffectsForTest(0.6f);
        Assert.That(enemy.ActiveStatusEffectCount, Is.EqualTo(0));
        Assert.That(enemyObject.GetComponent<PathFollower>().Speed, Is.EqualTo(10f).Within(0.001f));

        Object.DestroyImmediate(enemyObject);
        Object.DestroyImmediate(slow);
    }

    [Test]
    public void Slow_StrongestApplicationWins()
    {
        EnemyAgent enemy = CreateEnemy(out GameObject enemyObject, 10f, 100f);
        EnemyStatusEffectDef slow = CreateStatus("slow", EnemyStatusEffectBehaviorType.Slow, EnemyStatusStackingMode.NonStackingStrongest);

        enemy.ApplyStatusEffect(CreateApplication(slow, 5f, 0.25f));
        enemy.ApplyStatusEffect(CreateApplication(slow, 1f, 0.5f));

        Assert.That(enemy.ActiveStatusEffectCount, Is.EqualTo(1));
        Assert.That(enemyObject.GetComponent<PathFollower>().Speed, Is.EqualTo(5f).Within(0.001f));

        enemy.TickStatusEffectsForTest(1.1f);
        Assert.That(enemyObject.GetComponent<PathFollower>().Speed, Is.EqualTo(5f).Within(0.001f));

        Object.DestroyImmediate(enemyObject);
        Object.DestroyImmediate(slow);
    }

    [Test]
    public void DamageOverTime_StackingInstancesTickAndExpireIndependently()
    {
        EnemyAgent enemy = CreateEnemy(out GameObject enemyObject, 10f, 20f);
        EnemyStatusEffectDef poison = CreateStatus("poison", EnemyStatusEffectBehaviorType.DamageOverTime, EnemyStatusStackingMode.StackingInstances);
        poison.defaultTickInterval = 1f;
        poison.defaultTickDamage = 2f;

        enemy.ApplyStatusEffect(CreateApplication(poison, 1.1f, 0f));
        enemy.ApplyStatusEffect(CreateApplication(poison, 2.1f, 0f));

        enemy.TickStatusEffectsForTest(1.01f);
        Assert.That(enemy.CurrentHealth, Is.EqualTo(16f).Within(0.001f));
        Assert.That(enemy.ActiveStatusEffectCount, Is.EqualTo(2));

        enemy.TickStatusEffectsForTest(0.2f);
        Assert.That(enemy.ActiveStatusEffectCount, Is.EqualTo(1));

        enemy.TickStatusEffectsForTest(0.8f);
        Assert.That(enemy.CurrentHealth, Is.EqualTo(14f).Within(0.001f));
        Assert.That(enemy.ActiveStatusEffectCount, Is.EqualTo(1));

        enemy.TickStatusEffectsForTest(0.2f);
        Assert.That(enemy.ActiveStatusEffectCount, Is.EqualTo(0));

        Object.DestroyImmediate(enemyObject);
        Object.DestroyImmediate(poison);
    }

    [Test]
    public void Stun_ReapplicationKeepsLongestRemainingDuration()
    {
        EnemyAgent enemy = CreateEnemy(out GameObject enemyObject, 10f, 100f);
        EnemyStatusEffectDef stun = CreateStatus("stun", EnemyStatusEffectBehaviorType.Stun, EnemyStatusStackingMode.RefreshLongest);

        enemy.ApplyStatusEffect(CreateApplication(stun, 1f, 0f));
        enemy.TickStatusEffectsForTest(0.5f);
        enemy.ApplyStatusEffect(CreateApplication(stun, 2f, 0f));
        enemy.TickStatusEffectsForTest(1.4f);

        Assert.That(enemyObject.GetComponent<PathFollower>().Speed, Is.EqualTo(0f));
        Assert.That(enemy.ActiveStatusEffectCount, Is.EqualTo(1));

        enemy.TickStatusEffectsForTest(0.7f);
        Assert.That(enemy.ActiveStatusEffectCount, Is.EqualTo(0));
        Assert.That(enemyObject.GetComponent<PathFollower>().Speed, Is.EqualTo(10f).Within(0.001f));

        Object.DestroyImmediate(enemyObject);
        Object.DestroyImmediate(stun);
    }

    [Test]
    public void TowerHit_ResistancePreventsOnHitStatusApplication()
    {
        DamageTypeDef kinetic = ScriptableObject.CreateInstance<DamageTypeDef>();
        EnemyStatusEffectDef slow = CreateStatus("slow", EnemyStatusEffectBehaviorType.Slow, EnemyStatusStackingMode.NonStackingStrongest);
        EnemyAgent enemy = CreateEnemy(out GameObject enemyObject, 10f, 100f);
        enemy.Definition.resistances.Add(new EnemyDamageResistance { damageType = kinetic });

        ProjectileTowerAttackDef attack = ScriptableObject.CreateInstance<ProjectileTowerAttackDef>();
        SetPrivateField(attack, "damageType", kinetic);
        SetPrivateField(attack, "onHitStatusEffects", new List<EnemyStatusEffectApplication>
        {
            CreateApplication(slow, 2f, 0.5f)
        });

        EnemyDamageResult result = TowerHitResolver.ApplyHit(null, attack, enemy, 10f, kinetic, enemyObject.transform.position);

        Assert.That(result.ResponseType, Is.EqualTo(EnemyDamageResponseType.Resistance));
        Assert.That(result.AppliedAmount, Is.EqualTo(5f).Within(0.001f));
        Assert.That(enemy.ActiveStatusEffectCount, Is.EqualTo(0));
        Assert.That(enemyObject.GetComponent<PathFollower>().Speed, Is.EqualTo(10f).Within(0.001f));

        Object.DestroyImmediate(attack);
        Object.DestroyImmediate(enemyObject);
        Object.DestroyImmediate(slow);
        Object.DestroyImmediate(kinetic);
    }

    private static EnemyAgent CreateEnemy(out GameObject enemyObject, float moveSpeed, float health)
    {
        EnemyDef enemyDef = ScriptableObject.CreateInstance<EnemyDef>();
        enemyDef.moveSpeed = moveSpeed;
        enemyDef.maxHealth = health;

        enemyObject = new GameObject("Status Effect Enemy");
        EnemyAgent enemy = enemyObject.AddComponent<EnemyAgent>();
        enemy.Initialize(null, null, null, null, enemyDef);
        return enemy;
    }

    private static EnemyStatusEffectDef CreateStatus(
        string id,
        EnemyStatusEffectBehaviorType behaviorType,
        EnemyStatusStackingMode stackingMode)
    {
        EnemyStatusEffectDef effect = ScriptableObject.CreateInstance<EnemyStatusEffectDef>();
        effect.id = id;
        effect.displayName = id;
        effect.stackKey = id;
        effect.behaviorType = behaviorType;
        effect.stackingMode = stackingMode;
        effect.defaultDuration = 1f;
        effect.defaultStrength = 0f;
        effect.defaultTickInterval = 1f;
        effect.defaultTickDamage = 0f;
        return effect;
    }

    private static EnemyStatusEffectApplication CreateApplication(EnemyStatusEffectDef effect, float duration, float strength)
    {
        return new EnemyStatusEffectApplication
        {
            effect = effect,
            duration = duration,
            strength = strength
        };
    }

    private static void SetPrivateField<TTarget>(TTarget target, string fieldName, object value)
    {
        FieldInfo field = null;
        System.Type type = typeof(TTarget);
        while (type != null && field == null)
        {
            field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            type = type.BaseType;
        }

        Assert.NotNull(field);
        field.SetValue(target, value);
    }
}
