using System;
using System.Collections.Generic;
using Cards;
using Relics;
using UnityEngine;

namespace RunFlow
{
    public class RunContentRepository
    {
        private readonly Dictionary<string, CardDef> cardsById = new();
        private readonly Dictionary<string, CardAugmentDef> augmentsById = new();
        private readonly Dictionary<string, RelicDef> relicsById = new();
        private readonly Dictionary<string, MapTemplateDef> mapTemplatesById = new();
        private readonly List<MapTemplateDef> mapTemplates = new();
        private readonly Dictionary<string, EncounterDef> encountersById = new();
        private readonly Dictionary<string, ShopInventoryDef> shopInventoriesById = new();
        private readonly Dictionary<string, MetaUnlockEntry> metaUnlocksById = new();
        private readonly Dictionary<string, List<MetaUnlockEntry>> cardMetaUnlocksByCardId = new();
        private readonly List<MetaUnlockEntry> metaUnlocks = new();

        private bool isLoaded;

        public IReadOnlyCollection<CardDef> Cards => cardsById.Values;
        public IReadOnlyCollection<CardAugmentDef> Augments => augmentsById.Values;
        public IReadOnlyCollection<RelicDef> Relics => relicsById.Values;

        public void Refresh()
        {
            cardsById.Clear();
            augmentsById.Clear();
            relicsById.Clear();
            mapTemplatesById.Clear();
            mapTemplates.Clear();
            encountersById.Clear();
            shopInventoriesById.Clear();
            metaUnlocksById.Clear();
            cardMetaUnlocksByCardId.Clear();
            metaUnlocks.Clear();

            LoadCards();
            LoadAugments();
            LoadRelics();
            LoadMetaUnlockCatalogs();
            LoadMapTemplates();
            LoadEncounters();
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

        public RelicDef GetRelicById(string id)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(id) && relicsById.TryGetValue(id, out RelicDef relic) ? relic : null;
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

        public ShopInventoryDef GetShopInventoryById(string id)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(id) && shopInventoriesById.TryGetValue(id, out ShopInventoryDef inventory) ? inventory : null;
        }

        public MetaUnlockEntry GetMetaUnlockById(string id)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(id) && metaUnlocksById.TryGetValue(id, out MetaUnlockEntry unlock) ? unlock : null;
        }

        public List<MetaUnlockEntry> GetMetaUnlocks()
        {
            EnsureLoaded();
            return new List<MetaUnlockEntry>(metaUnlocks);
        }

        public MetaUnlockEntry GetMetaUnlockForCard(CardDef card)
        {
            EnsureLoaded();
            string cardId = GetCardId(card);
            if (string.IsNullOrWhiteSpace(cardId) || !cardMetaUnlocksByCardId.TryGetValue(cardId, out List<MetaUnlockEntry> unlocks) || unlocks.Count == 0)
                return null;

            return unlocks[0];
        }

        public List<MetaUnlockEntry> GetMetaUnlocksForCard(CardDef card)
        {
            EnsureLoaded();
            string cardId = GetCardId(card);
            return !string.IsNullOrWhiteSpace(cardId) && cardMetaUnlocksByCardId.TryGetValue(cardId, out List<MetaUnlockEntry> unlocks)
                ? new List<MetaUnlockEntry>(unlocks)
                : new List<MetaUnlockEntry>();
        }

        public MapTemplateDef GetDefaultMapTemplate()
        {
            EnsureLoaded();
            return SelectDefaultMapTemplate(mapTemplates);
        }

        public string GetCardId(CardDef card)
        {
            return card == null ? null : string.IsNullOrWhiteSpace(card.id) ? card.name : card.id;
        }

        public string GetAugmentId(CardAugmentDef augment)
        {
            return augment == null ? null : string.IsNullOrWhiteSpace(augment.id) ? augment.name : augment.id;
        }

        public string GetRelicId(RelicDef relic)
        {
            return relic == null ? null : relic.RelicId;
        }

        public string GetEncounterId(EncounterDef encounter)
        {
            return encounter == null ? null : encounter.EncounterId;
        }

