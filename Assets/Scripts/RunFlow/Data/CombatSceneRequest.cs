using System;
using Cards;

namespace RunFlow
{
    [Serializable]
    public class CombatSceneRequest
    {
        public string nodeId;
        public EncounterDef encounter;
        public RunSaveData run;
        public int startingMana;
        public int maxMana;
        public float manaRegenPerSecond;
        public int openingHandSize;
        public int manualDrawCost;

        public CombatSceneRequest(string nodeId, EncounterDef encounter, RunSaveData run)
        {
            this.nodeId = nodeId;
            this.encounter = encounter;
            this.run = run;
            startingMana = 0;
            maxMana = 20;
            manaRegenPerSecond = 1f;
            openingHandSize = 5;
            manualDrawCost = 2;
        }
    }
}
