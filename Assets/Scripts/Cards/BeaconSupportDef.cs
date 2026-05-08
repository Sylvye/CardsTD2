using System.Collections.Generic;
using UnityEngine;

namespace Cards
{
    [CreateAssetMenu(menuName = "Cards/Support/Beacon Definition", fileName = "BeaconSupport")]
    public sealed class BeaconSupportDef : SupportDef
    {
        [Header("Beacon")]
        public SupportBuffType buffType = SupportBuffType.None;
        public List<float> baseValues = new();
        public List<float> amplifiedValues = new();
        public Color linkColor = Color.white;

        private void Reset()
        {
            supportSubtype = SupportSubtype.Beacon;
        }

        protected override void OnValidate()
        {
            supportSubtype = SupportSubtype.Beacon;
            base.OnValidate();
        }
    }
}
