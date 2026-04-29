using Cards;
using UnityEngine;

namespace RunFlow
{
    public abstract class MapNodeConfigDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public NodeTypeGenerationRule generationRule = new();

        public abstract MapNodeType NodeType { get; }
        public string ConfigId => string.IsNullOrWhiteSpace(id) ? name : id;
        public string DisplayNameOrFallback => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        protected virtual void OnValidate()
        {
            id = string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
            generationRule ??= new NodeTypeGenerationRule();
            generationRule.nodeType = NodeType;
        }
    }

    public abstract class CombatNodeConfigDef : MapNodeConfigDef
    {
        public EncounterPoolDef encounterPool;
        public CombatMapPoolDef pathPool;
        public CardRewardPoolDef rewardPool;
        public int goldReward;
        public int metaCurrencyReward;

        protected override void OnValidate()
        {
            base.OnValidate();
            goldReward = Mathf.Max(0, goldReward);
            metaCurrencyReward = Mathf.Max(0, metaCurrencyReward);
        }
    }
}
