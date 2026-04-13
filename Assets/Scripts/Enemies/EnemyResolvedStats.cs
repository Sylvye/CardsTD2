using UnityEngine;

namespace Enemies
{
    public struct EnemyResolvedStats
    {
        public float MoveSpeed;
        public float DamageTakenMultiplier;

        public EnemyResolvedStats(float moveSpeed, float damageTakenMultiplier)
        {
            MoveSpeed = Mathf.Max(0f, moveSpeed);
            DamageTakenMultiplier = Mathf.Max(0f, damageTakenMultiplier);
        }

        public void Clamp()
        {
            MoveSpeed = Mathf.Max(0f, MoveSpeed);
            DamageTakenMultiplier = Mathf.Max(0f, DamageTakenMultiplier);
        }
    }
}
