using UnityEngine;

namespace Cards
{
    [CreateAssetMenu(menuName = "Cards/Support/Conduit Definition", fileName = "ConduitSupport")]
    public sealed class ConduitSupportDef : SupportDef
    {
        private void Reset()
        {
            supportSubtype = SupportSubtype.Conduit;
        }

        protected override void OnValidate()
        {
            supportSubtype = SupportSubtype.Conduit;
            base.OnValidate();
        }
    }
}
