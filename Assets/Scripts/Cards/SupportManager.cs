using System.Collections.Generic;
using Enemies;
using Towers;
using UnityEngine;

namespace Cards
{
    public sealed class SupportManager : MonoBehaviour
    {
        private readonly struct SupportSegmentKey
        {
            public SupportSegmentKey(SupportAgent beacon, Component from, Component to)
            {
                Beacon = beacon;
                From = from;
                To = to;
            }

            public SupportAgent Beacon { get; }
            public Component From { get; }
            public Component To { get; }
        }

        private sealed class SupportPath
        {
            public SupportPath(SupportAgent beacon, TowerAgent tower, float value, Color color, List<SupportAgent> nodes)
            {
                Beacon = beacon;
                Tower = tower;
                Value = value;
                Color = color;
                Nodes = nodes;
            }

            public SupportAgent Beacon { get; }
            public TowerAgent Tower { get; }
            public float Value { get; }
            public Color Color { get; }
            public List<SupportAgent> Nodes { get; }
        }

        [SerializeField] private Transform supportParent;
        [SerializeField] private TowerManager towerManager;

        private readonly List<SupportAgent> supports = new();
        private readonly Dictionary<SupportSegmentKey, LineRenderer> activeSegments = new();

        public IReadOnlyList<SupportAgent> ActiveSupports => supports;

        private void Awake()
        {
            if (supportParent == null)
                supportParent = transform;

            if (towerManager == null)
                towerManager = FindAnyObjectByType<TowerManager>();
        }

        public bool CanPlaceSupport(CardInstance card, Vector3 position)
        {
            if (card?.ResolvedData == null || card.ResolvedData.SupportCardMode != SupportCardMode.Spawnable)
                return false;

            return CanPlaceSupport(card.ResolvedData.SupportDefinition, position);
        }

        public bool CanPlaceSupport(SupportDef supportDef, Vector3 position)
        {
            if (supportDef == null)
                return false;

            if (supportDef.supportSubtype is not (SupportSubtype.Beacon or SupportSubtype.Conduit))
                return false;

            return CanPlaceCircularObject(supportDef.placementRadius, position, null);
        }

        public bool CanApplySupportUpgrade(CardInstance card, Vector3 position)
        {
            return card?.ResolvedData != null && CanApplySupportUpgrade(card.ResolvedData, position);
        }

        public bool CanApplySupportUpgrade(ResolvedCardData resolvedCard, Vector3 position)
        {
            if (resolvedCard == null || resolvedCard.SupportCardMode != SupportCardMode.Upgrade)
                return false;

            return CanApplySupportUpgrade(resolvedCard, FindSupportAtPoint(position));
        }

        public SupportAgent PlaceSupport(ResolvedCardData resolvedCard, Vector3 position)
        {
            if (resolvedCard == null || resolvedCard.SupportCardMode != SupportCardMode.Spawnable)
                return null;

            return PlaceSupport(resolvedCard.SupportDefinition, position);
        }

        public SupportAgent PlaceSupport(SupportDef supportDef, Vector3 position)
        {
            if (!CanPlaceSupport(supportDef, position))
                return null;

            GameObject supportObject = supportDef.prefab != null
                ? Instantiate(supportDef.prefab, position, Quaternion.identity, supportParent)
                : CreateFallbackSupportObject(supportDef, position);

            SupportAgent agent = supportObject.GetComponent<SupportAgent>();
            if (agent == null)
                agent = supportObject.AddComponent<SupportAgent>();

            agent.Initialize(supportDef, this);
            RegisterSupport(agent);
            return agent;
        }

        public bool ApplySupportUpgrade(ResolvedCardData resolvedCard, Vector3 position)
        {
            if (resolvedCard == null || resolvedCard.SupportCardMode != SupportCardMode.Upgrade)
                return false;

            return ApplySupportUpgrade(resolvedCard, FindSupportAtPoint(position));
        }

        public void RegisterSupport(SupportAgent support)
        {
            if (support == null || supports.Contains(support))
                return;

            supports.Add(support);
            RefreshSupportNetwork();
        }

