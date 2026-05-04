using UnityEngine;

namespace Towers
{
    public abstract class TowerStatModifierDef : ScriptableObject, IStatModifier
    {
        [Header("Inspector")]
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private TowerModifierTone tone = TowerModifierTone.Buff;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public Sprite Icon => icon;
        public TowerModifierTone Tone => tone;

        public abstract void ModifyStats(TowerAgent tower, ref TowerResolvedStats stats);
    }
}
