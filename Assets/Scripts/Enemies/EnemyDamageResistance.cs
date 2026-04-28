using System;
using Combat;

namespace Enemies
{
    [Serializable]
    public class EnemyDamageResistance
    {
        public DamageTypeDef damageType;
        public bool immune;
    }
}
