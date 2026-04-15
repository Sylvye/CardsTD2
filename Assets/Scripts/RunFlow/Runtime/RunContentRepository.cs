using System.Collections.Generic;
using Cards;
using UnityEngine;

namespace RunFlow
{
    public class RunContentRepository
    {
        private readonly Dictionary<string, CardDef> cardsById = new();
        private readonly Dictionary<string, CardAugmentDef> augmentsById = new();
        private readonly Dictionary<string, MapTemplateDef> mapTemplatesById = new();
        private readonly Dictionary<string, EncounterDef> encountersById = new();
        private readonly Dictionary<string, CardRewardPoolDef> rewardPoolsById = new();
        private readonly Dictionary<string, ShopInventoryDef> shopInventoriesById = new();

        private bool isLoaded;

        public IReadOnlyCollection<CardDef> Cards => cardsById.Values;

        public void Refresh()
        {
            cardsById.Clear();
            augmentsById.Clear();
            mapTemplatesById.Clear();
            encountersById.Clear();
            rewardPoolsById.Clear();
            shopInventoriesById.Clear();

            LoadCards();
            LoadAugments();
            LoadMapTemplates();
            LoadEncounters();
            LoadRewardPools();
            LoadShopInventories();

            isLoaded = true;
        }

        public CardDef GetCardById(string id)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(id) && cardsById.TryGetValue(id, out CardDef card) ? card : null;
        }

        public CardAugmentDef GetAugmentById(string id)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(id) && augmentsById.TryGetValue(id, out CardAugmentDef augment) ? augment : null;
        }

        public MapTemplateDef GetMapTemplateById(string id)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(id) && mapTemplatesById.TryGetValue(id, out MapTemplateDef template) ? template : null;
        }

        public EncounterDef GetEncounterById(string id)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(id) && encountersById.TryGetValue(id, out EncounterDef encounter) ? encounter : null;
        }

        public MapTemplateDef GetDefaultMapTemplate()
        {
            EnsureLoaded();

            if (mapTemplatesById.TryGetValue("starter-map", out MapTemplateDef starterMap))
                return starterMap;

            foreach (MapTemplateDef template in mapTemplatesById.Values)
                return template;

            return null;
        }

        public string GetCardId(CardDef card)
        {
            return card == null ? null : string.IsNullOrWhiteSpace(card.id) ? card.name : card.id;
        }

        public string GetAugmentId(CardAugmentDef augment)
        {
            return augment == null ? null : string.IsNullOrWhiteSpace(augment.id) ? augment.name : augment.id;
        }

        private void EnsureLoaded()
        {
            if (!isLoaded)
                Refresh();
        }

        private void LoadCards()
        {
            CardDef[] cards = Resources.LoadAll<CardDef>("Combat/Cards/Definitions");
            for (int i = 0; i < cards.Length; i++)
            {
                CardDef card = cards[i];
                string id = GetCardId(card);
                if (!string.IsNullOrWhiteSpace(id))
                    cardsById[id] = card;
            }
        }

        private void LoadAugments()
        {
            CardAugmentDef[] augments = Resources.LoadAll<CardAugmentDef>("Combat/Cards/Augments");
            for (int i = 0; i < augments.Length; i++)
            {
                CardAugmentDef augment = augments[i];
                string id = GetAugmentId(augment);
                if (!string.IsNullOrWhiteSpace(id))
                    augmentsById[id] = augment;
            }
        }

        private void LoadMapTemplates()
        {
            MapTemplateDef[] templates = Resources.LoadAll<MapTemplateDef>("RunFlow/Maps");
            for (int i = 0; i < templates.Length; i++)
            {
                MapTemplateDef template = templates[i];
                if (template != null)
                    mapTemplatesById[template.TemplateId] = template;
            }
        }

        private void LoadEncounters()
        {
            EncounterDef[] encounters = Resources.LoadAll<EncounterDef>("RunFlow/Encounters");
            for (int i = 0; i < encounters.Length; i++)
            {
                EncounterDef encounter = encounters[i];
                if (encounter != null)
                    encountersById[encounter.EncounterId] = encounter;
            }
        }

        private void LoadRewardPools()
        {
            CardRewardPoolDef[] rewardPools = Resources.LoadAll<CardRewardPoolDef>("RunFlow/Rewards");
            for (int i = 0; i < rewardPools.Length; i++)
            {
                CardRewardPoolDef rewardPool = rewardPools[i];
                if (rewardPool != null)
                    rewardPoolsById[rewardPool.PoolId] = rewardPool;
            }
        }

        private void LoadShopInventories()
        {
            ShopInventoryDef[] inventories = Resources.LoadAll<ShopInventoryDef>("RunFlow/Shops");
            for (int i = 0; i < inventories.Length; i++)
            {
                ShopInventoryDef inventory = inventories[i];
                if (inventory != null)
                    shopInventoriesById[inventory.InventoryId] = inventory;
            }
        }
    }
}
