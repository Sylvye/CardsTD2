using UnityEngine;

namespace Towers
{
    public class TowerInstance : MonoBehaviour
    {
        public float PlacementRadius { get; private set; }
        public float Range { get; private set; }

        public void Initialize(float placementRadius, float range)
        {
            PlacementRadius = placementRadius;
            Range = range;
        }
    }
}