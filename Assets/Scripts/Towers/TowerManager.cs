using System.Collections.Generic;
using Cards;
using Enemies;
using UnityEngine;

namespace Towers
{
    public class TowerManager : MonoBehaviour
    {
        [SerializeField] private Transform towerParent;
        [SerializeField] private EnemyManager enemyManager;

        private readonly List<TowerAgent> towers = new();

        public IReadOnlyList<TowerAgent> ActiveTowers => towers;

        private void Awake()
        {
            if (towerParent == null)
                towerParent = transform;

            if (enemyManager == null)
                enemyManager = FindAnyObjectByType<EnemyManager>();
        }

        public bool CanPlaceTower(CardDef cardDef, Vector3 position)
        {
            if (cardDef == null)
                return false;

            return CanPlaceTower(cardDef.GetPlacementRadius(), position);
        }

        public bool CanPlaceTower(CardInstance card, Vector3 position)
        {
            if (card == null)
                return false;

            return CanPlaceTower(card.Definition, position);
        }

        public bool CanPlaceTower(TowerDef towerDef, Vector3 position)
        {
            if (towerDef == null)
                return false;

            return CanPlaceTower(towerDef.placementRadius, position);
        }

        public bool CanPlaceTower(float placementRadius, Vector3 position)
        {
            if (placementRadius < 0f)
                return false;

            Vector2 newPos = position;

            foreach (TowerAgent tower in towers)
            {
                if (tower == null)
                    continue;

                float combinedRadius = placementRadius + tower.PlacementRadius;
                Vector2 towerPos = tower.transform.position;

                if (Vector2.Distance(newPos, towerPos) < combinedRadius)
                    return false;
            }

            foreach (SplinePathRenderer pathRenderer in FindObjectsByType<SplinePathRenderer>())
            {
                if (pathRenderer == null)
                    continue;

                EdgeCollider2D pathCollider = pathRenderer.GetComponent<EdgeCollider2D>();
                if (pathCollider == null)
                    continue;

                Vector2 closestPoint = pathCollider.ClosestPoint(newPos);
                if ((closestPoint - newPos).sqrMagnitude <= placementRadius * placementRadius)
                    return false;
            }

            return true;
        }

        public TowerAgent PlaceTower(CardDef cardDef, Vector3 position)
        {
            if (cardDef == null)
                return null;

            SpawnableObjectDef spawnable = cardDef.spawnableObject;
            if (spawnable is TowerDef towerDef)
                return PlaceTower(towerDef, position);

            if (spawnable == null || spawnable.prefab == null)
                return null;

            GameObject spawned = Instantiate(
                spawnable.prefab,
                position,
                Quaternion.identity,
                towerParent
            );

            TowerAgent tower = spawned.GetComponent<TowerAgent>();
            if (tower != null)
            {
                float placementRadius = cardDef.GetPlacementRadius();
                if (placementRadius < 0f)
                    placementRadius = 0f;

                tower.Initialize(placementRadius, spawnable.effectRadius, BuildRuntimeContext());
                RegisterTower(tower);
            }

            return tower;
        }

        public TowerAgent PlaceTower(TowerDef towerDef, Vector3 position)
        {
            if (towerDef == null || towerDef.prefab == null)
                return null;

            GameObject spawned = Instantiate(
                towerDef.prefab,
                position,
                Quaternion.identity,
                towerParent
            );

            TowerAgent tower = spawned.GetComponent<TowerAgent>();
            if (tower == null)
                return null;

            tower.Initialize(towerDef, BuildRuntimeContext());
            RegisterTower(tower);
            return tower;
        }

        public TowerAgent PlaceTower(ResolvedCardData resolvedCard, Vector3 position)
        {
            if (resolvedCard == null || resolvedCard.TowerDefinition == null || resolvedCard.TowerDefinition.prefab == null)
                return null;

            GameObject spawned = Instantiate(
                resolvedCard.TowerDefinition.prefab,
                position,
                Quaternion.identity,
                towerParent
            );

            TowerAgent tower = spawned.GetComponent<TowerAgent>();
            if (tower == null)
                return null;

            tower.Initialize(resolvedCard.TowerDefinition, BuildRuntimeContext());

            if (resolvedCard.AdditionalTowerModifiers != null)
            {
                for (int i = 0; i < resolvedCard.AdditionalTowerModifiers.Count; i++)
                {
                    IStatModifier modifier = resolvedCard.AdditionalTowerModifiers[i];
                    if (modifier != null)
                        tower.AddModifier(modifier);
                }
            }

            if (resolvedCard.RuntimeTowerAttacks != null && resolvedCard.RuntimeTowerAttacks.Count > 0)
                tower.SetRuntimeAttackDefinitions(resolvedCard.RuntimeTowerAttacks);

            RegisterTower(tower);
            return tower;
        }

        public void RegisterTower(TowerAgent tower)
        {
            if (tower == null || towers.Contains(tower))
                return;

            towers.Add(tower);
        }

        public void UnregisterTower(TowerAgent tower)
        {
            if (tower == null)
                return;

            towers.Remove(tower);
        }

        private TowerRuntimeContext BuildRuntimeContext()
        {
            return new TowerRuntimeContext(this, enemyManager);
        }
    }
}
