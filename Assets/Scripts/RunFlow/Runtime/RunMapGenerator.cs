using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunFlow
{
    public class RunMapGenerator
    {
        private readonly RunContentRepository contentRepository;

        public RunMapGenerator(RunContentRepository contentRepository)
        {
            this.contentRepository = contentRepository;
        }

        public RunMapStateData Generate(MapTemplateDef template, int seed)
        {
            RunMapStateData mapState = new()
            {
                mapTemplateId = template != null ? template.TemplateId : string.Empty,
                shopPurchaseStates = new List<ShopPurchaseStateData>(),
                nodes = new List<RunMapNodeData>()
            };

            if (template == null)
                return mapState;

            int totalPlayableNodes = Mathf.Max(1, template.totalPlayableNodes);
            int maxActivePaths = Mathf.Max(1, template.maxActivePaths);
            int playableColumns = DeterminePlayableColumns(template, totalPlayableNodes, seed);
            int intermediateColumns = Mathf.Max(0, playableColumns - 1);
            List<int> columnWidths = BuildColumnWidths(template, totalPlayableNodes - 1, intermediateColumns, maxActivePaths, seed);
            List<List<RunMapNodeData>> columns = BuildColumns(columnWidths);

            ConnectColumns(columns);
            AssignNodeTypes(template, columns, seed);
            AssignNodeContent(template, columns, seed);

            mapState.startNodeId = columns[0][0].nodeId;
            for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                List<RunMapNodeData> column = columns[columnIndex];
                for (int nodeIndex = 0; nodeIndex < column.Count; nodeIndex++)
                    mapState.nodes.Add(column[nodeIndex]);
            }

            return mapState;
        }

        private static int DeterminePlayableColumns(MapTemplateDef template, int totalPlayableNodes, int seed)
        {
            int minColumns = Mathf.Max(1, template.minColumns);
            int maxColumns = Mathf.Max(minColumns, template.maxColumns);
            int intermediateNodes = Mathf.Max(0, totalPlayableNodes - 1);
            int requiredIntermediateColumns = intermediateNodes <= 0
                ? 0
                : 1 + Mathf.CeilToInt(Mathf.Max(0, intermediateNodes - 1) / (float)Mathf.Max(1, template.maxActivePaths));
            int requiredPlayableColumns = Mathf.Max(1, requiredIntermediateColumns + 1);
            int boundedMaxColumns = Mathf.Clamp(maxColumns, requiredPlayableColumns, totalPlayableNodes);
            int boundedMinColumns = Mathf.Clamp(minColumns, requiredPlayableColumns, boundedMaxColumns);

            if (boundedMinColumns == boundedMaxColumns)
                return boundedMinColumns;

            System.Random random = new(seed ^ 0x4D415001);
            return random.Next(boundedMinColumns, boundedMaxColumns + 1);
        }

        private static List<int> BuildColumnWidths(MapTemplateDef template, int intermediateNodeCount, int intermediateColumns, int maxActivePaths, int seed)
        {
            List<int> widths = new();
            if (intermediateColumns <= 0)
                return widths;

            System.Random random = new(seed ^ 0x57494454);
            int remainingNodes = Mathf.Max(0, intermediateNodeCount);
            int previousWidth = 1;

            for (int columnIndex = 0; columnIndex < intermediateColumns; columnIndex++)
            {
                int remainingColumns = intermediateColumns - columnIndex;
                bool isLastColumn = remainingColumns == 1;
                int width;

                if (isLastColumn)
                {
                    width = Mathf.Clamp(remainingNodes, 1, 1);
                }
                else
                {
                    int minRemainingNodes = remainingColumns - 1;
                    int maxRemainingNodes = ((remainingColumns - 2) * maxActivePaths) + 1;
                    int minWidth = Mathf.Max(1, remainingNodes - maxRemainingNodes);
                    int maxWidth = Mathf.Min(maxActivePaths, remainingNodes - minRemainingNodes);

                    int targetWidth = previousWidth;
                    bool canBranch = previousWidth < maxActivePaths && maxWidth > previousWidth;
                    bool canMerge = previousWidth > 1 && minWidth < previousWidth;

                    if (canBranch && random.NextDouble() < template.branchChance)
                        targetWidth = previousWidth + 1;
                    else if (canMerge && random.NextDouble() < template.mergeChance)
                        targetWidth = previousWidth - 1;

                    width = Mathf.Clamp(targetWidth, minWidth, maxWidth);
                }

                widths.Add(width);
                remainingNodes -= width;
                previousWidth = width;
            }

            return widths;
        }

        private static List<List<RunMapNodeData>> BuildColumns(List<int> columnWidths)
        {
            List<List<RunMapNodeData>> columns = new();
            columns.Add(new List<RunMapNodeData>
            {
                new()
                {
                    nodeId = "node-start",
                    displayName = "Start",
                    nodeType = MapNodeType.Start,
                    column = 0,
                    lane = 0
                }
            });

            for (int columnIndex = 0; columnIndex < columnWidths.Count; columnIndex++)
            {
                int width = Mathf.Max(1, columnWidths[columnIndex]);
                List<RunMapNodeData> columnNodes = new();
                for (int laneIndex = 0; laneIndex < width; laneIndex++)
                {
                    columnNodes.Add(new RunMapNodeData
                    {
                        nodeId = $"node-c{columnIndex + 1}-l{laneIndex}",
                        nodeType = MapNodeType.Fight,
                        column = columnIndex + 1,
                        lane = laneIndex
                    });
                }

                columns.Add(columnNodes);
            }

            int bossColumn = columns.Count;
            columns.Add(new List<RunMapNodeData>
            {
                new()
                {
                    nodeId = "node-boss",
                    displayName = "Boss",
                    nodeType = MapNodeType.Boss,
                    column = bossColumn,
                    lane = 0
                }
            });

            return columns;
        }

        private static void ConnectColumns(List<List<RunMapNodeData>> columns)
        {
            for (int columnIndex = 0; columnIndex < columns.Count - 1; columnIndex++)
            {
                List<RunMapNodeData> currentColumn = columns[columnIndex];
                List<RunMapNodeData> nextColumn = columns[columnIndex + 1];

                for (int currentIndex = 0; currentIndex < currentColumn.Count; currentIndex++)
                {
                    int nextIndex = ResolveMappedIndex(currentIndex, currentColumn.Count, nextColumn.Count);
                    AddEdge(currentColumn[currentIndex], nextColumn[nextIndex]);
                }

                for (int nextIndex = 0; nextIndex < nextColumn.Count; nextIndex++)
                {
                    int currentIndex = ResolveMappedIndex(nextIndex, nextColumn.Count, currentColumn.Count);
                    AddEdge(currentColumn[currentIndex], nextColumn[nextIndex]);
                }
            }
        }

        private static void AssignNodeTypes(MapTemplateDef template, List<List<RunMapNodeData>> columns, int seed)
        {
            List<RunMapNodeData> middleNodes = GetMiddleNodes(columns);
            if (middleNodes.Count == 0)
                return;

            Dictionary<MapNodeType, int> counts = BuildNodeTypeCounts(template, middleNodes.Count, seed);
            List<MapNodeType> assignedTypes = new();
            foreach (KeyValuePair<MapNodeType, int> entry in counts)
            {
                for (int i = 0; i < entry.Value; i++)
                    assignedTypes.Add(entry.Key);
            }

            while (assignedTypes.Count < middleNodes.Count)
                assignedTypes.Add(MapNodeType.Fight);

            Shuffle(assignedTypes, new System.Random(seed ^ 0x54595045));

            for (int i = 0; i < middleNodes.Count; i++)
                middleNodes[i].nodeType = assignedTypes[i];
        }

        private void AssignNodeContent(MapTemplateDef template, List<List<RunMapNodeData>> columns, int seed)
        {
            List<RunMapNodeData> fightNodes = new();
            List<RunMapNodeData> minibossNodes = new();
            List<RunMapNodeData> bossNodes = new();

            for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                List<RunMapNodeData> column = columns[columnIndex];
                for (int nodeIndex = 0; nodeIndex < column.Count; nodeIndex++)
                {
                    RunMapNodeData node = column[nodeIndex];
                    if (node == null)
                        continue;

                    switch (node.nodeType)
                    {
                        case MapNodeType.Fight:
                            fightNodes.Add(node);
                            break;
                        case MapNodeType.Miniboss:
                            minibossNodes.Add(node);
                            break;
                        case MapNodeType.Boss:
                            bossNodes.Add(node);
                            break;
                        case MapNodeType.Shop:
                            node.shopInventoryId = ResolveShopInventoryId(template);
                            node.displayName = "Shop";
                            break;
                        case MapNodeType.Rest:
                            node.displayName = "Rest";
                            break;
                        case MapNodeType.Start:
                            node.displayName = "Start";
                            break;
                    }
                }
            }

            AssignEncounters(template, MapNodeType.Fight, fightNodes, seed ^ 0x46494748);
            AssignEncounters(template, MapNodeType.Miniboss, minibossNodes, seed ^ 0x4D494E49);
            AssignEncounters(template, MapNodeType.Boss, bossNodes, seed ^ 0x424F5353);
        }

        private void AssignEncounters(MapTemplateDef template, MapNodeType nodeType, List<RunMapNodeData> nodes, int seed)
        {
            if (nodes.Count == 0)
                return;

            List<WeightedEncounterCandidate> candidates = BuildEncounterCandidates(template, nodeType);
            List<EncounterDef> picks = BuildEncounterSequence(candidates, nodes.Count, seed);

            for (int i = 0; i < nodes.Count; i++)
            {
                RunMapNodeData node = nodes[i];
                EncounterDef encounter = i < picks.Count ? picks[i] : null;
                node.encounterId = contentRepository.GetEncounterId(encounter);
                node.displayName = encounter != null ? encounter.DisplayNameOrFallback : GetDefaultDisplayName(nodeType);
            }
        }

        private List<WeightedEncounterCandidate> BuildEncounterCandidates(MapTemplateDef template, MapNodeType nodeType)
        {
            List<WeightedEncounterCandidate> candidates = new();
            EncounterPoolDef encounterPool = template.GetEncounterPool(nodeType);
            if (encounterPool != null)
            {
                List<WeightedEncounterEntry> poolEntries = encounterPool.GetValidEntries();
                for (int i = 0; i < poolEntries.Count; i++)
                {
                    WeightedEncounterEntry entry = poolEntries[i];
                    candidates.Add(new WeightedEncounterCandidate(entry.encounter, entry.weight));
                }
            }

            if (candidates.Count > 0)
                return candidates;

            List<EncounterDef> encounters = contentRepository.GetEncountersByKind(GetEncounterKind(nodeType));
            for (int i = 0; i < encounters.Count; i++)
                candidates.Add(new WeightedEncounterCandidate(encounters[i], 1));

            return candidates;
        }

        private static List<EncounterDef> BuildEncounterSequence(List<WeightedEncounterCandidate> candidates, int count, int seed)
        {
            List<EncounterDef> picks = new();
            if (count <= 0 || candidates.Count == 0)
                return picks;

            System.Random random = new(seed);
            while (picks.Count < count)
            {
                List<WeightedEncounterCandidate> cycleEntries = new(candidates);
                while (cycleEntries.Count > 0 && picks.Count < count)
                {
                    int totalWeight = 0;
                    for (int i = 0; i < cycleEntries.Count; i++)
                        totalWeight += Mathf.Max(1, cycleEntries[i].weight);

                    int roll = random.Next(totalWeight);
                    int runningWeight = 0;
                    int chosenIndex = 0;
                    for (int i = 0; i < cycleEntries.Count; i++)
                    {
                        runningWeight += Mathf.Max(1, cycleEntries[i].weight);
                        if (roll < runningWeight)
                        {
                            chosenIndex = i;
                            break;
                        }
                    }

                    picks.Add(cycleEntries[chosenIndex].encounter);
                    cycleEntries.RemoveAt(chosenIndex);
                }
            }

            return picks;
        }

        private static Dictionary<MapNodeType, int> BuildNodeTypeCounts(MapTemplateDef template, int totalSlots, int seed)
        {
            Dictionary<MapNodeType, int> counts = new();
            List<NodeTypeGenerationRule> rules = new()
            {
                template.GetNodeTypeRule(MapNodeType.Fight),
                template.GetNodeTypeRule(MapNodeType.Shop),
                template.GetNodeTypeRule(MapNodeType.Rest),
                template.GetNodeTypeRule(MapNodeType.Miniboss)
            };

            int assignedCount = 0;
            int remainingCapacity = totalSlots;
            for (int i = 0; i < rules.Count; i++)
            {
                NodeTypeGenerationRule rule = rules[i];
                if (rule == null)
                    continue;

                int clampedMin = Mathf.Clamp(rule.minCount, 0, remainingCapacity);
                counts[rule.nodeType] = clampedMin;
                assignedCount += clampedMin;
                remainingCapacity -= clampedMin;
            }

            int remainingSlots = Mathf.Max(0, totalSlots - assignedCount);
            System.Random random = new(seed ^ 0x434F554E);

            while (remainingSlots > 0)
            {
                List<NodeTypeGenerationRule> candidates = new();
                int totalWeight = 0;

                for (int i = 0; i < rules.Count; i++)
                {
                    NodeTypeGenerationRule rule = rules[i];
                    if (rule == null || rule.weight <= 0)
                        continue;

                    counts.TryGetValue(rule.nodeType, out int currentCount);
                    if (currentCount >= rule.GetEffectiveMaxCount(totalSlots))
                        continue;

                    candidates.Add(rule);
                    totalWeight += rule.weight;
                }

                if (candidates.Count == 0 || totalWeight <= 0)
                {
                    counts.TryGetValue(MapNodeType.Fight, out int fightCount);
                    counts[MapNodeType.Fight] = fightCount + remainingSlots;
                    break;
                }

                int roll = random.Next(totalWeight);
                int runningWeight = 0;
                NodeTypeGenerationRule chosenRule = candidates[0];
                for (int i = 0; i < candidates.Count; i++)
                {
                    runningWeight += candidates[i].weight;
                    if (roll < runningWeight)
                    {
                        chosenRule = candidates[i];
                        break;
                    }
                }

                counts.TryGetValue(chosenRule.nodeType, out int chosenCount);
                counts[chosenRule.nodeType] = chosenCount + 1;
                remainingSlots--;
            }

            return counts;
        }

        private static List<RunMapNodeData> GetMiddleNodes(List<List<RunMapNodeData>> columns)
        {
            List<RunMapNodeData> middleNodes = new();
            for (int columnIndex = 1; columnIndex < columns.Count - 1; columnIndex++)
            {
                List<RunMapNodeData> column = columns[columnIndex];
                for (int nodeIndex = 0; nodeIndex < column.Count; nodeIndex++)
                    middleNodes.Add(column[nodeIndex]);
            }

            return middleNodes;
        }

        private string ResolveShopInventoryId(MapTemplateDef template)
        {
            ShopInventoryDef configuredInventory = template.GetShopInventory(MapNodeType.Shop);
            if (configuredInventory != null)
                return contentRepository.GetShopInventoryId(configuredInventory);

            if (template.defaultShopInventory != null)
                return contentRepository.GetShopInventoryId(template.defaultShopInventory);

            ShopInventoryDef inventory = contentRepository.GetDefaultShopInventory();
            return contentRepository.GetShopInventoryId(inventory);
        }

        private static EncounterKind GetEncounterKind(MapNodeType nodeType)
        {
            return nodeType switch
            {
                MapNodeType.Miniboss => EncounterKind.Miniboss,
                MapNodeType.Boss => EncounterKind.Boss,
                _ => EncounterKind.RegularFight
            };
        }

        private static string GetDefaultDisplayName(MapNodeType nodeType)
        {
            return nodeType switch
            {
                MapNodeType.Miniboss => "Miniboss",
                MapNodeType.Boss => "Boss",
                MapNodeType.Shop => "Shop",
                MapNodeType.Rest => "Rest",
                MapNodeType.Start => "Start",
                _ => "Fight"
            };
        }

        private static int ResolveMappedIndex(int sourceIndex, int sourceCount, int targetCount)
        {
            if (targetCount <= 1 || sourceCount <= 1)
                return 0;

            float ratio = sourceIndex / (float)(sourceCount - 1);
            return Mathf.Clamp(Mathf.RoundToInt(ratio * (targetCount - 1)), 0, targetCount - 1);
        }

        private static void AddEdge(RunMapNodeData from, RunMapNodeData to)
        {
            from.nextNodeIds ??= new List<string>();
            if (!from.nextNodeIds.Contains(to.nodeId))
                from.nextNodeIds.Add(to.nodeId);
        }

        private static void Shuffle<T>(List<T> list, System.Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }

        private readonly struct WeightedEncounterCandidate
        {
            public readonly EncounterDef encounter;
            public readonly int weight;

            public WeightedEncounterCandidate(EncounterDef encounter, int weight)
            {
                this.encounter = encounter;
                this.weight = Mathf.Max(1, weight);
            }
        }

    }
}
