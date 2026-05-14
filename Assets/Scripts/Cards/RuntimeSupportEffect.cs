using Towers;
using UnityEngine;

namespace Cards
{
    public sealed class RuntimeSupportEffect : IStatModifier, ITowerInspectorDescribable
    {
        public RuntimeSupportEffect(
            string displayName,
            float healthAdd = 0f,
            float rangeAdd = 0f,
            float fireIntervalMultiplier = 1f,
            float damageAdd = 0f,
            TowerAttackModifierData attackModifier = null,
            TowerTriggeredEffect triggeredEffect = null,
            Sprite icon = null,
            TowerModifierTone tone = TowerModifierTone.Buff)
        {
            DisplayName = displayName;
            HealthAdd = healthAdd;
            RangeAdd = rangeAdd;
            FireIntervalMultiplier = Mathf.Max(0.01f, fireIntervalMultiplier);
            DamageAdd = damageAdd;
            AttackModifier = attackModifier;
            TriggeredEffect = triggeredEffect;
            Icon = icon;
            Tone = tone;
        }

        public string DisplayName { get; }
        public Sprite Icon { get; }
        public TowerModifierTone Tone { get; }
        public float HealthAdd { get; }
        public float RangeAdd { get; }
        public float FireIntervalMultiplier { get; }
        public float DamageAdd { get; }
        public TowerAttackModifierData AttackModifier { get; }
        public TowerTriggeredEffect TriggeredEffect { get; }

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
