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
            SpawnableObjectDef spawnable = cardDef.spawnableObject;
            
            if (spawnable.prefab is null)
                return false;

            foreach (TowerInstance tower in towers)
            {
                if (tower is null)
                    continue;
                
                float combinedRadius = spawnable.placementRadius + tower.PlacementRadius;
                if (Vector3.Distance(position, tower.transform.position) < combinedRadius)
                    return false;
            }

            return true;
        }

        public TowerInstance PlaceTower(CardDef cardDef, Vector3 position)
        {
            SpawnableObjectDef spawnable = cardDef.spawnableObject;
            
            if (spawnable.prefab is null)
                return null;

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