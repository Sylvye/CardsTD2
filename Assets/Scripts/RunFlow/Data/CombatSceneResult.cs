using System;

namespace RunFlow
{
    [Serializable]
    public class CombatSceneResult
    {
        public string nodeId;
        public EncounterDef encounter;
        public bool victory;
        public int remainingHealth;

        public CombatSceneResult(string nodeId, EncounterDef encounter, bool victory, int remainingHealth)
        {
            this.nodeId = nodeId;
            this.encounter = encounter;
            this.victory = victory;
            this.remainingHealth = remainingHealth;
        }
    }
}
