using UnityEngine;

namespace Cards
{
    public abstract class SupportDef : SpawnableObjectDef
    {
        [Header("Support")]
        public SupportSubtype supportSubtype;
        [Min(0f)] public float placementRadius = 0.5f;
        [Min(0f)] public float supportRadius = 2f;

        protected virtual void OnValidate()
        {
            effectRadius = supportRadius;
        }
    }
}
