using System;

namespace Combat
{
    [Serializable]
    public class CombatSessionSetup
    {
        public int StartingMana = 0;
        public int MaxMana = 20;
        public float ManaRegenPerSecond = 1f;
        public int CurrentHealth = 20;
        public int MaxHealth = 20;
        public int OpeningHandSize = 5;
    }
}
