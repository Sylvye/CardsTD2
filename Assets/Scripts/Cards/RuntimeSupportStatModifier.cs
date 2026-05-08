using Towers;
using UnityEngine;

namespace Cards
{
    internal sealed class RuntimeSupportStatModifier : IStatModifier, ITowerInspectorDescribable
    {
        public RuntimeSupportStatModifier(
            string displayName,
            float healthAdd = 0f,
            float rangeAdd = 0f,
            float fireIntervalMultiplier = 1f,
            float damageAdd = 0f)
        {
            DisplayName = displayName;
            HealthAdd = healthAdd;
            RangeAdd = rangeAdd;
            FireIntervalMultiplier = Mathf.Max(0.01f, fireIntervalMultiplier);
            DamageAdd = damageAdd;
        }

        public string DisplayName { get; }
        public Sprite Icon => null;
        public TowerModifierTone Tone => TowerModifierTone.Buff;
        public float HealthAdd { get; }
        public float RangeAdd { get; }
        public float FireIntervalMultiplier { get; }
        public float DamageAdd { get; }

        public void ModifyStats(TowerAgent tower, ref TowerResolvedStats stats)
        {
            stats.MaxHealth += HealthAdd;
            stats.Range += RangeAdd;
            stats.FireInterval *= FireIntervalMultiplier;
            stats.Damage += DamageAdd;
            stats.Clamp();
        }
    }
}
