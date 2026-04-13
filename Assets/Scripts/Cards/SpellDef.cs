using System.Collections.Generic;
using UnityEngine;

namespace Cards
{
    [CreateAssetMenu(menuName = "Cards/Spell Definition", fileName = "New Spell")]
    public class SpellDef : SpawnableObjectDef
    {
        [Header("Spell")]
        [Min(0f)] public new float effectRadius = 1f;
        public List<SpellEffectData> effects = new();
    }
}
