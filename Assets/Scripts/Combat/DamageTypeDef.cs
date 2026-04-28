using UnityEngine;

namespace Combat
{
    [CreateAssetMenu(menuName = "Combat/Damage Type", fileName = "New Damage Type")]
    public class DamageTypeDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public bool bypassesResistances;
    }
}
