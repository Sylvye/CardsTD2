using System;

namespace RunFlow
{
    [Serializable]
    public class CombatSceneResult
    {
        public string nodeId;
        public bool victory;
        public int remainingHealth;

        public CombatSceneResult(string nodeId, bool victory, int remainingHealth)
        {
            this.nodeId = nodeId;
            this.victory = victory;
            this.remainingHealth = remainingHealth;
        }
    }
}
