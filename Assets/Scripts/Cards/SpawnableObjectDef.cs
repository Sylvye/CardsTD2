using UnityEngine;

namespace Cards
{
    [CreateAssetMenu(menuName = "Cards/Spawnable Object Definition", fileName = "New Spawnable Object")]
    public class SpawnableObjectDef : ScriptableObject
    {
        [Header("Spawnable Fields")]
        public GameObject prefab;
        public float effectRadius = 1f;
    }
}