        public void UnregisterSupport(SupportAgent support)
        {
            if (support == null)
                return;

            if (supports.Remove(support))
                RefreshSupportNetwork();
        }

        public void NotifyTowerLayoutChanged()
        {
            RefreshSupportNetwork();
        }

        public void ShowPlacementPreview(Vector3 towerPosition)
        {
            ClearSupportHighlights();

            for (int i = 0; i < supports.Count; i++)
            {
                SupportAgent support = supports[i];
                if (support == null)
                    continue;

                if (!(support.IsBeacon || support.IsConduit))
                {
                    support.HideAllRadiusPreviews();
                    continue;
                }

                float radius = support.EffectiveSupportRadius;
                if ((support.transform.position - towerPosition).sqrMagnitude <= radius * radius)
                    support.ShowRadiusPreview(GetSupportDisplayColor(support));
                else
                    support.HideAllRadiusPreviews();
            }
        }

        public void HidePlacementPreview()
        {
            for (int i = 0; i < supports.Count; i++)
                supports[i]?.HideAllRadiusPreviews();
        }

        public void ShowUpgradePreview(CardInstance card, Vector3 position, Color validColor, Color invalidColor)
        {
            ClearSupportHighlights();
            if (card?.ResolvedData == null)
                return;

            SupportAgent hoveredSupport = FindSupportAtPoint(position);

            for (int i = 0; i < supports.Count; i++)
            {
                SupportAgent support = supports[i];
                if (support == null)
                    continue;

                if (!CanTargetSupportUpgrade(card.ResolvedData, support))
                    continue;

                Color previewColor = support == hoveredSupport && CanApplySupportUpgrade(card.ResolvedData, support)
                    ? validColor
                    : new Color(validColor.r, validColor.g, validColor.b, Mathf.Max(0.55f, validColor.a * 0.55f));
                support.SetHighlighted(true, previewColor);
            }

            if (hoveredSupport != null && !CanApplySupportUpgrade(card.ResolvedData, hoveredSupport))
                hoveredSupport.SetHighlighted(true, invalidColor);
        }

        public void HideUpgradePreview()
        {
            ClearSupportHighlights();
        }

        public SupportAgent FindSupportAtPoint(Vector3 position, SupportSubtype requiredSubtype = SupportSubtype.None)
        {
            ClearDeadSupports();

            SupportAgent nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < supports.Count; i++)
            {
                SupportAgent support = supports[i];
                if (support == null)
                    continue;

                if (requiredSubtype != SupportSubtype.None && support.SupportSubtype != requiredSubtype)
                    continue;

                if (!support.ContainsPoint(position))
                    continue;

                float distance = (support.transform.position - position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = support;
                }
            }

            return nearest;
        }

        private bool CanApplySupportUpgrade(ResolvedCardData resolvedCard, SupportAgent target)
        {
            if (!CanTargetSupportUpgrade(resolvedCard, target))
                return false;

            return resolvedCard.SupportSubtype switch
            {
                SupportSubtype.Amplifier when target.IsBeacon => !target.IsAmplified,
                SupportSubtype.Amplifier when target.IsConduit => resolvedCard.SupportConduitRangeIncrease > 0f,
                SupportSubtype.Capacitor when target.IsBeacon => resolvedCard.SupportEffectiveConnectionReduction > 0,
                _ => false
            };
        }

        private bool CanTargetSupportUpgrade(ResolvedCardData resolvedCard, SupportAgent target)
        {
            if (resolvedCard == null || resolvedCard.SupportCardMode != SupportCardMode.Upgrade || target == null)
                return false;

            SupportUpgradeTargetMask targetMask = target.SupportSubtype switch
            {
                SupportSubtype.Beacon => SupportUpgradeTargetMask.Beacon,
                SupportSubtype.Conduit => SupportUpgradeTargetMask.Conduit,
                _ => SupportUpgradeTargetMask.None
            };

            return targetMask != SupportUpgradeTargetMask.None
                && (resolvedCard.SupportUpgradeTargets & targetMask) != 0;
        }

