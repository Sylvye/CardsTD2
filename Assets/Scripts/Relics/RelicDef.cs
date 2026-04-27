using System.Collections.Generic;
using UnityEngine;

namespace Relics
{
    [CreateAssetMenu(menuName = "Relics/Relic Definition", fileName = "New Relic")]
    public class RelicDef : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        [TextArea(2, 5)] public string description;
        public Sprite icon;

        [Header("Effects")]
        public List<RelicEffectDef> effects = new();

        public string RelicId => string.IsNullOrWhiteSpace(id) ? name : id;
        public string DisplayNameOrFallback => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}
