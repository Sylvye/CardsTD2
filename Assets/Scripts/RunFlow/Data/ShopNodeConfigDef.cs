using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Nodes/Shop Node Config", fileName = "ShopNodeConfig")]
    public class ShopNodeConfigDef : MapNodeConfigDef
    {
        public ShopInventoryDef shopInventory;
        public override MapNodeType NodeType => MapNodeType.Shop;
    }
}
