using UnityEngine;

namespace Enemies
{
    public struct EnemyResolvedStats
    {
        public float MoveSpeed;
        public float DamageTakenMultiplier;
        public float FlatDamageReduction;

        public EnemyResolvedStats(float moveSpeed, float damageTakenMultiplier, float flatDamageReduction = 0f)
        {
            MoveSpeed = Mathf.Max(0f, moveSpeed);
            DamageTakenMultiplier = Mathf.Max(0f, damageTakenMultiplier);
            FlatDamageReduction = Mathf.Max(0f, flatDamageReduction);
        }

        public void Clamp()
        {
            MoveSpeed = Mathf.Max(0f, MoveSpeed);
            DamageTakenMultiplier = Mathf.Max(0f, DamageTakenMultiplier);
            FlatDamageReduction = Mathf.Max(0f, FlatDamageReduction);
        }
    }
}
