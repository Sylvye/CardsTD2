using UnityEngine;

namespace Towers
{
    [CreateAssetMenu(menuName = "Towers/Attacks/Summon Attack", fileName = "SummonTowerAttack")]
    public class SummonTowerAttackDef : TowerAttackDef
    {
        public TowerSummonedUnit summonPrefab;
        [Min(1)] public int maxActiveSummons = 1;
        [Min(0f)] public float spawnRadius = 0.5f;
        [Min(0.01f)] public float summonLifetime = 8f;
        [Min(0f)] public float summonRange = 2.5f;
        [Min(0f)] public float summonDamage = 1f;
        [Min(0.01f)] public float summonFireInterval = 0.75f;

        public override IAttackExecution CreateExecution(TowerAgent tower)
        {
            return new SummonAttackExecution(tower, this);
        }
    }
}