        private bool ApplySupportUpgrade(ResolvedCardData resolvedCard, SupportAgent target)
        {
            if (!CanApplySupportUpgrade(resolvedCard, target))
                return false;

            switch (resolvedCard.SupportSubtype)
            {
                case SupportSubtype.Amplifier:
                    if (target.IsBeacon)
                        target.SetAmplified();
                    else if (target.IsConduit)
                        target.AddConduitRangeBonus(resolvedCard.SupportConduitRangeIncrease);
                    break;

                case SupportSubtype.Capacitor:
                    target.AddCapacitorCharge(resolvedCard.SupportEffectiveConnectionReduction);
                    break;

                default:
                    return false;
            }

            RefreshSupportNetwork();
            return true;
        }

        private GameObject CreateFallbackSupportObject(SupportDef supportDef, Vector3 position)
        {
            GameObject supportObject = new(string.IsNullOrWhiteSpace(supportDef.name) ? "Support" : supportDef.name);
            supportObject.transform.SetParent(supportParent, false);
            supportObject.transform.position = position;
            return supportObject;
        }

        private bool CanPlaceCircularObject(float placementRadius, Vector3 position, SupportAgent ignoredSupport)
        {
            if (placementRadius < 0f)
                return false;

            Vector2 newPos = position;

            if (towerManager != null)
            {
                IReadOnlyList<TowerAgent> towers = towerManager.ActiveTowers;
                for (int i = 0; i < towers.Count; i++)
                {
                    TowerAgent tower = towers[i];
                    if (tower == null)
                        continue;

                    float combinedRadius = placementRadius + tower.PlacementRadius;
                    if (Vector2.Distance(newPos, tower.transform.position) < combinedRadius)
                        return false;
                }
            }

            for (int i = 0; i < supports.Count; i++)
            {
                SupportAgent support = supports[i];
                if (support == null || support == ignoredSupport)
                    continue;

                float combinedRadius = placementRadius + support.PlacementRadius;
                if (Vector2.Distance(newPos, support.transform.position) < combinedRadius)
                    return false;
            }

            foreach (SplinePathRenderer pathRenderer in FindObjectsByType<SplinePathRenderer>())
            {
                if (pathRenderer == null)
                    continue;

                EdgeCollider2D pathCollider = pathRenderer.GetComponent<EdgeCollider2D>();
                if (pathCollider == null)
                    continue;

                Vector2 closestPoint = pathCollider.ClosestPoint(newPos);
                if ((closestPoint - newPos).sqrMagnitude <= placementRadius * placementRadius)
                    return false;
            }

            return true;
        }

        private void RefreshSupportNetwork()
        {
            ClearDeadSupports();

            IReadOnlyList<TowerAgent> towers = towerManager != null ? towerManager.ActiveTowers : System.Array.Empty<TowerAgent>();
            for (int i = 0; i < towers.Count; i++)
                towers[i]?.SetSupportState(null, null, null);

            Dictionary<TowerAgent, List<SupportPath>> applicationsByTower = new();
            List<SupportAgent> beacons = new();

            for (int i = 0; i < supports.Count; i++)
            {
                SupportAgent support = supports[i];
                if (support != null && support.IsBeacon)
                    beacons.Add(support);
            }

            for (int i = 0; i < beacons.Count; i++)
                AccumulateBeaconApplications(beacons[i], applicationsByTower);

            foreach (KeyValuePair<TowerAgent, List<SupportPath>> pair in applicationsByTower)
                ApplySupportApplications(pair.Key, pair.Value);

            RefreshSegments(applicationsByTower);
        }

