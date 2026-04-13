using System.Collections.Generic;
using UnityEngine;

namespace Cards
{
    [CreateAssetMenu(menuName = "Cards/Spell Definition", fileName = "New Spell")]
    public class SpellDef : SpawnableObjectDef
    {
        [Header("Spell")]
        public List<SpellEffectData> effects = new();
    }
}
