using UnityEngine;

namespace Enemies
{
    [CreateAssetMenu(menuName = "Enemies/Modifiers/Flat Stat Modifier", fileName = "EnemyFlatStatModifier")]
    public class EnemyFlatStatModifierDef : EnemyStatModifierDef
    {
        [Header("Additive")]
        public float moveSpeedAdd = 0f;
        public float damageTakenMultiplierAdd = 0f;

        [Header("Multipliers")]
        public float moveSpeedMultiplier = 1f;
        public float damageTakenMultiplier = 1f;

        public override void ModifyStats(EnemyAgent enemy, ref EnemyResolvedStats stats)
        {
            stats.MoveSpeed = (stats.MoveSpeed + moveSpeedAdd) * Mathf.Max(0f, moveSpeedMultiplier);
            stats.DamageTakenMultiplier = (stats.DamageTakenMultiplier + damageTakenMultiplierAdd) * Mathf.Max(0f, damageTakenMultiplier);
            stats.Clamp();
        }
    }
}
