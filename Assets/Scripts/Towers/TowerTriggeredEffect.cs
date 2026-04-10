using System;
using UnityEngine;

namespace Towers
{
    [Serializable]
    public class TowerTriggeredEffect
    {
        public TowerTriggerType trigger = TowerTriggerType.OnHit;
        public TowerEffectType effectType = TowerEffectType.None;

        [Header("Generic Value")]
        public float amount = 0f;
    }
}
