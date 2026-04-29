using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Nodes/Boss Node Config", fileName = "BossNodeConfig")]
    public class BossNodeConfigDef : CombatNodeConfigDef
    {
        public override MapNodeType NodeType => MapNodeType.Boss;
    }
}
