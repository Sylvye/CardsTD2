using UnityEngine;
using System.Collections.Generic;

namespace Enemies
{
    [CreateAssetMenu(menuName = "Enemies/Enemy Definition", fileName = "New Enemy")]
    public class EnemyDef : ScriptableObject
    {
        public EnemyAgent prefab;
        public float maxHealth = 10f;
        public float moveSpeed = 2f;
        public int lifeDamage = 1;
        public Color damageFlashColor = Color.red;
        [Min(0f)] public float damageFlashDuration = 0.1f;

        public List<EnemyTriggeredEffect> triggeredEffects = new();
    }
}
