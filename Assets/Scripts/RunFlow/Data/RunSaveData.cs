using System;
using System.Collections.Generic;
using Cards;

namespace RunFlow
{
    [Serializable]
    public class RunSaveData
    {
        public string runId;
        public int currentHealth;
        public int maxHealth;
        public int gold;
        public List<OwnedCard> deck = new();
        public string currentNodeId;
        public List<string> completedNodeIds = new();
        public RunMapStateData mapState = new();
        public PendingRewardData pendingReward;
        public int seed;

        public bool HasCompletedNode(string nodeId)
        {
            return !string.IsNullOrWhiteSpace(nodeId) && completedNodeIds != null && completedNodeIds.Contains(nodeId);
        }

        public void MarkNodeCompleted(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
                return;

            completedNodeIds ??= new List<string>();
            if (!completedNodeIds.Contains(nodeId))
                completedNodeIds.Add(nodeId);
        }
    }

    [Serializable]
    public class RunMapStateData
    {
        public string mapTemplateId;
        public string startNodeId;
        public List<RunMapNodeData> nodes = new();
        public List<ShopPurchaseStateData> shopPurchaseStates = new();

        public RunMapNodeData FindNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || nodes == null)
                return null;

            for (int i = 0; i < nodes.Count; i++)
            {
                RunMapNodeData node = nodes[i];
                if (node != null && node.nodeId == nodeId)
                    return node;
            }

            return null;
        }

        public ShopPurchaseStateData GetOrCreateShopState(string nodeId)
        {
            shopPurchaseStates ??= new List<ShopPurchaseStateData>();

            for (int i = 0; i < shopPurchaseStates.Count; i++)
            {
                ShopPurchaseStateData state = shopPurchaseStates[i];
                if (state != null && state.nodeId == nodeId)
                    return state;
            }

            ShopPurchaseStateData created = new() { nodeId = nodeId };
            shopPurchaseStates.Add(created);
            return created;
        }
    }

    [Serializable]
    public class RunMapNodeData
    {
        public string nodeId;
        public string displayName;
        public MapNodeType nodeType;
        public string encounterId;
        public string shopInventoryId;
        public int column;
        public int lane;
        public List<string> nextNodeIds = new();

        public string DisplayNameOrFallback => string.IsNullOrWhiteSpace(displayName) ? nodeType.ToString() : displayName;
    }

    [Serializable]
    public class ShopPurchaseStateData
    {
        public string nodeId;
        public List<string> purchasedOfferIds = new();

        public bool HasPurchased(string offerId)
        {
            return !string.IsNullOrWhiteSpace(offerId) &&
                   purchasedOfferIds != null &&
                   purchasedOfferIds.Contains(offerId);
        }

        public void MarkPurchased(string offerId)
        {
            if (string.IsNullOrWhiteSpace(offerId))
                return;

            purchasedOfferIds ??= new List<string>();
            if (!purchasedOfferIds.Contains(offerId))
                purchasedOfferIds.Add(offerId);
        }
    }

    [Serializable]
    public class PendingRewardData
    {
        public string sourceNodeId;
        public List<string> offeredCardIds = new();
    }
}
