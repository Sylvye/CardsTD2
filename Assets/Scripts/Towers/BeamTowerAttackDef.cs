using UnityEngine;

namespace Towers
{
    [CreateAssetMenu(menuName = "Towers/Attacks/Beam Attack", fileName = "BeamTowerAttack")]
    public class BeamTowerAttackDef : TowerAttackDef
    {
        [Min(0.01f)] public float tickInterval = 0.2f;
        [Min(0f)] public float damageMultiplier = 1f;
        [Min(0f)] public float flatDamageBonus;

        public override IAttackExecution CreateExecution(TowerAgent tower)
        {
            return new BeamAttackExecution(tower, this);
        }
    }
}
