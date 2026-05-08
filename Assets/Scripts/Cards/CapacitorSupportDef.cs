using UnityEngine;

namespace Cards
{
    [CreateAssetMenu(menuName = "Cards/Support/Capacitor Definition", fileName = "CapacitorSupport")]
    public sealed class CapacitorSupportDef : SupportDef
    {
        [Header("Capacitor")]
        [Min(1)] public int effectiveConnectionReduction = 1;

        private void Reset()
        {
            supportSubtype = SupportSubtype.Capacitor;
        }

        protected override void OnValidate()
        {
            supportSubtype = SupportSubtype.Capacitor;
            effectiveConnectionReduction = Mathf.Max(1, effectiveConnectionReduction);
            base.OnValidate();
        }
    }
}
