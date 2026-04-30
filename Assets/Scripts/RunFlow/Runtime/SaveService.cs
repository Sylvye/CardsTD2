using System;
using System.Collections.Generic;
using System.IO;
using Cards;
using Relics;
using UnityEngine;

namespace RunFlow
{
    public class SaveService
    {
        private const string ProfileFileName = "profile.json";

        private readonly RunContentRepository contentRepository;
        private readonly string saveDirectory;

        public SaveService(RunContentRepository contentRepository, string overrideSaveDirectory = null)
        {
            this.contentRepository = contentRepository;
            saveDirectory = string.IsNullOrWhiteSpace(overrideSaveDirectory)
                ? Path.Combine(Application.persistentDataPath, "RunFlow")
                : overrideSaveDirectory;
            Directory.CreateDirectory(saveDirectory);
        }

        public ProfileSaveData LoadProfile()
        {
            string path = Path.Combine(saveDirectory, ProfileFileName);
            if (!File.Exists(path))
                return new ProfileSaveData();

            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return new ProfileSaveData();

            ProfileSaveData profile = JsonUtility.FromJson<ProfileSaveData>(json) ?? new ProfileSaveData();
            profile.activeRunId = string.IsNullOrWhiteSpace(profile.activeRunId) ? null : profile.activeRunId;
            return profile;
        }

        public void SaveProfile(ProfileSaveData profile)
        {
            if (profile == null)
                return;

            string path = Path.Combine(saveDirectory, ProfileFileName);
            string json = JsonUtility.ToJson(profile, true);
            File.WriteAllText(path, json);
        }

        public RunSaveData LoadRun(string runId)
        {
            if (string.IsNullOrWhiteSpace(runId))
                return null;

            string path = GetRunPath(runId);
            if (!File.Exists(path))
                return null;

            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            RunFileData fileData = JsonUtility.FromJson<RunFileData>(json);
            return fileData != null ? DeserializeRun(fileData) : null;
        }

        public void SaveRun(RunSaveData run)
        {
            if (run == null || string.IsNullOrWhiteSpace(run.runId))
                return;

            RunFileData fileData = SerializeRun(run);
            string json = JsonUtility.ToJson(fileData, true);
            File.WriteAllText(GetRunPath(run.runId), json);
        }

        public void DeleteRun(string runId)
        {
            string path = GetRunPath(runId);
            if (File.Exists(path))
                File.Delete(path);
        }

        private string GetRunPath(string runId)
        {
            return Path.Combine(saveDirectory, $"run_{runId}.json");
        }

        private RunFileData SerializeRun(RunSaveData run)
        {
            RunFileData fileData = new()
            {
                runId = run.runId,
                currentHealth = run.currentHealth,
                maxHealth = run.maxHealth,
                gold = run.gold,
                currentNodeId = run.currentNodeId,
                completedNodeIds = run.completedNodeIds ?? new List<string>(),
                mapState = run.mapState ?? new RunMapStateData(),
                pendingReward = run.pendingReward,
                queuedNextMapTemplateId = run.queuedNextMapTemplateId,
                endRunAfterPendingReward = run.endRunAfterPendingReward,
                seed = run.seed
            };

            fileData.deck = new List<OwnedCardFileData>();
            fileData.ownedAugments = new List<OwnedAugmentFileData>();
            fileData.ownedRelicIds = new List<string>();
            if (run.deck != null)
            {
                for (int i = 0; i < run.deck.Count; i++)
                {
                    OwnedCard ownedCard = run.deck[i];
                    if (ownedCard == null || ownedCard.CurrentDefinition == null)
                        continue;

                    OwnedCardFileData cardFileData = new()
                    {
                        uniqueId = ownedCard.UniqueId,
                        cardId = contentRepository.GetCardId(ownedCard.CurrentDefinition),
                        augmentIds = new List<string>()
                    };

                    IReadOnlyList<CardAugmentDef> augments = ownedCard.AppliedAugments;
                    for (int augmentIndex = 0; augmentIndex < augments.Count; augmentIndex++)
                    {
                        CardAugmentDef augment = augments[augmentIndex];
                        string augmentId = contentRepository.GetAugmentId(augment);
                        if (!string.IsNullOrWhiteSpace(augmentId))
                            cardFileData.augmentIds.Add(augmentId);
                    }

                    fileData.deck.Add(cardFileData);
                }
            }

            if (run.ownedAugments != null)
            {
                for (int i = 0; i < run.ownedAugments.Count; i++)
                {
                    OwnedAugment ownedAugment = run.ownedAugments[i];
                    if (ownedAugment?.Definition == null)
                        continue;

                    string augmentId = contentRepository.GetAugmentId(ownedAugment.Definition);
                    if (string.IsNullOrWhiteSpace(augmentId))
                        continue;

                    fileData.ownedAugments.Add(new OwnedAugmentFileData
                    {
                        uniqueId = ownedAugment.UniqueId,
                        augmentId = augmentId
                    });
                }
            }

            if (run.ownedRelics != null)
            {
                for (int i = 0; i < run.ownedRelics.Count; i++)
                {
                    OwnedRelic ownedRelic = run.ownedRelics[i];
                    if (ownedRelic?.Definition == null)
                        continue;

                    string relicId = contentRepository.GetRelicId(ownedRelic.Definition);
                    if (!string.IsNullOrWhiteSpace(relicId) && !fileData.ownedRelicIds.Contains(relicId))
                        fileData.ownedRelicIds.Add(relicId);
                }
            }

            return fileData;
        }

