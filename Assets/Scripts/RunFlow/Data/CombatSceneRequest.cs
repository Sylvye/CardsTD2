using System;
using Cards;
using Combat;

namespace RunFlow
{
    [Serializable]
    public class CombatSceneRequest
    {
        public string nodeId;
        public EncounterDef encounter;
        public RunSaveData run;
        public CombatSessionSetup sessionOverrides;

        public CombatSceneRequest(string nodeId, EncounterDef encounter, RunSaveData run)
        {
            this.nodeId = nodeId;
            this.encounter = encounter;
            this.run = run;
        }
    }
}
