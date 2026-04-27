using System;
using System.Collections.Generic;

namespace RunFlow
{
    [Serializable]
    public class ProfileSaveData
    {
        public int metaCurrency;
        public List<string> unlockIds = new();
        public List<string> discoveredRelicIds = new();
        public string activeRunId;
        public bool debugUiEnabled;

        public bool HasUnlock(string unlockId)
        {
            return !string.IsNullOrWhiteSpace(unlockId) && unlockIds != null && unlockIds.Contains(unlockId);
        }

        public void AddUnlock(string unlockId)
        {
            if (string.IsNullOrWhiteSpace(unlockId))
                return;

            unlockIds ??= new List<string>();
            if (!unlockIds.Contains(unlockId))
                unlockIds.Add(unlockId);
        }

        public bool HasDiscoveredRelic(string relicId)
        {
            return !string.IsNullOrWhiteSpace(relicId) &&
                   discoveredRelicIds != null &&
                   discoveredRelicIds.Contains(relicId);
        }

        public void AddDiscoveredRelic(string relicId)
        {
            if (string.IsNullOrWhiteSpace(relicId))
                return;

            discoveredRelicIds ??= new List<string>();
            if (!discoveredRelicIds.Contains(relicId))
                discoveredRelicIds.Add(relicId);
        }
    }
}
