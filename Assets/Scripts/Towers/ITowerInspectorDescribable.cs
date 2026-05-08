using UnityEngine;

namespace Towers
{
    public interface ITowerInspectorDescribable
    {
        string DisplayName { get; }
        Sprite Icon { get; }
        TowerModifierTone Tone { get; }
    }
}
