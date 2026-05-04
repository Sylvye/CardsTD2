using UnityEngine;

namespace Towers
{
    public enum TowerModifierSource
    {
        Unknown,
        Base,
        Augment,
        Spell,
        Inherited
    }

    public enum TowerModifierDuration
    {
        Permanent,
        Active
    }

    public readonly struct TowerInspectorModifierEntry
    {
        public TowerInspectorModifierEntry(
            string displayName,
            Sprite icon,
            TowerModifierTone tone,
            TowerModifierSource source,
            TowerModifierDuration duration)
        {
            DisplayName = displayName;
            Icon = icon;
            Tone = tone;
            Source = source;
            Duration = duration;
        }

        public string DisplayName { get; }
        public Sprite Icon { get; }
        public TowerModifierTone Tone { get; }
        public TowerModifierSource Source { get; }
        public TowerModifierDuration Duration { get; }

        public string SourceLabel
        {
            get
            {
                return Source switch
                {
                    TowerModifierSource.Base => "Base",
                    TowerModifierSource.Augment => "Augment",
                    TowerModifierSource.Spell => "Spell",
                    TowerModifierSource.Inherited => "Inherited",
                    _ => "Effect"
                };
            }
        }
    }
}
