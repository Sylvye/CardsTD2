using System.Collections.Generic;
using Cards;
using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Map Template", fileName = "MapTemplate")]
    public class MapTemplateDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public MapNodeDef startNode;
        public List<MapNodeDef> nodes = new();
        public List<CardDef> startingDeck = new();
        public int startingHealth = 20;
        public int maxHealth = 20;
        public int startingGold;

        public string TemplateId => string.IsNullOrWhiteSpace(id) ? name : id;

        public MapNodeDef FindNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || nodes == null)
                return null;

            for (int i = 0; i < nodes.Count; i++)
            {
                MapNodeDef node = nodes[i];
                if (node != null && node.NodeId == nodeId)
                    return node;
            }

            return null;
        }
    }
}
