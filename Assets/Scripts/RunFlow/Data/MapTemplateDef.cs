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
        public List<MapNodeConfigDef> nodeConfigs = new();
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

        public string TemplateId => string.IsNullOrWhiteSpace(id) ? null : id.Trim();

        public MapNodeConfigDef GetNodeConfig(MapNodeType nodeType)
        {
            if (nodeConfigs == null)
                return null;

            for (int i = 0; i < nodeConfigs.Count; i++)
            {
                MapNodeConfigDef config = nodeConfigs[i];
                if (config != null && config.NodeType == nodeType)
                    return config;
            }

            return null;
        }

        public NodeTypeGenerationRule GetNodeTypeRule(MapNodeType nodeType)
        {
            MapNodeConfigDef config = GetNodeConfig(nodeType);
            if (config?.generationRule != null)
                return config.generationRule;

            return GetDefaultRule(nodeType);
        }

        public EncounterPoolDef GetEncounterPool(MapNodeType nodeType)
        {
            return GetNodeConfig(nodeType) is CombatNodeConfigDef config ? config.encounterPool : null;
        }

        public CombatMapPoolDef GetCombatMapPool(MapNodeType nodeType)
        {
            return GetNodeConfig(nodeType) is CombatNodeConfigDef config ? config.pathPool : null;
        }

        public NodeRewardRule GetRewardRule(MapNodeType nodeType)
        {
            if (GetNodeConfig(nodeType) is CombatNodeConfigDef config)
            {
                return new NodeRewardRule
                {
                    nodeType = nodeType,
                    rewardPool = config.rewardPool,
                    goldReward = config.goldReward,
                    metaCurrencyReward = config.metaCurrencyReward
                };
            }

            return null;
        }

        public ShopInventoryDef GetShopInventory(MapNodeType nodeType)
        {
            return GetNodeConfig(nodeType) is ShopNodeConfigDef config ? config.shopInventory : null;
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
    public class NodeRewardRule
    {
        public MapNodeType nodeType;
        public CardRewardPoolDef rewardPool;
        public int goldReward;
        public int metaCurrencyReward;
    }
}