        private RunSaveData DeserializeRun(RunFileData fileData)
        {
            RunSaveData run = new()
            {
                runId = fileData.runId,
                currentHealth = fileData.currentHealth,
                maxHealth = fileData.maxHealth,
                gold = fileData.gold,
                currentNodeId = fileData.currentNodeId,
                completedNodeIds = fileData.completedNodeIds ?? new List<string>(),
                mapState = fileData.mapState ?? new RunMapStateData(),
                pendingReward = fileData.pendingReward,
                queuedNextMapTemplateId = fileData.queuedNextMapTemplateId,
                endRunAfterPendingReward = fileData.endRunAfterPendingReward,
                seed = fileData.seed,
                deck = new List<OwnedCard>(),
                ownedAugments = new List<OwnedAugment>(),
                ownedRelics = new List<OwnedRelic>()
            };

            if (fileData.deck != null)
            {
                for (int i = 0; i < fileData.deck.Count; i++)
                {
                    OwnedCardFileData cardFileData = fileData.deck[i];
                    if (cardFileData == null)
                        continue;

                    CardDef definition = contentRepository.GetCardById(cardFileData.cardId);
                    if (definition == null)
                        continue;

                    List<CardAugmentDef> augments = new();
                    if (cardFileData.augmentIds != null)
                    {
                        for (int augmentIndex = 0; augmentIndex < cardFileData.augmentIds.Count; augmentIndex++)
                        {
                            CardAugmentDef augment = contentRepository.GetAugmentById(cardFileData.augmentIds[augmentIndex]);
                            if (augment != null)
                                augments.Add(augment);
                        }
                    }

                    run.deck.Add(new OwnedCard(definition, cardFileData.uniqueId, augments));
                }
            }

            if (fileData.ownedAugments != null)
            {
                for (int i = 0; i < fileData.ownedAugments.Count; i++)
                {
                    OwnedAugmentFileData augmentFileData = fileData.ownedAugments[i];
                    if (augmentFileData == null)
                        continue;

                    CardAugmentDef definition = contentRepository.GetAugmentById(augmentFileData.augmentId);
                    if (definition == null)
                        continue;

                    run.ownedAugments.Add(new OwnedAugment(definition, augmentFileData.uniqueId));
                }
            }

            if (fileData.ownedRelicIds != null)
            {
                for (int i = 0; i < fileData.ownedRelicIds.Count; i++)
                {
                    RelicDef relic = contentRepository.GetRelicById(fileData.ownedRelicIds[i]);
                    if (relic != null && !ContainsRelic(run.ownedRelics, relic))
                        run.ownedRelics.Add(new OwnedRelic(relic));
                }
            }

            return run;
        }

        private static bool ContainsRelic(List<OwnedRelic> ownedRelics, RelicDef relic)
        {
            if (ownedRelics == null || relic == null)
                return false;

            for (int i = 0; i < ownedRelics.Count; i++)
            {
                if (ownedRelics[i]?.Definition == relic)
                    return true;
            }

            return false;
        }

        [Serializable]
        private class RunFileData
        {
            public string runId;
            public int currentHealth;
            public int maxHealth;
            public int gold;
            public List<OwnedCardFileData> deck = new();
            public List<OwnedAugmentFileData> ownedAugments = new();
            public List<string> ownedRelicIds = new();
            public string currentNodeId;
            public List<string> completedNodeIds = new();
            public RunMapStateData mapState = new();
            public PendingRewardData pendingReward;
            public string queuedNextMapTemplateId;
            public bool endRunAfterPendingReward;
            public int seed;
        }

        [Serializable]
        private class OwnedCardFileData
        {
            public string uniqueId;
            public string cardId;
            public List<string> augmentIds = new();
        }

        [Serializable]
        private class OwnedAugmentFileData
        {
            public string uniqueId;
            public string augmentId;
        }
    }
}