        private void AccumulateBeaconApplications(
            SupportAgent beacon,
            Dictionary<TowerAgent, List<SupportPath>> applicationsByTower)
        {
            BeaconSupportDef beaconDef = beacon != null ? beacon.BeaconDefinition : null;
            if (beaconDef == null || towerManager == null)
                return;

            float beaconRadius = beacon.EffectiveSupportRadius;
            List<TowerAgent> directlyConnectedTowers = GetTowersWithinRadius(beacon.transform.position, beaconRadius);
            List<SupportAgent> directlyConnectedConduits = GetSupportsWithinRadius(beacon.transform.position, beaconRadius, SupportSubtype.Conduit);
            int effectiveConnectionCount = Mathf.Max(1, directlyConnectedTowers.Count + directlyConnectedConduits.Count - beacon.CapacitorCharge);
            float effectValue = ResolveBeaconValue(beaconDef, beacon.IsAmplified, effectiveConnectionCount);
            if (effectValue <= 0f || beaconDef.buffType == SupportBuffType.None)
                return;

            Dictionary<TowerAgent, List<SupportAgent>> bestPaths = new();
            Queue<List<SupportAgent>> conduitQueue = new();
            HashSet<SupportAgent> visitedConduits = new();

            for (int i = 0; i < directlyConnectedTowers.Count; i++)
            {
                TowerAgent tower = directlyConnectedTowers[i];
                if (tower != null && !tower.IsDead)
                    bestPaths[tower] = new List<SupportAgent> { beacon };
            }

            for (int i = 0; i < directlyConnectedConduits.Count; i++)
            {
                SupportAgent conduit = directlyConnectedConduits[i];
                if (conduit == null)
                    continue;

                visitedConduits.Add(conduit);
                conduitQueue.Enqueue(new List<SupportAgent> { beacon, conduit });
            }

            while (conduitQueue.Count > 0)
            {
                List<SupportAgent> path = conduitQueue.Dequeue();
                SupportAgent conduit = path[path.Count - 1];
                if (conduit == null)
                    continue;

                List<TowerAgent> nearbyTowers = GetTowersWithinRadius(conduit.transform.position, conduit.EffectiveSupportRadius);
                for (int i = 0; i < nearbyTowers.Count; i++)
                {
                    TowerAgent tower = nearbyTowers[i];
                    if (tower == null || tower.IsDead)
                        continue;

                    if (!bestPaths.TryGetValue(tower, out List<SupportAgent> currentPath) || currentPath.Count > path.Count)
                        bestPaths[tower] = new List<SupportAgent>(path);
                }

                List<SupportAgent> nearbyConduits = GetSupportsWithinRadius(conduit.transform.position, conduit.EffectiveSupportRadius, SupportSubtype.Conduit);
                for (int i = 0; i < nearbyConduits.Count; i++)
                {
                    SupportAgent nextConduit = nearbyConduits[i];
                    if (nextConduit == null || nextConduit == conduit)
                        continue;

                    if (visitedConduits.Contains(nextConduit))
                        continue;

                    visitedConduits.Add(nextConduit);
                    List<SupportAgent> nextPath = new(path) { nextConduit };
                    conduitQueue.Enqueue(nextPath);
                }
            }

            foreach (KeyValuePair<TowerAgent, List<SupportAgent>> pair in bestPaths)
            {
                if (!applicationsByTower.TryGetValue(pair.Key, out List<SupportPath> towerPaths))
                {
                    towerPaths = new List<SupportPath>();
                    applicationsByTower[pair.Key] = towerPaths;
                }

                towerPaths.Add(new SupportPath(beacon, pair.Key, effectValue, GetSupportDisplayColor(beacon), pair.Value));
            }
        }

