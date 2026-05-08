using UnityEngine;

namespace Cards
{
    [CreateAssetMenu(menuName = "Cards/Support/Amplifier Definition", fileName = "AmplifierSupport")]
    public sealed class AmplifierSupportDef : SupportDef
    {
        private void Reset()
        {
            supportSubtype = SupportSubtype.Amplifier;
        }

        protected override void OnValidate()
        {
            supportSubtype = SupportSubtype.Amplifier;
            base.OnValidate();
        }
    }
}