        public string GetShopInventoryId(ShopInventoryDef inventory)
        {
            return inventory == null ? null : inventory.InventoryId;
        }

        public List<EncounterDef> GetEncountersByKind(EncounterKind encounterKind)
        {
            EnsureLoaded();

            List<EncounterDef> encounters = new();
            foreach (EncounterDef encounter in encountersById.Values)
            {
                if (encounter != null && encounter.encounterKind == encounterKind)
                    encounters.Add(encounter);
            }

            return encounters;
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

        private void LoadRelics()
        {
            RelicDef[] relics = Resources.LoadAll<RelicDef>("RunFlow/Relics");
            for (int i = 0; i < relics.Length; i++)
            {
                RelicDef relic = relics[i];
                string id = GetRelicId(relic);
                if (!string.IsNullOrWhiteSpace(id))
                    relicsById[id] = relic;
            }
        }

        private void LoadMetaUnlockCatalogs()
        {
            MetaUnlockCatalogDef[] catalogs = Resources.LoadAll<MetaUnlockCatalogDef>("RunFlow/Unlocks");
            Array.Sort(catalogs, CompareMetaUnlockCatalogs);

            for (int catalogIndex = 0; catalogIndex < catalogs.Length; catalogIndex++)
            {
                MetaUnlockCatalogDef catalog = catalogs[catalogIndex];
                if (catalog?.unlocks == null)
                    continue;

                for (int unlockIndex = 0; unlockIndex < catalog.unlocks.Count; unlockIndex++)
                {
                    MetaUnlockEntry unlock = catalog.unlocks[unlockIndex];
                    if (unlock == null)
                        continue;

                    string unlockId = unlock.UnlockId;
                    if (string.IsNullOrWhiteSpace(unlockId))
                    {
                        Debug.LogWarning($"Meta unlock catalog '{catalog.name}' has an entry without an id and it will be ignored.");
                        continue;
                    }

                    if (metaUnlocksById.ContainsKey(unlockId))
                    {
                        Debug.LogWarning($"Duplicate meta unlock id '{unlockId}' found in '{catalog.name}'. The first entry will be kept.");
                        continue;
                    }

                    metaUnlocksById[unlockId] = unlock;
                    metaUnlocks.Add(unlock);
                }
            }

            for (int i = 0; i < metaUnlocks.Count; i++)
                IndexCardsForUnlock(metaUnlocks[i]);
        }

        private void IndexCardsForUnlock(MetaUnlockEntry unlock)
        {
            if (unlock == null)
                return;

            string unlockId = unlock.UnlockId;
            if (string.IsNullOrWhiteSpace(unlockId))
                return;

            if (unlock.type == MetaUnlockType.Card)
                IndexCardForUnlock(unlock.card, unlock, $"Card meta unlock '{unlockId}'");

            if (unlock.type == MetaUnlockType.UnlockGroup)
            {
                if (unlock.contents == null || unlock.contents.Count == 0)
                {
                    Debug.LogWarning($"Unlock group '{unlockId}' has no contained unlock content and will not gate card content.");
                    return;
                }

                for (int i = 0; i < unlock.contents.Count; i++)
                {
                    MetaUnlockContent content = unlock.contents[i];
                    if (content == null)
                        continue;

                    if (content.type == MetaUnlockType.Card)
                        IndexCardForUnlock(content.card, unlock, $"Contained card unlock '{content.GetDisplayName()}' in group '{unlockId}'");
                }
            }
        }

        private void IndexCardForUnlock(CardDef card, MetaUnlockEntry ownerUnlock, string context)
        {
            string cardId = GetCardId(card);
            if (string.IsNullOrWhiteSpace(cardId))
            {
                Debug.LogWarning($"{context} is missing a card and will not gate card content.");
                return;
            }

            if (!cardMetaUnlocksByCardId.TryGetValue(cardId, out List<MetaUnlockEntry> unlocks))
            {
                unlocks = new List<MetaUnlockEntry>();
                cardMetaUnlocksByCardId[cardId] = unlocks;
            }

            if (!unlocks.Contains(ownerUnlock))
                unlocks.Add(ownerUnlock);
        }

        private void LoadMapTemplates()
        {
            MapTemplateDef[] templates = Resources.LoadAll<MapTemplateDef>("RunFlow");
            Array.Sort(templates, CompareMapTemplates);
            for (int i = 0; i < templates.Length; i++)
            {
                MapTemplateDef template = templates[i];
                if (template == null)
                    continue;

                string templateId = template.TemplateId;
                if (string.IsNullOrWhiteSpace(templateId))
                {
                    Debug.LogWarning($"Map template '{template.name}' is missing an id and will be ignored. Map template ids must be set explicitly.");
                    continue;
                }

                if (mapTemplatesById.TryGetValue(templateId, out MapTemplateDef existingTemplate))
                {
                    Debug.LogWarning($"Duplicate map template id '{templateId}' found on '{template.name}'. Existing template '{existingTemplate.name}' will be kept. Map template ids must be unique.");
                    continue;
                }

                mapTemplatesById[templateId] = template;
                mapTemplates.Add(template);
            }

            mapTemplates.Sort(CompareMapTemplates);
        }

        private void LoadEncounters()
        {
            EncounterDef[] encounters = Resources.LoadAll<EncounterDef>("RunFlow/Encounters");
            for (int i = 0; i < encounters.Length; i++)
            {
                EncounterDef encounter = encounters[i];
                if (encounter == null)
                    continue;

                string encounterId = encounter.EncounterId;
                if (string.IsNullOrWhiteSpace(encounterId))
                    continue;

                if (encountersById.ContainsKey(encounterId))
                {
                    Debug.LogWarning($"Duplicate encounter id '{encounterId}' found on '{encounter.name}'. Encounter ids must be unique.");
                    continue;
                }

                encountersById[encounterId] = encounter;
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

        private static MapTemplateDef SelectDefaultMapTemplate(IReadOnlyList<MapTemplateDef> templates)
        {
            if (templates == null || templates.Count == 0)
                return null;

            List<MapTemplateDef> sortedTemplates = new();
            for (int i = 0; i < templates.Count; i++)
            {
                if (templates[i] != null)
                    sortedTemplates.Add(templates[i]);
            }

            if (sortedTemplates.Count == 0)
                return null;

            sortedTemplates.Sort(CompareMapTemplates);

            List<MapTemplateDef> defaultTemplates = new();
            for (int i = 0; i < sortedTemplates.Count; i++)
            {
                MapTemplateDef template = sortedTemplates[i];
                if (template.isDefaultStartTemplate)
                    defaultTemplates.Add(template);
            }

            if (defaultTemplates.Count == 1)
                return defaultTemplates[0];

            if (defaultTemplates.Count > 1)
            {
                defaultTemplates.Sort(CompareMapTemplates);
                Debug.LogWarning($"Multiple map templates are marked as default start templates. Using '{defaultTemplates[0].TemplateId}'.");
                return defaultTemplates[0];
            }

            Debug.LogWarning($"No map template is marked as the default start template. Using '{sortedTemplates[0].TemplateId}'.");
            return sortedTemplates[0];
        }

        private static int CompareMapTemplates(MapTemplateDef left, MapTemplateDef right)
        {
            if (ReferenceEquals(left, right))
                return 0;

            if (left == null)
                return 1;

            if (right == null)
                return -1;

            int idComparison = string.Compare(left.TemplateId, right.TemplateId, StringComparison.Ordinal);
            if (idComparison != 0)
                return idComparison;

            return string.Compare(left.name, right.name, StringComparison.Ordinal);
        }

        private static int CompareMetaUnlockCatalogs(MetaUnlockCatalogDef left, MetaUnlockCatalogDef right)
        {
            if (ReferenceEquals(left, right))
                return 0;

            if (left == null)
                return 1;

            if (right == null)
                return -1;

            int idComparison = string.Compare(left.CatalogId, right.CatalogId, StringComparison.Ordinal);
            if (idComparison != 0)
                return idComparison;

            return string.Compare(left.name, right.name, StringComparison.Ordinal);
        }
    }
}
