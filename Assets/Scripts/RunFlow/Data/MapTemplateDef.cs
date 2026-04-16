using System;
using System.Collections.Generic;
using Cards;
using UnityEngine;

namespace RunFlow
{
    [CreateAssetMenu(menuName = "Run Flow/Map Template", fileName = "MapTemplate")]
    public class MapTemplateDef : ScriptableObject
    {
        public string id;
        public bool isDefaultStartTemplate;
        public string displayName;
        public MapTemplateDef nextActTemplate;
        public MapNodeDef startNode;
        public List<MapNodeDef> nodes = new();
        public List<CardDef> startingDeck = new();
        public int startingHealth = 20;
        public int maxHealth = 20;
        public int startingGold;
        public int totalPlayableNodes = 8;
        public int maxActivePaths = 3;
        public int minColumns = 5;
        public int maxColumns = 7;
        [Range(0f, 1f)] public float branchChance = 0.45f;
        [Range(0f, 1f)] public float mergeChance = 0.35f;
        public ShopInventoryDef defaultShopInventory;
        public List<NodeTypeGenerationRule> nodeTypeRules = new();
        public List<NodeEncounterPoolBinding> nodeEncounterPools = new();

        public string TemplateId => string.IsNullOrWhiteSpace(id) ? null : id.Trim();

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

        public NodeTypeGenerationRule GetNodeTypeRule(MapNodeType nodeType)
        {
            if (nodeTypeRules != null)
            {
                for (int i = 0; i < nodeTypeRules.Count; i++)
                {
                    NodeTypeGenerationRule rule = nodeTypeRules[i];
                    if (rule != null && rule.nodeType == nodeType)
                        return rule;
                }
            }

            return GetDefaultRule(nodeType);
        }

        public EncounterPoolDef GetEncounterPool(MapNodeType nodeType)
        {
            if (nodeEncounterPools == null)
                return null;

            for (int i = 0; i < nodeEncounterPools.Count; i++)
            {
                NodeEncounterPoolBinding binding = nodeEncounterPools[i];
                if (binding != null && binding.nodeType == nodeType)
                    return binding.encounterPool;
            }

            return null;
        }

        private void OnValidate()
        {
            id = string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
            totalPlayableNodes = Mathf.Max(1, totalPlayableNodes);
            maxActivePaths = Mathf.Max(1, maxActivePaths);
            minColumns = Mathf.Max(1, minColumns);
            maxColumns = Mathf.Max(minColumns, maxColumns);
            startingHealth = Mathf.Max(1, startingHealth);
            maxHealth = Mathf.Max(startingHealth, maxHealth);
            startingGold = Mathf.Max(0, startingGold);
        }

        private static NodeTypeGenerationRule GetDefaultRule(MapNodeType nodeType)
        {
            return nodeType switch
            {
                MapNodeType.Fight => new NodeTypeGenerationRule
                {
                    nodeType = MapNodeType.Fight,
                    weight = 6,
                    minCount = 0,
                    maxCount = -1
                },
                MapNodeType.Shop => new NodeTypeGenerationRule
                {
                    nodeType = MapNodeType.Shop,
                    weight = 2,
                    minCount = 1,
                    maxCount = 2
                },
                MapNodeType.Rest => new NodeTypeGenerationRule
                {
                    nodeType = MapNodeType.Rest,
                    weight = 2,
                    minCount = 1,
                    maxCount = 2
                },
                MapNodeType.Miniboss => new NodeTypeGenerationRule
                {
                    nodeType = MapNodeType.Miniboss,
                    weight = 1,
                    minCount = 0,
                    maxCount = 1
                },
                _ => null
            };
        }
    }

    [Serializable]
    public class NodeTypeGenerationRule
    {
        public MapNodeType nodeType;
        public int weight = 1;
        public int minCount;
        public int maxCount = -1;

        public int GetEffectiveMaxCount(int totalSlots)
        {
            if (maxCount < 0)
                return Mathf.Max(0, totalSlots);

            return Mathf.Max(0, maxCount);
        }
    }

    [Serializable]
    public class NodeEncounterPoolBinding
    {
        public MapNodeType nodeType;
        public EncounterPoolDef encounterPool;
    }
}
