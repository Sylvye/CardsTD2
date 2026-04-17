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
        public int ManualDrawCost = 2;

        public CombatSessionSetup Clone()
        {
            return new CombatSessionSetup
            {
                StartingMana = StartingMana,
                MaxMana = MaxMana,
                ManaRegenPerSecond = ManaRegenPerSecond,
                CurrentHealth = CurrentHealth,
                MaxHealth = MaxHealth,
                OpeningHandSize = OpeningHandSize,
                ManualDrawCost = ManualDrawCost
            };
        }
    }
}
