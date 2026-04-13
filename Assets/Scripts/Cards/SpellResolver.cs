using Combat;
using Enemies;
using Towers;
using UnityEngine;

namespace Cards
{
    public class SpellResolver
    {
        private readonly TowerManager towerManager;
        private readonly EnemyManager enemyManager;
        private readonly IPlayerEffects playerEffects;

        public SpellResolver(TowerManager towerManager, EnemyManager enemyManager, IPlayerEffects playerEffects)
        {
            this.towerManager = towerManager;
            this.enemyManager = enemyManager;
            this.playerEffects = playerEffects;
        }

        public void Resolve(SpellDef spellDef, GameObject spawnedSpellObject)
        {
            if (spellDef == null || spawnedSpellObject == null)
                return;

            Collider2D spellCollider = spawnedSpellObject.GetComponent<Collider2D>();
            if (spellCollider == null)
            {
                Debug.LogWarning($"Spell '{spellDef.name}' requires a Collider2D on its spawned object to initialize its zone runtime.");
                return;
            }

            SpellZoneRuntime zoneRuntime = spawnedSpellObject.GetComponent<SpellZoneRuntime>();
            if (zoneRuntime == null)
                zoneRuntime = spawnedSpellObject.AddComponent<SpellZoneRuntime>();

            zoneRuntime.Initialize(spellDef, spellCollider, towerManager, enemyManager, playerEffects);
        }
    }
}
