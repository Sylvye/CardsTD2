using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Nodes/Rest Node Config", fileName = "RestNodeConfig")]
    public class RestNodeConfigDef : MapNodeConfigDef
    {
        public override MapNodeType NodeType => MapNodeType.Rest;
    }
}
