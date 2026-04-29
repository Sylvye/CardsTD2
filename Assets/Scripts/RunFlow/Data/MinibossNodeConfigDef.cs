using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Nodes/Miniboss Node Config", fileName = "MinibossNodeConfig")]
    public class MinibossNodeConfigDef : CombatNodeConfigDef
    {
        public override MapNodeType NodeType => MapNodeType.Miniboss;
    }
}
