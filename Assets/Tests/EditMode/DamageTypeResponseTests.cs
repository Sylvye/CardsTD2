using Combat;
using Enemies;
using NUnit.Framework;
using UnityEngine;

public class DamageTypeResponseTests
{
    [Test]
    public void ApplyDamageTypeResponses_Weakness_DoublesMatchingDamage()
    {
        DamageTypeDef fire = CreateDamageType("fire");
        EnemyDef enemy = CreateEnemy();
        enemy.weaknesses.Add(fire);

        Assert.That(enemy.ApplyDamageTypeResponses(3f, fire), Is.EqualTo(6f));
        Assert.That(enemy.ResolveDamageTypeResponse(3f, fire).ResponseType, Is.EqualTo(EnemyDamageResponseType.Weakness));

        Object.DestroyImmediate(enemy);
        Object.DestroyImmediate(fire);
    }

    [Test]
    public void ApplyDamageTypeResponses_MultipleWeaknesses_MatchIndependently()
    {
        DamageTypeDef fire = CreateDamageType("fire");
        DamageTypeDef ice = CreateDamageType("ice");
        EnemyDef enemy = CreateEnemy();
        enemy.weaknesses.Add(fire);
        enemy.weaknesses.Add(ice);

        Assert.That(enemy.ApplyDamageTypeResponses(4f, ice), Is.EqualTo(8f));

        Object.DestroyImmediate(enemy);
        Object.DestroyImmediate(fire);
        Object.DestroyImmediate(ice);
    }

    [Test]
    public void ApplyDamageTypeResponses_Resistance_HalvesMatchingDamageRoundedDown()
    {
        DamageTypeDef kinetic = CreateDamageType("kinetic");
        EnemyDef enemy = CreateEnemy();
        enemy.resistances.Add(new EnemyDamageResistance { damageType = kinetic });

        Assert.That(enemy.ApplyDamageTypeResponses(5f, kinetic), Is.EqualTo(2f));
        Assert.That(enemy.ResolveDamageTypeResponse(5f, kinetic).ResponseType, Is.EqualTo(EnemyDamageResponseType.Resistance));

        Object.DestroyImmediate(enemy);
        Object.DestroyImmediate(kinetic);
    }

    [Test]
    public void ApplyDamageTypeResponses_ImmuneResistance_PreventsMatchingDamage()
    {
        DamageTypeDef plasma = CreateDamageType("plasma");
        EnemyDef enemy = CreateEnemy();
        enemy.resistances.Add(new EnemyDamageResistance { damageType = plasma, immune = true });

        Assert.That(enemy.ApplyDamageTypeResponses(7f, plasma), Is.EqualTo(0f));
        Assert.That(enemy.ResolveDamageTypeResponse(7f, plasma).ResponseType, Is.EqualTo(EnemyDamageResponseType.Resistance));

        Object.DestroyImmediate(enemy);
        Object.DestroyImmediate(plasma);
    }

    [Test]
    public void ApplyDamageTypeResponses_BypassResistanceType_IgnoresMatchingResistance()
    {
        DamageTypeDef unblockable = CreateDamageType("unblockable");
        unblockable.bypassesResistances = true;
        EnemyDef enemy = CreateEnemy();
        enemy.resistances.Add(new EnemyDamageResistance { damageType = unblockable, immune = true });

        Assert.That(enemy.ApplyDamageTypeResponses(7f, unblockable), Is.EqualTo(7f));
        Assert.That(enemy.ResolveDamageTypeResponse(7f, unblockable).ResponseType, Is.EqualTo(EnemyDamageResponseType.Normal));

        Object.DestroyImmediate(enemy);
        Object.DestroyImmediate(unblockable);
    }

    [Test]
    public void ApplyDamageTypeResponses_UnmatchedType_LeavesDamageUnchanged()
    {
        DamageTypeDef fire = CreateDamageType("fire");
        DamageTypeDef magic = CreateDamageType("magic");
        EnemyDef enemy = CreateEnemy();
        enemy.weaknesses.Add(fire);

        Assert.That(enemy.ApplyDamageTypeResponses(3f, magic), Is.EqualTo(3f));
        Assert.That(enemy.ResolveDamageTypeResponse(3f, magic).ResponseType, Is.EqualTo(EnemyDamageResponseType.Normal));

        Object.DestroyImmediate(enemy);
        Object.DestroyImmediate(fire);
        Object.DestroyImmediate(magic);
    }

    [Test]
    public void EnemyAgent_TakeDamage_UsesFlashColorForDamageResponseType()
    {
        DamageTypeDef fire = CreateDamageType("fire");
        DamageTypeDef kinetic = CreateDamageType("kinetic");
        EnemyDef enemyDef = CreateEnemy();
        enemyDef.maxHealth = 100f;
        enemyDef.damageFlashColor = Color.red;
        enemyDef.resistedDamageFlashColor = Color.blue;
        enemyDef.weaknessDamageFlashColor = Color.yellow;
        enemyDef.damageFlashDuration = 1f;
        enemyDef.weaknesses.Add(fire);
        enemyDef.resistances.Add(new EnemyDamageResistance { damageType = kinetic });

        GameObject enemyObject = new("Enemy Damage Flash Test");
        SpriteRenderer spriteRenderer = enemyObject.AddComponent<SpriteRenderer>();
        EnemyAgent enemyAgent = enemyObject.AddComponent<EnemyAgent>();
        enemyAgent.Initialize(null, null, null, null, enemyDef);

        enemyAgent.TakeDamage(1f);
        Assert.That(spriteRenderer.color, Is.EqualTo(Color.red));

        enemyAgent.TakeDamage(1f, fire);
        Assert.That(spriteRenderer.color, Is.EqualTo(Color.yellow));

        enemyAgent.TakeDamage(5f, kinetic);
        Assert.That(spriteRenderer.color, Is.EqualTo(Color.blue));

        Object.DestroyImmediate(enemyObject);
        Object.DestroyImmediate(enemyDef);
        Object.DestroyImmediate(fire);
        Object.DestroyImmediate(kinetic);
    }

    private static EnemyDef CreateEnemy()
    {
        return ScriptableObject.CreateInstance<EnemyDef>();
    }

    private static DamageTypeDef CreateDamageType(string id)
    {
        DamageTypeDef damageType = ScriptableObject.CreateInstance<DamageTypeDef>();
        damageType.id = id;
        damageType.displayName = id;
        return damageType;
    }
}
