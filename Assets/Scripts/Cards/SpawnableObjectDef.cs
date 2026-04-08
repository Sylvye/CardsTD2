using UnityEngine;

namespace Cards
{
    [CreateAssetMenu(menuName = "Cards/Spawnable Object Definition", fileName = "New Spawnable Object")]
    public class SpawnableObjectDef : ScriptableObject
    {
        [Header("Spawn")]
        public GameObject prefab;

        [Header("Placement")]
        public float placementRadius = 0.5f;

        [Header("Preview / Area")]
        public float effectRadius = 1f;
    }
}