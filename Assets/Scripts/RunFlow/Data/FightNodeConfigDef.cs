using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Nodes/Fight Node Config", fileName = "FightNodeConfig")]
    public class FightNodeConfigDef : CombatNodeConfigDef
    {
        public override MapNodeType NodeType => MapNodeType.Fight;
    }
}
