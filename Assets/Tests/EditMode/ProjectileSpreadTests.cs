using Cards;
using NUnit.Framework;
using Towers;
using UnityEditor;
using UnityEngine;

public class ProjectileSpreadTests
{
    [Test]
    public void GetAngleOffsetDegrees_SingleProjectile_FiresStraight()
    {
        float angleOffset = ProjectileSpreadUtility.GetAngleOffsetDegrees(0, 1, 25f);

        Assert.That(angleOffset, Is.EqualTo(0f));
    }

    [Test]
    public void GetAngleOffsetDegrees_OddProjectileCount_UsesConstantAdjacentSpacing()
    {
        float[] angleOffsets =
        {
            ProjectileSpreadUtility.GetAngleOffsetDegrees(0, 3, 10f),
            ProjectileSpreadUtility.GetAngleOffsetDegrees(1, 3, 10f),
            ProjectileSpreadUtility.GetAngleOffsetDegrees(2, 3, 10f)
        };

        Assert.That(angleOffsets, Is.EqualTo(new[] { -10f, 0f, 10f }));
    }

    [Test]
    public void GetAngleOffsetDegrees_EvenProjectileCount_IsCenteredSymmetrically()
    {
        float[] angleOffsets =
        {
            ProjectileSpreadUtility.GetAngleOffsetDegrees(0, 4, 10f),
            ProjectileSpreadUtility.GetAngleOffsetDegrees(1, 4, 10f),
            ProjectileSpreadUtility.GetAngleOffsetDegrees(2, 4, 10f),
            ProjectileSpreadUtility.GetAngleOffsetDegrees(3, 4, 10f)
        };

        Assert.That(angleOffsets, Is.EqualTo(new[] { -15f, -5f, 5f, 15f }));
    }

    [Test]
    public void ApplyTo_SpreadReduction_StopsAtMinimumDegreesSpread()
    {
        ProjectileTowerAttackDef attackDef = ScriptableObject.CreateInstance<ProjectileTowerAttackDef>();
        SetSerializedFloat(attackDef, "degreesSpread", 15f);
        SetSerializedFloat(attackDef, "minimumDegreesSpread", 8f);
        attackDef.ClampSpreadToMinimum();

        TowerAttackModifierData modifier = new()
        {
            degreesSpreadDelta = -20f
        };

        modifier.ApplyTo(attackDef);

        Assert.That(attackDef.DegreesSpread, Is.EqualTo(8f));
        Assert.That(attackDef.MinimumDegreesSpread, Is.EqualTo(8f));

        Object.DestroyImmediate(attackDef);
    }

    [Test]
    public void ApplyTo_SpreadIncrease_CanGrowAboveMinimumDegreesSpread()
    {
        ProjectileTowerAttackDef attackDef = ScriptableObject.CreateInstance<ProjectileTowerAttackDef>();
        SetSerializedFloat(attackDef, "degreesSpread", 8f);
        SetSerializedFloat(attackDef, "minimumDegreesSpread", 8f);
        attackDef.ClampSpreadToMinimum();

        TowerAttackModifierData modifier = new()
        {
            degreesSpreadDelta = 5f
        };

        modifier.ApplyTo(attackDef);

        Assert.That(attackDef.DegreesSpread, Is.EqualTo(13f));
        Assert.That(attackDef.MinimumDegreesSpread, Is.EqualTo(8f));

        Object.DestroyImmediate(attackDef);
    }

    [Test]
    public void ClampSpreadToMinimum_CorrectsSpreadBelowMinimum()
    {
        ProjectileTowerAttackDef attackDef = ScriptableObject.CreateInstance<ProjectileTowerAttackDef>();
        SetSerializedFloat(attackDef, "degreesSpread", 3f);
        SetSerializedFloat(attackDef, "minimumDegreesSpread", 9f);

        attackDef.ClampSpreadToMinimum();

        Assert.That(attackDef.DegreesSpread, Is.EqualTo(9f));
        Assert.That(attackDef.MinimumDegreesSpread, Is.EqualTo(9f));

        Object.DestroyImmediate(attackDef);
    }

    [Test]
    public void CombatProjectileAssets_DefaultMinimumSpreadToZero_AndKeepExistingSpreadValues()
    {
        ProjectileTowerAttackDef pellet = AssetDatabase.LoadAssetAtPath<ProjectileTowerAttackDef>(
            "Assets/Resources/Combat/Spawnables/Towers/Tower Attacks/Definitions/Projectiles/PelletProjectileDef.asset"
        );
        ProjectileTowerAttackDef pelletBurst = AssetDatabase.LoadAssetAtPath<ProjectileTowerAttackDef>(
            "Assets/Resources/Combat/Spawnables/Towers/Tower Attacks/Definitions/Projectiles/PelletBurstProjectileDef.asset"
        );

        Assert.That(pellet, Is.Not.Null);
        Assert.That(pelletBurst, Is.Not.Null);
        Assert.That(pellet.MinimumDegreesSpread, Is.EqualTo(0f));
        Assert.That(pelletBurst.MinimumDegreesSpread, Is.EqualTo(0f));
        Assert.That(pellet.DegreesSpread, Is.EqualTo(15f));
        Assert.That(pelletBurst.DegreesSpread, Is.EqualTo(45f));
    }

    private static void SetSerializedFloat(Object target, string propertyName, float value)
    {
        SerializedObject serializedObject = new(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        Assert.That(property, Is.Not.Null, $"Missing serialized property '{propertyName}'.");
        property.floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
