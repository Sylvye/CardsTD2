using System;
using Enemies;

namespace RunFlow
{
    [Serializable]
    public class CombatSceneRequest
    {
        public string nodeId;
        public EncounterDef encounter;
        public EnemyPath pathPrefab;
        public RunSaveData run;

        public CombatSceneRequest(string nodeId, EncounterDef encounter, EnemyPath pathPrefab, RunSaveData run)
        {
            this.nodeId = nodeId;
            this.encounter = encounter;
            this.pathPrefab = pathPrefab;
            this.run = run;
        }
    }
}
