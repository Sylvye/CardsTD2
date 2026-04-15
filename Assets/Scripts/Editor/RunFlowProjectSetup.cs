using System.Collections.Generic;
using System.Reflection;
using Cards;
using Combat;
using Enemies;
using RunFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RunFlowProjectSetup
{
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string RunMapScenePath = "Assets/Scenes/RunMap.unity";
    private const string CombatScenePath = "Assets/Scenes/Combat.unity";

    private const string RunFlowRootPath = "Assets/Resources/RunFlow";
    private const string PathsPath = RunFlowRootPath + "/Paths";
    private const string EncountersPath = RunFlowRootPath + "/Encounters";
    private const string RewardsPath = RunFlowRootPath + "/Rewards";
    private const string ShopsPath = RunFlowRootPath + "/Shops";
    private const string MapNodesPath = RunFlowRootPath + "/MapNodes";
    private const string MapsPath = RunFlowRootPath + "/Maps";
    private const string DefaultPathPrefabPath = PathsPath + "/StarterCombatPath 1.prefab";

    [MenuItem("Tools/Run Flow/Generate Scenes And Content")]
    public static void GenerateScenesAndContent()
    {
        EnsureFolders();

        GameObject pathPrefab = ExtractPathPrefab();

        List<CardDef> cards = LoadAssets<CardDef>("Assets/Resources/Combat/Cards/Definitions");
        List<CardAugmentDef> augments = LoadAssets<CardAugmentDef>("Assets/Resources/Combat/Cards/Augments");
        List<EnemyDef> enemies = LoadAssets<EnemyDef>("Assets/Resources/Combat/Enemies/Definitions");
        CardDef upgradedStarterCard = EnsureStarterUpgrade(cards);
        if (upgradedStarterCard != null && !cards.Contains(upgradedStarterCard))
            cards.Add(upgradedStarterCard);

        CardRewardPoolDef rewardPool = CreateRewardPool(cards);
        ShopInventoryDef shopInventory = CreateShopInventory(cards, augments);
        EncounterDef regularFightA = CreateEncounter("regular-fight-a", "Regular Fight I", EncounterKind.RegularFight, pathPrefab, enemies, rewardPool, 10, 1, 4, 0);
        EncounterDef regularFightB = CreateEncounter("regular-fight-b", "Regular Fight II", EncounterKind.RegularFight, pathPrefab, enemies, rewardPool, 12, 1, 6, 0);
        EncounterDef regularFightC = CreateEncounter("regular-fight-c", "Regular Fight III", EncounterKind.RegularFight, pathPrefab, enemies, rewardPool, 14, 1, 4, 1);
        EncounterDef miniboss = CreateEncounter("starter-miniboss", "Starter Miniboss", EncounterKind.Miniboss, pathPrefab, enemies, rewardPool, 20, 2, 0, 6);
        EncounterDef boss = CreateEncounter("starter-boss", "Starter Boss", EncounterKind.Boss, pathPrefab, enemies, rewardPool, 32, 4, 6, 6);
        EncounterPoolDef regularPool = CreateEncounterPool("starter-fight-pool", "Starter Fight Pool", regularFightA, regularFightB, regularFightC);
        EncounterPoolDef minibossPool = CreateEncounterPool("starter-miniboss-pool", "Starter Miniboss Pool", miniboss);
        EncounterPoolDef bossPool = CreateEncounterPool("starter-boss-pool", "Starter Boss Pool", boss);
        MapTemplateDef mapTemplate = CreateMap(cards, shopInventory, regularPool, minibossPool, bossPool);

        CreateBootstrapScene();
        CreateControllerScene<MainMenuSceneController>(MainMenuScenePath, "Main Menu");
        CreateControllerScene<RunMapSceneController>(RunMapScenePath, "Run Map");
        CreateCombatScene(regularFightA, cards);
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Run Flow", "Scenes and content generated successfully.", "OK");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder(RunFlowRootPath);
        EnsureFolder(PathsPath);
        EnsureFolder(EncountersPath);
        EnsureFolder(RewardsPath);
        EnsureFolder(ShopsPath);
        EnsureFolder(MapNodesPath);
        EnsureFolder(MapsPath);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folder = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folder);
    }

    private static GameObject ExtractPathPrefab()
    {
        EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        GameObject pathRoot = GameObject.Find("Enemy Path");
        if (pathRoot == null)
            throw new MissingReferenceException("SampleScene does not contain an 'Enemy Path' object.");

        return PrefabUtility.SaveAsPrefabAsset(pathRoot, DefaultPathPrefabPath);
    }

    private static List<T> LoadAssets<T>(string folder) where T : Object
    {
        List<T> assets = new();
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                assets.Add(asset);
        }

        return assets;
    }

    private static CardRewardPoolDef CreateRewardPool(List<CardDef> cards)
    {
        CardRewardPoolDef rewardPool = LoadOrCreateAsset<CardRewardPoolDef>($"{RewardsPath}/StarterRewardPool.asset");
        rewardPool.id = "starter-reward-pool";
        rewardPool.displayName = "Starter Reward Pool";
        rewardPool.choiceCount = Mathf.Min(3, cards.Count);
        rewardPool.cards = new List<CardDef>(cards);
        EditorUtility.SetDirty(rewardPool);
        return rewardPool;
    }

    private static ShopInventoryDef CreateShopInventory(List<CardDef> cards, List<CardAugmentDef> augments)
    {
        ShopInventoryDef shop = LoadOrCreateAsset<ShopInventoryDef>($"{ShopsPath}/StarterShop.asset");
        shop.id = "starter-shop";
        shop.displayName = "Starter Shop";
        shop.offers = new List<ShopOfferData>();

        CardDef cardOffer = cards.Count > 0 ? cards[cards.Count - 1] : null;
        CardAugmentDef augmentOffer = augments.Count > 0 ? augments[0] : null;

        shop.offers.Add(new ShopOfferData
        {
            id = "shop-card",
            displayName = cardOffer != null ? $"Buy {cardOffer.displayName}" : "Buy Card",
            offerType = ShopOfferType.Card,
            price = 18,
            card = cardOffer
        });

        shop.offers.Add(new ShopOfferData
        {
            id = "shop-augment",
            displayName = augmentOffer != null ? $"Apply {augmentOffer.displayName}" : "Apply Augment",
            offerType = ShopOfferType.Augment,
            price = 12,
            augment = augmentOffer
        });

        shop.offers.Add(new ShopOfferData
        {
            id = "shop-heal",
            displayName = "Recover 6 Health",
            offerType = ShopOfferType.Heal,
            price = 8,
            healAmount = 6
        });

        EditorUtility.SetDirty(shop);
        return shop;
    }

    private static EncounterDef CreateEncounter(
        string id,
        string displayName,
        EncounterKind kind,
        GameObject pathPrefab,
        List<EnemyDef> enemies,
        CardRewardPoolDef rewardPool,
        int goldReward,
        int metaCurrencyReward,
        int enemyACount,
        int enemyBCount)
    {
        EncounterDef encounter = LoadOrCreateAsset<EncounterDef>($"{EncountersPath}/{displayName}.asset", out bool created);
        encounter.id = id;
        encounter.displayName = displayName;
        encounter.encounterKind = kind;
        encounter.pathPrefab = pathPrefab;
        encounter.rewardPool = rewardPool;
        encounter.goldReward = goldReward;
        encounter.metaCurrencyReward = metaCurrencyReward;
        if (created)
            encounter.spawnBatches = BuildStarterSpawnBatches(enemies, enemyACount, enemyBCount);

        EditorUtility.SetDirty(encounter);
        return encounter;
    }

    private static EncounterPoolDef CreateEncounterPool(string id, string displayName, params EncounterDef[] encounters)
    {
        EncounterPoolDef encounterPool = LoadOrCreateAsset<EncounterPoolDef>($"{RunFlowRootPath}/{displayName}.asset");
        encounterPool.id = id;
        encounterPool.displayName = displayName;
        encounterPool.encounters = new List<WeightedEncounterEntry>();

        for (int i = 0; i < encounters.Length; i++)
        {
            EncounterDef encounter = encounters[i];
            if (encounter == null)
                continue;

            encounterPool.encounters.Add(new WeightedEncounterEntry
            {
                encounter = encounter,
                weight = 1
            });
        }

        EditorUtility.SetDirty(encounterPool);
        return encounterPool;
    }

    private static List<SpawnBatch> BuildStarterSpawnBatches(List<EnemyDef> enemies, int enemyACount, int enemyBCount)
    {
        List<SpawnBatch> spawnBatches = new();
        EnemyDef enemyA = enemies.Find(enemy => enemy != null && enemy.name.Contains("Enemy A"));
        EnemyDef enemyB = enemies.Find(enemy => enemy != null && enemy.name.Contains("Enemy B"));

        if (enemyACount > 0 && enemyA != null)
        {
            spawnBatches.Add(new SpawnBatch
            {
                enemyDef = enemyA,
                spawnCount = enemyACount,
                spawnInterval = 0.8f,
                waitTime = 1f
            });
        }

        if (enemyBCount > 0 && enemyB != null)
        {
            spawnBatches.Add(new SpawnBatch
            {
                enemyDef = enemyB,
                spawnCount = enemyBCount,
                spawnInterval = 1f,
                waitTime = 1.25f
            });
        }

        return spawnBatches;
    }

    private static MapTemplateDef CreateMap(
        List<CardDef> cards,
        ShopInventoryDef shopInventory,
        EncounterPoolDef regularFightPool,
        EncounterPoolDef minibossPool,
        EncounterPoolDef bossPool)
    {
        MapTemplateDef map = LoadOrCreateAsset<MapTemplateDef>($"{MapsPath}/StarterMap.asset");
        map.id = "starter-map";
        map.displayName = "Starter Act";
        map.startNode = null;
        map.nodes = new List<MapNodeDef>();
        map.startingDeck = BuildStartingDeck(cards);
        map.startingHealth = 20;
        map.maxHealth = 20;
        map.startingGold = 12;
        map.totalPlayableNodes = 8;
        map.maxActivePaths = 3;
        map.minColumns = 5;
        map.maxColumns = 6;
        map.branchChance = 0.5f;
        map.mergeChance = 0.4f;
        map.defaultShopInventory = shopInventory;
        map.nodeTypeRules = new List<NodeTypeGenerationRule>
        {
            new() { nodeType = MapNodeType.Fight, weight = 6, minCount = 0, maxCount = -1 },
            new() { nodeType = MapNodeType.Shop, weight = 2, minCount = 1, maxCount = 2 },
            new() { nodeType = MapNodeType.Rest, weight = 2, minCount = 1, maxCount = 2 },
            new() { nodeType = MapNodeType.Miniboss, weight = 1, minCount = 1, maxCount = 1 }
        };
        map.nodeEncounterPools = new List<NodeEncounterPoolBinding>
        {
            new() { nodeType = MapNodeType.Fight, encounterPool = regularFightPool },
            new() { nodeType = MapNodeType.Miniboss, encounterPool = minibossPool },
            new() { nodeType = MapNodeType.Boss, encounterPool = bossPool }
        };
        EditorUtility.SetDirty(map);
        return map;
    }

    private static MapNodeDef CreateNode(string id, string displayName, MapNodeType nodeType, EncounterDef encounter = null, ShopInventoryDef shopInventory = null)
    {
        string assetName = $"{displayName}.asset";
        MapNodeDef node = LoadOrCreateAsset<MapNodeDef>($"{MapNodesPath}/{assetName}");
        node.id = id;
        node.displayName = displayName;
        node.nodeType = nodeType;
        node.encounter = encounter;
        node.shopInventory = shopInventory;
        node.nextNodes = node.nextNodes ?? new List<MapNodeDef>();
        EditorUtility.SetDirty(node);
        return node;
    }

    private static List<CardDef> BuildStartingDeck(List<CardDef> cards)
    {
        List<CardDef> startingDeck = new();
        for (int i = 0; i < cards.Count && startingDeck.Count < 5; i++)
        {
            if (cards[i] != null)
                startingDeck.Add(cards[i]);
        }

        return startingDeck;
    }

    private static CardDef EnsureStarterUpgrade(List<CardDef> cards)
    {
        CardDef baseCard = cards.Find(card => card != null && card.id == "A");
        if (baseCard == null)
            return null;

        CardDef upgradedCard = LoadOrCreateAsset<CardDef>("Assets/Resources/Combat/Cards/Definitions/Starter Basic Turret Plus.asset");
        upgradedCard.id = "A_PLUS";
        upgradedCard.displayName = $"{baseCard.displayName}+";
        upgradedCard.icon = baseCard.icon;
        upgradedCard.type = baseCard.type;
        upgradedCard.description = $"{baseCard.description} Upgraded for stronger rest-site scaling.";
        upgradedCard.baseManaCost = Mathf.Max(0, baseCard.baseManaCost - 1);
        upgradedCard.cardFamilyId = string.IsNullOrWhiteSpace(baseCard.cardFamilyId) ? baseCard.id : baseCard.cardFamilyId;
        upgradedCard.baseTier = baseCard.baseTier;
        upgradedCard.upgradeTier = 2;
        upgradedCard.baseAugmentSlots = Mathf.Max(1, baseCard.baseAugmentSlots);
        upgradedCard.spawnableObject = baseCard.spawnableObject;
        upgradedCard.effects = new List<CardEffectData>();
        if (baseCard.effects != null)
        {
            for (int i = 0; i < baseCard.effects.Count; i++)
            {
                CardEffectData effect = baseCard.effects[i];
                if (effect != null)
                    upgradedCard.effects.Add(effect.Clone());
            }
        }

        upgradedCard.nextUpgradeDef = null;
        baseCard.nextUpgradeDef = upgradedCard;
        EditorUtility.SetDirty(upgradedCard);
        EditorUtility.SetDirty(baseCard);
        return upgradedCard;
    }

    private static void CreateBootstrapScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera("Bootstrap Camera");
        _ = new GameObject("Bootstrap Controller", typeof(BootstrapSceneController));
        EditorSceneManager.SaveScene(scene, BootstrapScenePath);
    }

    private static void CreateControllerScene<T>(string scenePath, string cameraName) where T : Component
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera(cameraName);
        _ = new GameObject(typeof(T).Name, typeof(T));
        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static void CreateCombatScene(EncounterDef debugEncounter, List<CardDef> cards)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CombatScenePath) != null)
            AssetDatabase.DeleteAsset(CombatScenePath);

        AssetDatabase.CopyAsset(SampleScenePath, CombatScenePath);
        AssetDatabase.Refresh();

        Scene combatScene = EditorSceneManager.OpenScene(CombatScenePath, OpenSceneMode.Single);
        GameObject existingPath = GameObject.Find("Enemy Path");
        if (existingPath != null)
            Object.DestroyImmediate(existingPath);

        CombatSessionDriver combatSessionDriver = Object.FindFirstObjectByType<CombatSessionDriver>();
        HandViewDriver handViewDriver = Object.FindFirstObjectByType<HandViewDriver>();
        EnemySpawner enemySpawner = Object.FindFirstObjectByType<EnemySpawner>();
        EnemyManager enemyManager = Object.FindFirstObjectByType<EnemyManager>();

        GameObject pathAnchor = GameObject.Find("Path Anchor");
        if (pathAnchor == null)
            pathAnchor = new GameObject("Path Anchor");

        CombatSceneBootstrapper bootstrapper = new GameObject("Combat Scene Bootstrapper").AddComponent<CombatSceneBootstrapper>();
        CombatOutcomeWatcher outcomeWatcher = new GameObject("Combat Outcome Watcher").AddComponent<CombatOutcomeWatcher>();

        SetField(bootstrapper, "combatSessionDriver", combatSessionDriver);
        SetField(bootstrapper, "handViewDriver", handViewDriver);
        SetField(bootstrapper, "enemySpawner", enemySpawner);
        SetField(bootstrapper, "pathAnchor", pathAnchor.transform);
        SetField(bootstrapper, "debugEncounter", debugEncounter);
        SetField(bootstrapper, "debugDeck", BuildOwnedCardDeck(cards));
        SetField(bootstrapper, "debugCurrentHealth", 20);
        SetField(bootstrapper, "debugMaxHealth", 20);

        SetField(outcomeWatcher, "combatSessionDriver", combatSessionDriver);
        SetField(outcomeWatcher, "enemySpawner", enemySpawner);
        SetField(outcomeWatcher, "enemyManager", enemyManager);

        SetField(handViewDriver, "autoInitializeOnStart", false);
        SetField(enemySpawner, "startOnPlay", false);

        EditorSceneManager.MarkSceneDirty(combatScene);
        EditorSceneManager.SaveScene(combatScene);
    }

    private static void CreateCamera(string name)
    {
        GameObject cameraObject = new(name, typeof(Camera), typeof(AudioListener));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.05f, 0.07f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.tag = "MainCamera";
    }

    private static void UpdateBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(BootstrapScenePath, true),
            new EditorBuildSettingsScene(MainMenuScenePath, true),
            new EditorBuildSettingsScene(RunMapScenePath, true),
            new EditorBuildSettingsScene(CombatScenePath, true)
        };
    }

    private static List<OwnedCard> BuildOwnedCardDeck(List<CardDef> cards)
    {
        List<OwnedCard> deck = new();
        List<CardDef> startingDeck = BuildStartingDeck(cards);
        for (int i = 0; i < startingDeck.Count; i++)
            deck.Add(new OwnedCard(startingDeck[i]));

        return deck;
    }

    private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        return LoadOrCreateAsset<T>(path, out _);
    }

    private static T LoadOrCreateAsset<T>(string path, out bool created) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            created = false;
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        created = true;
        return asset;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        if (target == null)
            return;

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        field?.SetValue(target, value);
        if (target is Object unityObject)
            EditorUtility.SetDirty(unityObject);
    }
}