        private void ApplySupportApplications(TowerAgent tower, List<SupportPath> applications)
        {
            if (tower == null)
                return;

            List<IStatModifier> statModifiers = new();
            List<TowerAttackModifierData> attackModifiers = new();
            List<TowerTriggeredEffect> triggeredEffects = new();

            float rangeAdd = 0f;
            float damageAdd = 0f;
            float fireIntervalMultiplier = 1f;
            int pierceAdd = 0;
            float projectileCountMultiplier = 1f;

            for (int i = 0; i < applications.Count; i++)
            {
                SupportPath application = applications[i];
                if (application?.Beacon?.BeaconDefinition == null)
                    continue;

                switch (application.Beacon.BeaconDefinition.buffType)
                {
                    case SupportBuffType.RangeAdd:
                        rangeAdd += application.Value;
                        break;
                    case SupportBuffType.DamageAdd:
                        damageAdd += application.Value;
                        break;
                    case SupportBuffType.AttackSpeedMultiplier:
                        fireIntervalMultiplier *= 1f / Mathf.Max(0.01f, application.Value);
                        break;
                    case SupportBuffType.ProjectileCountMultiplier:
                        projectileCountMultiplier *= Mathf.Max(0f, application.Value);
                        break;
                    case SupportBuffType.PierceAdd:
                        pierceAdd += Mathf.RoundToInt(application.Value);
                        break;
                    case SupportBuffType.OnHitSplashRadius:
                        triggeredEffects.Add(new TowerTriggeredEffect
                        {
                            trigger = TowerTriggerType.OnHit,
                            effectType = TowerEffectType.SplashDamageFromHit,
                            radius = application.Value
                        });
                        break;
                    case SupportBuffType.OnKillGainMana:
                        triggeredEffects.Add(new TowerTriggeredEffect
                        {
                            trigger = TowerTriggerType.OnKill,
                            effectType = TowerEffectType.GainMana,
                            amount = application.Value
                        });
                        break;
                }
            }

            if (rangeAdd != 0f)
            {
                statModifiers.Add(new RuntimeSupportStatModifier(
                    $"{FormatSignedStatValue(rangeAdd)} Range",
                    rangeAdd: rangeAdd));
            }

            if (damageAdd != 0f)
            {
                statModifiers.Add(new RuntimeSupportStatModifier(
                    $"{FormatSignedStatValue(damageAdd)} Damage",
                    damageAdd: damageAdd));
            }

            if (!Mathf.Approximately(fireIntervalMultiplier, 1f))
            {
                float attackSpeedMultiplier = 1f / Mathf.Max(0.01f, fireIntervalMultiplier);
                statModifiers.Add(new RuntimeSupportStatModifier(
                    $"Attack Speed x{FormatMultiplierValue(attackSpeedMultiplier)}",
                    fireIntervalMultiplier: fireIntervalMultiplier));
            }

            if (pierceAdd != 0 || !Mathf.Approximately(projectileCountMultiplier, 1f))
            {
                attackModifiers.Add(new TowerAttackModifierData
                {
                    pierceDelta = pierceAdd,
                    projectileCountMultiplier = projectileCountMultiplier,
                    beamProjectileCountMultiplier = projectileCountMultiplier
                });
            }

            tower.SetSupportState(statModifiers, attackModifiers, triggeredEffects);
        }

        private void RefreshSegments(Dictionary<TowerAgent, List<SupportPath>> applicationsByTower)
        {
            HashSet<SupportSegmentKey> usedKeys = new();

            foreach (KeyValuePair<TowerAgent, List<SupportPath>> pair in applicationsByTower)
            {
                TowerAgent tower = pair.Key;
                if (tower == null)
                    continue;

                List<SupportPath> paths = pair.Value;
                for (int i = 0; i < paths.Count; i++)
                {
                    SupportPath path = paths[i];
                    if (path == null || path.Beacon == null || path.Nodes == null || path.Nodes.Count == 0)
                        continue;

                    for (int nodeIndex = 0; nodeIndex < path.Nodes.Count - 1; nodeIndex++)
                    {
                        SupportAgent from = path.Nodes[nodeIndex];
                        SupportAgent to = path.Nodes[nodeIndex + 1];
                        if (from == null || to == null)
                            continue;

                        SupportSegmentKey key = new(path.Beacon, from, to);
                        usedKeys.Add(key);
                        UpdateSegment(key, from.transform.position, to.transform.position, path.Color);
                    }

                    SupportAgent lastNode = path.Nodes[path.Nodes.Count - 1];
                    if (lastNode == null)
                        continue;

                    SupportSegmentKey towerKey = new(path.Beacon, lastNode, tower);
                    usedKeys.Add(towerKey);
                    UpdateSegment(towerKey, lastNode.transform.position, tower.transform.position, path.Color);
                }
            }

            List<SupportSegmentKey> toRemove = new();
            foreach (KeyValuePair<SupportSegmentKey, LineRenderer> pair in activeSegments)
            {
                if (!usedKeys.Contains(pair.Key))
                    toRemove.Add(pair.Key);
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                SupportSegmentKey key = toRemove[i];
                if (!activeSegments.TryGetValue(key, out LineRenderer renderer))
                    continue;

                if (renderer != null)
                    Destroy(renderer.gameObject);

                activeSegments.Remove(key);
            }
        }

