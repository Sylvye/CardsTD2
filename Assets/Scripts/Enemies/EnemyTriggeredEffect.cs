using System;
using UnityEngine;

namespace Enemies
{
    [Serializable]
    public class EnemyTriggeredEffect
    {
        public EnemyTriggerType trigger = EnemyTriggerType.OnDeath;
        public EnemyEffectType effectType = EnemyEffectType.None;

        [Header("Generic Value")]
        public int amount = 0;

        [Header("Spawn Enemy")]
        public EnemyDef enemyToSpawn;
    }
}