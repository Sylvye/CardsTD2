using UnityEngine;
using System.Collections.Generic;
using Combat;

namespace Enemies
{
    public enum EnemyDamageResponseType
    {
        Normal,
        Weakness,
        Resistance
    }

    public readonly struct EnemyDamageResponse
    {
        public EnemyDamageResponse(float amount, EnemyDamageResponseType responseType)
        {
            Amount = amount;
            ResponseType = responseType;
        }

        public float Amount { get; }
        public EnemyDamageResponseType ResponseType { get; }
    }

    [CreateAssetMenu(menuName = "Enemies/Enemy Definition", fileName = "New Enemy")]
    public class EnemyDef : ScriptableObject
    {
        public EnemyAgent prefab;
        public float maxHealth = 10f;
        public float moveSpeed = 2f;
        public int lifeDamage = 1;

        [Header("Damage Flash")]
        public Color damageFlashColor = Color.red;
        public Color resistedDamageFlashColor = Color.gray;
        public Color weaknessDamageFlashColor = Color.yellow;
        [Min(0f)] public float damageFlashDuration = 0.1f;

        [Header("Damage Type Responses")]
        public List<DamageTypeDef> weaknesses = new();
        public List<EnemyDamageResistance> resistances = new();

        public List<EnemyTriggeredEffect> triggeredEffects = new();

        public float ApplyDamageTypeResponses(float amount, DamageTypeDef damageType)
        {
            return ResolveDamageTypeResponse(amount, damageType).Amount;
        }

        public EnemyDamageResponse ResolveDamageTypeResponse(float amount, DamageTypeDef damageType)
        {
            if (amount <= 0f)
                return new EnemyDamageResponse(0f, EnemyDamageResponseType.Normal);

            if (damageType == null)
                return new EnemyDamageResponse(amount, EnemyDamageResponseType.Normal);

            float adjustedAmount = amount;
            bool hasWeakness = HasWeakness(damageType);
            if (hasWeakness)
                adjustedAmount *= 2f;

            if (!damageType.bypassesResistances)
            {
                EnemyDamageResistance resistance = GetResistance(damageType);
                if (resistance != null)
                {
                    if (resistance.immune)
                        return new EnemyDamageResponse(0f, EnemyDamageResponseType.Resistance);

                    adjustedAmount = Mathf.Floor(adjustedAmount * 0.5f);
                    return new EnemyDamageResponse(Mathf.Max(0f, adjustedAmount), EnemyDamageResponseType.Resistance);
                }
            }

            return new EnemyDamageResponse(
                Mathf.Max(0f, adjustedAmount),
                hasWeakness ? EnemyDamageResponseType.Weakness : EnemyDamageResponseType.Normal
            );
        }

        public bool HasWeakness(DamageTypeDef damageType)
        {
            if (damageType == null || weaknesses == null)
                return false;

            for (int i = 0; i < weaknesses.Count; i++)
            {
                if (weaknesses[i] == damageType)
                    return true;
            }

            return false;
        }

        public EnemyDamageResistance GetResistance(DamageTypeDef damageType)
        {
            if (damageType == null || resistances == null)
                return null;

            for (int i = 0; i < resistances.Count; i++)
            {
                EnemyDamageResistance resistance = resistances[i];
                if (resistance != null && resistance.damageType == damageType)
                    return resistance;
            }

            return null;
        }
    }
}
