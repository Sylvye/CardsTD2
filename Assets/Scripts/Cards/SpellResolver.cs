using System.Collections.Generic;
using Enemies;
using Towers;
using UnityEngine;

namespace Cards
{
    public class SpellResolver
    {
        private readonly TowerManager towerManager;
        private readonly EnemyManager enemyManager;

        public SpellResolver(TowerManager towerManager, EnemyManager enemyManager)
        {
            this.towerManager = towerManager;
            this.enemyManager = enemyManager;
        }

        public void Resolve(SpellDef spellDef, GameObject spawnedSpellObject)
        {
            if (spellDef == null || spellDef.effects == null)
                return;

            Collider2D spellCollider = spawnedSpellObject != null
                ? spawnedSpellObject.GetComponent<Collider2D>()
                : null;

            if (spellCollider == null)
            {
                Debug.LogWarning($"Spell '{spellDef.name}' requires a Collider2D on its spawned object to resolve targets.");
                return;
            }

            foreach (SpellEffectData effect in spellDef.effects)
            {
                if (effect == null || effect.effectType == SpellEffectType.None)
                    continue;

                ResolveEffect(effect, spellCollider);
            }
        }

        private void ResolveEffect(SpellEffectData effect, Collider2D spellCollider)
        {
            switch (effect.effectType)
            {
                case SpellEffectType.HealTower:
                    ResolveTowers(effect, spellCollider, tower => tower.Heal(effect.amount));
                    break;
                case SpellEffectType.DamageEnemy:
                    ResolveEnemies(effect, spellCollider, enemy => enemy.TakeDamage(effect.amount));
                    break;
                case SpellEffectType.ApplyTowerModifier:
                    if (effect.towerModifier == null)
                        return;
                    ResolveTowers(effect, spellCollider, tower => tower.AddModifier(effect.towerModifier));
                    break;
                case SpellEffectType.ApplyEnemyModifier:
                    if (effect.enemyModifier == null)
                        return;
                    ResolveEnemies(effect, spellCollider, enemy => enemy.AddModifier(effect.enemyModifier));
                    break;
            }
        }

        private void ResolveTowers(SpellEffectData effect, Collider2D spellCollider, System.Action<TowerAgent> applyEffect)
        {
            if (towerManager == null || effect.targetType == SpellTargetType.None || effect.targetType == SpellTargetType.Enemies)
                return;

            IReadOnlyList<TowerAgent> towers = towerManager.ActiveTowers;
            for (int i = towers.Count - 1; i >= 0; i--)
            {
                TowerAgent tower = towers[i];
                if (tower == null || tower.IsDead)
                    continue;

                if (!IsInsideSpellArea(tower.transform, spellCollider))
                    continue;

                applyEffect(tower);
            }
        }

        private void ResolveEnemies(SpellEffectData effect, Collider2D spellCollider, System.Action<EnemyAgent> applyEffect)
        {
            if (enemyManager == null || effect.targetType == SpellTargetType.None || effect.targetType == SpellTargetType.Towers)
                return;

            IReadOnlyList<EnemyAgent> enemies = enemyManager.ActiveEnemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EnemyAgent enemy = enemies[i];
                if (enemy == null || enemy.IsDeadOrEscaped)
                    continue;

                if (!IsInsideSpellArea(enemy.transform, spellCollider))
                    continue;

                applyEffect(enemy);
            }
        }

        private bool IsInsideSpellArea(Transform targetTransform, Collider2D spellCollider)
        {
            if (targetTransform == null)
                return false;

            Collider2D targetCollider = targetTransform.GetComponent<Collider2D>();
            if (targetCollider != null)
                return spellCollider.Distance(targetCollider).isOverlapped;

            return spellCollider.OverlapPoint(targetTransform.position);
        }
    }
}
