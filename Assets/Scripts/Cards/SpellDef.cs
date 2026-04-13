using System.Collections.Generic;
using UnityEngine;

namespace Cards
{
    [CreateAssetMenu(menuName = "Cards/Spell Definition", fileName = "New Spell")]
    public class SpellDef : SpawnableObjectDef
    {
        [Header("Spell")]
        [Min(0.01f)] public float duration = 1f;
        public List<SpellTriggeredEffect> triggeredEffects = new();
    }
}
