using UnityEngine;

namespace Towers
{
    [CreateAssetMenu(menuName = "Towers/Attacks/Beam Attack", fileName = "BeamTowerAttack")]
    public class BeamTowerAttackDef : TowerAttackDef
    {
        [Min(0f)] public float damageMultiplier = 1f; // increases damage dealt to the targeted enemy by this amount each attack
        [Min(0f)] public float flatDamageBonus;
        [Min(1)] public int projectileCount = 1;
        public LineRenderer beamRendererPrefab;

        public override IAttackExecution CreateExecution(TowerAgent tower)
        {
            return new BeamAttackExecution(tower, this);
        }
    }
}
