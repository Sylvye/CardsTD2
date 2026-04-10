using UnityEngine;
using System.Collections.Generic;

namespace Towers
{
    [CreateAssetMenu(menuName = "Towers/Attacks/Summon Attack", fileName = "SummonTowerAttack")]
    public class SummonTowerAttackDef : TowerAttackDef
    {
        [Tooltip("Prefab must include a MonoBehaviour implementing ITowerSummon.")]
        public MonoBehaviour summonPrefab;
        [Min(1)] public int maxActiveSummons = 1;
        [Min(0f)] public float spawnRadius = 0.5f;
        [Min(0.01f)] public float summonLifetime = 8f;
        [Tooltip("Optional full tower definition to initialize summon host stats/attacks.")]
        public TowerDef summonTowerDef;
        [Tooltip("Optional runtime attack overrides for summon hosts.")]
        public List<TowerAttackDef> summonAttacks = new();

        public override IAttackExecution CreateExecution(TowerAgent tower)
        {
            return new SummonAttackExecution(tower, this);
        }
    }
}
