using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Map Node", fileName = "MapNode")]
    public class MapNodeDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public MapNodeType nodeType;
        public EncounterDef encounter;
        public ShopInventoryDef shopInventory;
        public string NodeId => string.IsNullOrWhiteSpace(id) ? name : id;
        public string DisplayNameOrFallback => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }
}