        private void UpdateSegment(SupportSegmentKey key, Vector3 from, Vector3 to, Color color)
        {
            if (!activeSegments.TryGetValue(key, out LineRenderer renderer) || renderer == null)
            {
                string beaconName = key.Beacon != null ? key.Beacon.name : "Beacon";
                string fromName = key.From != null ? key.From.name : "From";
                string toName = key.To != null ? key.To.name : "To";
                GameObject segmentObject = new($"SupportLink_{beaconName}_{fromName}_{toName}");
                segmentObject.transform.SetParent(transform, false);
                renderer = segmentObject.AddComponent<LineRenderer>();
                renderer.positionCount = 2;
                renderer.useWorldSpace = true;
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.widthMultiplier = 0.06f;
                activeSegments[key] = renderer;
            }

            renderer.startColor = color;
            renderer.endColor = color;
            renderer.enabled = true;
            renderer.SetPosition(0, from);
            renderer.SetPosition(1, to);
        }

        private List<TowerAgent> GetTowersWithinRadius(Vector3 center, float radius)
        {
            List<TowerAgent> result = new();
            if (towerManager == null)
                return result;

            float radiusSqr = radius * radius;
            IReadOnlyList<TowerAgent> towers = towerManager.ActiveTowers;
            for (int i = 0; i < towers.Count; i++)
            {
                TowerAgent tower = towers[i];
                if (tower == null || tower.IsDead)
                    continue;

                if ((tower.transform.position - center).sqrMagnitude <= radiusSqr)
                    result.Add(tower);
            }

            return result;
        }

        private List<SupportAgent> GetSupportsWithinRadius(Vector3 center, float radius, SupportSubtype subtype)
        {
            List<SupportAgent> result = new();
            float radiusSqr = radius * radius;
            for (int i = 0; i < supports.Count; i++)
            {
                SupportAgent support = supports[i];
                if (support == null || support.SupportSubtype != subtype)
                    continue;

                if ((support.transform.position - center).sqrMagnitude <= radiusSqr)
                    result.Add(support);
            }

            return result;
        }

        private static float ResolveBeaconValue(BeaconSupportDef beaconDef, bool amplified, int effectiveConnectionCount)
        {
            List<float> values = amplified && beaconDef.amplifiedValues != null && beaconDef.amplifiedValues.Count > 0
                ? beaconDef.amplifiedValues
                : beaconDef.baseValues;

            if (values == null || values.Count == 0)
                return 0f;

            int index = Mathf.Clamp(effectiveConnectionCount - 1, 0, values.Count - 1);
            return values[index];
        }

        private static Color GetSupportDisplayColor(SupportAgent support)
        {
            if (support?.BeaconDefinition != null)
                return support.BeaconDefinition.linkColor;

            return support != null && support.IsConduit
                ? new Color(0.7f, 0.7f, 0.7f, 0.95f)
                : new Color(0.95f, 0.95f, 0.95f, 0.95f);
        }

        private static string FormatSignedStatValue(float value)
        {
            return value >= 0f
                ? $"+{FormatMultiplierValue(value)}"
                : $"-{FormatMultiplierValue(Mathf.Abs(value))}";
        }

        private static string FormatMultiplierValue(float value)
        {
            return Mathf.Approximately(value, Mathf.Round(value))
                ? Mathf.RoundToInt(value).ToString()
                : value.ToString("0.##");
        }

        private void ClearDeadSupports()
        {
            for (int i = supports.Count - 1; i >= 0; i--)
            {
                if (supports[i] == null)
                    supports.RemoveAt(i);
            }
        }

        private void ClearSupportHighlights()
        {
            for (int i = 0; i < supports.Count; i++)
                supports[i]?.SetHighlighted(false, default);
        }
    }
}
