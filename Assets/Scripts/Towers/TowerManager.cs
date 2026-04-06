using System.Collections.Generic;
using UnityEngine;
using Cards;
using Combat;

namespace Towers
{
    public class TowerManager : MonoBehaviour
    {
        [SerializeField] private Transform towerParent;

        private readonly List<TowerInstance> towers = new();

        public bool CanPlaceTower(CardDef cardDef, Vector3 position)
        {
            if (cardDef is null || cardDef.spawnableObject is null || cardDef.spawnableObject.prefab is null)
                return false;

            float newRadius = cardDef.spawnableObject.placementRadius;
            Vector2 newPos = position;

            foreach (TowerInstance tower in towers)
            {
                if (tower is null)
                    continue;

                float combinedRadius = newRadius + tower.PlacementRadius;
                Vector2 towerPos = tower.transform.position;

                if (Vector2.Distance(newPos, towerPos) < combinedRadius)
                    return false;
            }

            return true;
        }

        public TowerInstance PlaceTower(CardDef cardDef, Vector3 position)
        {
            if (cardDef is null || cardDef.spawnableObject is null || cardDef.spawnableObject.prefab is null)
                return null;

            SpawnableObjectDef spawnable = cardDef.spawnableObject;

            GameObject spawned = Instantiate(
                spawnable.prefab,
                position,
                Quaternion.identity,
                towerParent
            );

            TowerInstance tower = spawned.GetComponent<TowerInstance>();
            if (tower is not null)
            {
                tower.Initialize(spawnable.placementRadius, spawnable.effectRadius);
                towers.Add(tower);
            }

            return tower;
        }
    }
}