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

        public void Resolve(SpellDef spellDef, Vector3 worldPosition)
        {
            if (spellDef == null || spellDef.effects == null)
                return;

            for (int i = 0; i < spellDef.effects.Count; i++)
            {
                SpellEffectData effect = spellDef.effects[i];
                if (effect == null || effect.effectType == SpellEffectType.None)
                    continue;

                ResolveEffect(spellDef, effect, worldPosition);
            }
        }

        private void ResolveEffect(SpellDef spellDef, SpellEffectData effect, Vector3 worldPosition)
        {
            switch (effect.effectType)
            {
                case SpellEffectType.HealTower:
                    ResolveTowers(spellDef, effect, worldPosition, tower => tower.Heal(effect.amount));
                    break;
                case SpellEffectType.DamageEnemy:
                    ResolveEnemies(spellDef, effect, worldPosition, enemy => enemy.TakeDamage(effect.amount));
                    break;
                case SpellEffectType.ApplyTowerModifier:
                    if (effect.towerModifier == null)
                        return;
                    ResolveTowers(spellDef, effect, worldPosition, tower => tower.AddModifier(effect.towerModifier));
                    break;
                case SpellEffectType.ApplyEnemyModifier:
                    if (effect.enemyModifier == null)
                        return;
                    ResolveEnemies(spellDef, effect, worldPosition, enemy => enemy.AddModifier(effect.enemyModifier));
                    break;
            }
        }

        private void ResolveTowers(SpellDef spellDef, SpellEffectData effect, Vector3 worldPosition, System.Action<TowerAgent> applyEffect)
        {
            if (towerManager == null || effect.targetType == SpellTargetType.None || effect.targetType == SpellTargetType.Enemies)
                return;

            float radiusSqr = spellDef.effectRadius * spellDef.effectRadius;
            for (int i = 0; i < towerManager.ActiveTowers.Count; i++)
            {
                TowerAgent tower = towerManager.ActiveTowers[i];
                if (tower == null || tower.IsDead)
                    continue;

                if ((tower.transform.position - worldPosition).sqrMagnitude > radiusSqr)
                    continue;

                applyEffect(tower);
            }
        }

        private void ResolveEnemies(SpellDef spellDef, SpellEffectData effect, Vector3 worldPosition, System.Action<EnemyAgent> applyEffect)
        {
            if (enemyManager == null || effect.targetType == SpellTargetType.None || effect.targetType == SpellTargetType.Towers)
                return;

            float radiusSqr = spellDef.effectRadius * spellDef.effectRadius;
            for (int i = 0; i < enemyManager.ActiveEnemies.Count; i++)
            {
                EnemyAgent enemy = enemyManager.ActiveEnemies[i];
                if (enemy == null || enemy.IsDeadOrEscaped)
                    continue;

                if ((enemy.transform.position - worldPosition).sqrMagnitude > radiusSqr)
                    continue;

                applyEffect(enemy);
            }
        }
    }
}
